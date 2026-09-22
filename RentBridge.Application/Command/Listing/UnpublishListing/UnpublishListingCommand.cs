using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Listing
{
    /// <summary>
    /// Owner-only: takes a published listing off public search. The listing
    /// can be published again afterwards.
    /// </summary>
    public record class UnpublishListingCommand(Guid ListingId) : IRequest<Result<Guid>>;
}
