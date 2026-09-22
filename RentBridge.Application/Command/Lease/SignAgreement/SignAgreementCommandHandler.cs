using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using LeaseAggregate = RentBridge.Domain.Aggregates.Lease;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using RentBridge.Domain.ValueObjects;

namespace RentBridge.Application.Command.Lease;

public sealed class SignAgreementCommandHandler(
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor,
    ILogger<SignAgreementCommandHandler> logger)
    : IRequestHandler<SignAgreementCommand, Result>
{
    public async Task<Result> Handle(SignAgreementCommand request, CancellationToken cancellationToken)
    {
        var res = await currentUser.GetCurrentUser(false, cancellationToken);
        if (!res.IsSuccess)
        {
            return Result.Fail(res.Error!);
        }
        var user = res.Value;

        var lease = await unitOfWork.Repository<LeaseAggregate>()
            .FirstOrDefault(l => l.Id == request.LeaseId, cancellationToken);
        if (lease is null)
        {
            logger.LogInformation("Lease {LeaseId} not found", request.LeaseId);
            return Result.Fail("Lease not found");
        }

        var isTenant = user.Id == lease.TenantUserId;
        var isLandlord = user.Id == lease.LandlordUserId;
        if (!isTenant && !isLandlord)
        {
            logger.LogInformation("User {UserId} is not a party to lease {LeaseId}", user.Id, request.LeaseId);
            return Result.Fail("Only the tenant or landlord of this lease can sign the agreement.");
        }

        var party = isTenant ? LeaseParty.Tenant : LeaseParty.Landlord;
        var signature = new SignatureRecord(
            party,
            HashSignature(request, user.Id),
            DateTimeOffset.UtcNow,
            ClientIp());

        var sign = lease.Sign(party, signature);
        if (!sign.IsSuccess)
        {
            logger.LogInformation("Lease {LeaseId} cannot be signed by {Party}: {Error}", request.LeaseId, party, sign.Error);
            return Result.Fail(sign.Error!);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    private static string HashSignature(SignAgreementCommand request, Guid userId)
    {
        var raw = string.IsNullOrWhiteSpace(request.SignatureImage)
            ? $"{userId}|{DateTimeOffset.UtcNow:O}"
            : request.SignatureImage;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToBase64String(bytes);
    }

    private string ClientIp()
        => httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
}