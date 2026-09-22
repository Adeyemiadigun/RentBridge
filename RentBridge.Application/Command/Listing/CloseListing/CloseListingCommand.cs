using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Listing
{
    /// <summary>
    /// Owner-only: closes a listing permanently. Closed listings leave public
    /// search and cannot be edited or re-published.
    /// </summary>
    public record class CloseListingCommand(Guid ListingId) : IRequest<Result<Guid>>;
}
