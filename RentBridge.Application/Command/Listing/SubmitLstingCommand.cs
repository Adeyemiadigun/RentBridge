using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Listing
{
    public sealed record SubmitListingCommand(Guid ListingId) : IRequest<Result<Guid>>;
}
