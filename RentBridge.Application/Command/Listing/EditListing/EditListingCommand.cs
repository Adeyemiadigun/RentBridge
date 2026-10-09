using MediatR;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;

namespace RentBridge.Application.Command.Listing
{
    /// <summary>
    /// Owner-only: updates a listing's title, description, price, and/or rent frequency.
    /// Closed listings cannot be edited. Null fields are left unchanged.
    /// </summary>
    public record class EditListingCommand(
        Guid ListingId,
        string? Title = null,
        string? Description = null,
        decimal? PriceAmount = null,
        RentFrequency? RentFrequency = null) : IRequest<Result<Guid>>;
}
