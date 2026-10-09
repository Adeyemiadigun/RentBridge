using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Common.Options;
using RentBridge.Application.Common.Payments;
using RentBridge.Application.Dtos.Lease;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using RentBridge.Domain.ValueObjects;
using EscrowPaymentEntity = RentBridge.Domain.Entities.EscrowPayment;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;
using UserAggregate = RentBridge.Domain.Aggregates.User;

namespace RentBridge.Application.Command.Lease;

public sealed class FundEscrowCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IPlatformSettingsService settingsService,
    IEscrowProvider escrowProvider,
    PaymentOptions paymentOptions,
    ILogger<FundEscrowCommandHandler> logger)
    : IRequestHandler<FundEscrowCommand, Result<FundEscrowResponse>>
{
    public async Task<Result<FundEscrowResponse>> Handle(FundEscrowCommand request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(true, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result<FundEscrowResponse>.Fail(res.Error!);
        }
        var tenant = res.Value;

        var lease = await unitOfWork.Leases.GetWithAgreementGraphAsync(request.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogInformation("Lease {LeaseId} not found", request.LeaseId);
            return Result<FundEscrowResponse>.Fail("Lease not found.");
        }

        if (lease.TenantUserId != tenant.Id)
        {
            logger.LogInformation("User {UserId} tried to fund escrow on a lease they do not tenant", tenant.Id);
            return Result<FundEscrowResponse>.Forbid("Only the tenant on this lease can fund escrow.");
        }

        if (!lease.Agreement.IsFullySigned)
        {
            logger.LogInformation(
                "Tenant {UserId} attempted to fund escrow on lease {LeaseId} before both parties signed",
                tenant.Id,
                request.LeaseId);
            return Result<FundEscrowResponse>.Fail(
                "Both you and the landlord must sign the agreement before payment can be made.");
        }

        var existing = lease.EscrowPayments
            .FirstOrDefault(p => p.Status == EscrowStatus.Initialized && !string.IsNullOrWhiteSpace(p.CheckoutUrl));
        if (existing is not null)
        {
            return Result<FundEscrowResponse>.Ok(
                new FundEscrowResponse(existing.CheckoutUrl!, existing.Reference, existing.Status.ToString(), existing.GrossAmount.Amount, existing.GrossAmount.Currency));
        }

        if (lease.EscrowPayments.Any(p => p.Status is EscrowStatus.Funded or EscrowStatus.Releasing or EscrowStatus.Released))
        {
            return Result<FundEscrowResponse>.Fail("Escrow has already been funded for this lease.");
        }

        var listing = await unitOfWork.Repository<Domain.Aggregates.Listing>()
            .FirstOrDefault(l => l.Id == lease.ListingId, cancellationToken);
        if (listing is null)
        {
            return Result<FundEscrowResponse>.Fail("Listing for this lease was not found.");
        }

        var landlord = await unitOfWork.Repository<UserAggregate>()
            .FirstOrDefault(u => u.Id == lease.LandlordUserId, cancellationToken);
        if (landlord?.PayoutAccount is not { IsActive: true })
        {
            logger.LogWarning("Lease {LeaseId} landlord {LandlordUserId} has no payout account", lease.Id, lease.LandlordUserId);
            return Result<FundEscrowResponse>.Fail("The landlord has not registered a payout account yet.");
        }

        var setRecipient = lease.SetLandlordPayoutRecipientCode(landlord.PayoutAccount.RecipientCode);
        if (!setRecipient.IsSuccess)
        {
            return Result<FundEscrowResponse>.Fail(setRecipient.Error!);
        }

        var settingsRes = await settingsService.GetOrCreateAsync(cancellationToken);
        if (!settingsRes.IsSuccess)
        {
            return Result<FundEscrowResponse>.Fail(settingsRes.Error!);
        }
        var settings = settingsRes.Value;

        var gross = listing.Price with { };
        
        // Calculate total amount: gross (annual rent) + caution fee + real house fee + agent fee
        // These additional fees are added on top of the gross rent
        var cautionFee = listing.CautionFee?.Amount ?? 0m;
        var realHouseFee = listing.RealHouseFee?.Amount ?? 0m;
        var agentFee = listing.AgentFee?.Amount ?? 0m;
        var totalAmount = gross.Amount + cautionFee + realHouseFee + agentFee;
        var totalMoney = Money.Naira(totalAmount);
        if (!totalMoney.IsSuccess)
        {
            return Result<FundEscrowResponse>.Fail(totalMoney.Error!);
        }

        var split = BuildSplit(gross, settings.PlatformCommissionRate, settings.LegalFeeRate);
        if (!split.IsSuccess)
        {
            return Result<FundEscrowResponse>.Fail(split.Error!);
        }

        var newReference = $"RB{Guid.NewGuid():N}".ToUpperInvariant();

        var payment = lease.EscrowPayments
            .Where(p => p.Status is EscrowStatus.Failed or EscrowStatus.Pending)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefault();
        var isNewPayment = payment is null;

        if (payment is not null)
        {
            var reinitialize = payment.Reinitialize(newReference);
            if (!reinitialize.IsSuccess)
            {
                return Result<FundEscrowResponse>.Fail(reinitialize.Error!);
            }
        }
        else
        {
            payment = new EscrowPaymentEntity(lease.Id, tenant.Id, totalMoney.Value, newReference, lease.Id);

            var record = lease.AddPendingEscrowPayment(payment);
            if (!record.IsSuccess)
            {
                return Result<FundEscrowResponse>.Fail(record.Error!);
            }
        }

        payment.AttachSplit(split.Value);

        if (isNewPayment)
        {
            unitOfWork.MarkAsAdded(payment);
        }

        var callbackUrl = string.IsNullOrWhiteSpace(paymentOptions.Paystack.ResolvedCallbackUrl)
            ? paymentOptions.Paystack.ResolvedCallbackUrl
            : $"{paymentOptions.Paystack.ResolvedCallbackUrl}?leaseId={request.LeaseId}&paymentId={payment.Id}";

        var init = await escrowProvider.InitializeAsync(
            new PaymentInitiationRequest(
                payment.Id,
                payment.Reference,
                totalMoney.Value.Currency,
                totalMoney.Value.Amount,
                tenant.Email.Value,
                callbackUrl),
            cancellationToken);
        if (!init.IsSuccess)
        {
            logger.LogWarning("Escrow initialization failed for lease {LeaseId}: {Error}", request.LeaseId, init.Error);
            return Result<FundEscrowResponse>.Fail(init.Error!);
        }

        var attachUrl = payment.AttachCheckoutUrl(init.Value.CheckoutUrl);
        if (!attachUrl.IsSuccess)
        {
            return Result<FundEscrowResponse>.Fail(attachUrl.Error!);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<FundEscrowResponse>.Ok(
            new FundEscrowResponse(payment.CheckoutUrl!, payment.Reference, payment.Status.ToString(), totalMoney.Value.Amount, totalMoney.Value.Currency));
    }

    private static Result<FeeSplit> BuildSplit(Money gross, decimal commissionRate, decimal legalFeeRate)
    {
        var commission = RoundPercent(gross.Amount, commissionRate);
        var legalFee = RoundPercent(gross.Amount, legalFeeRate);

        var moneyCommission = Money.Naira(commission);
        if (!moneyCommission.IsSuccess) return Result<FeeSplit>.Fail(moneyCommission.Error!);
        var moneyLegal = Money.Naira(legalFee);
        if (!moneyLegal.IsSuccess) return Result<FeeSplit>.Fail(moneyLegal.Error!);

        return FeeSplit.Create(gross, moneyCommission.Value, moneyLegal.Value);
    }

    private static decimal RoundPercent(decimal amount, decimal rate)
        => decimal.Round(amount * rate / 100m, 2, MidpointRounding.AwayFromZero);
}