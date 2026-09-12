using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Listing
{
    public sealed record PublishListingCommand(Guid ListingId) : IRequest<Result<Guid>>;
}