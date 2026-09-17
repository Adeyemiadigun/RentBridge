using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Listing
{
    /// <summary>
    /// Owner-only: updates a listing's title, description, and/or price.
    /// Closed listings cannot be edited. Null fields are left unchanged.
    /// </summary>
    public record class EditListingCommand(
        Guid ListingId,
        string? Title = null,
        string? Description = null,
        decimal? PriceAmount = null) : IRequest<Result<Guid>>;
}
