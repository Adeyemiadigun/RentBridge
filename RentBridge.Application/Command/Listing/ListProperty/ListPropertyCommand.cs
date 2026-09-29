using MediatR;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace RentBridge.Application.Command.Listing
{
    public record class ListPropertyCommand(
        Guid PropertyId,
        string Title,
        decimal PriceAmount,
        string? Description = null,
        ListingType ListingType = ListingType.Rent,
        PaymentPlan PaymentPlan = PaymentPlan.Outright,
        decimal? CautionFeeAmount = null,
        string? OtherExpenses = null,
        decimal? RealHouseFeeAmount = null,
        decimal? AgentFeeAmount = null,
        List<string>? ImageUrls = null) : IRequest<Result<Guid>>;
}
