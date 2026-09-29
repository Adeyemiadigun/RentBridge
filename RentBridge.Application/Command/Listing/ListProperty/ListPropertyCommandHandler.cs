using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Common;
using RentBridge.Domain.ValueObjects;
using ListingAggreagte = RentBridge.Domain.Aggregates.Listing;

namespace RentBridge.Application.Command.Listing
{
    public class ListPropertyCommandHandler(IUnitOfWork unitOfWork, ICurrentUser _currentUser, ILogger<ListPropertyCommandHandler> logger) : IRequestHandler<ListPropertyCommand, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(ListPropertyCommand request, CancellationToken cancellationToken)
        {
            var res = await _currentUser.GetCurrentUser(true, cancellationToken);
            if(!res.IsSuccess)
            {
                return Result<Guid>.Fail(res.Error!);
            }
            var user = res.Value;
            var property = await unitOfWork.Repository<Domain.Aggregates.Property>().FirstOrDefault(p => p.Id == request.PropertyId, cancellationToken);
            if(property == null)
            {
                logger.LogInformation("Property not found {propertyId}",request.PropertyId);
                return Result<Guid>.Fail("Property not found");
            }
            if(property.OwnerUserId != user.Id)
            {
                logger.LogInformation("User {userId} is not the owner of property {propertyId}", user.Id, request.PropertyId);
                return Result<Guid>.Fail("You can only list your own property");
            }
            // Draft listing is allowed before ownership verification; the
            // PublishListingCommand stays gated on property verification, so
            // the owner submits the property + docs, a lawyer verifies, and
            // then the draft can be published.
            var moneyResult = Money.Naira(request.PriceAmount);
            if (!moneyResult.IsSuccess)
            {
                logger.LogInformation("Invalid listing price: {error}", moneyResult.Error);
                return Result<Guid>.Fail(moneyResult.Error!);
            }

            Money? cautionFee = null;
            if (request.CautionFeeAmount.HasValue)
            {
                var feeResult = Money.Naira(request.CautionFeeAmount.Value);
                if (!feeResult.IsSuccess)
                {
                    logger.LogInformation("Invalid caution fee: {error}", feeResult.Error);
                    return Result<Guid>.Fail(feeResult.Error!);
                }
                cautionFee = feeResult.Value;
            }

            Money? realHouseFee = null;
            if (request.RealHouseFeeAmount.HasValue)
            {
                var feeResult = Money.Naira(request.RealHouseFeeAmount.Value);
                if (!feeResult.IsSuccess)
                {
                    logger.LogInformation("Invalid real house fee: {error}", feeResult.Error);
                    return Result<Guid>.Fail(feeResult.Error!);
                }
                realHouseFee = feeResult.Value;
            }

            Money? agentFee = null;
            if (request.AgentFeeAmount.HasValue)
            {
                var feeResult = Money.Naira(request.AgentFeeAmount.Value);
                if (!feeResult.IsSuccess)
                {
                    logger.LogInformation("Invalid agent fee: {error}", feeResult.Error);
                    return Result<Guid>.Fail(feeResult.Error!);
                }
                agentFee = feeResult.Value;
            }

            var propertyListing = new ListingAggreagte(
                user.Id,
                property.Id,
                request.Title,
                moneyResult.Value,
                request.Description,
                request.ListingType,
                request.PaymentPlan,
                cautionFee,
                request.OtherExpenses,
                realHouseFee,
                agentFee);

            if (request.ImageUrls is not null)
            {
                propertyListing.SetImages(request.ImageUrls);
            }

             unitOfWork.Repository<ListingAggreagte>().Add(propertyListing);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Ok(propertyListing.Id);
        }
    }
}
