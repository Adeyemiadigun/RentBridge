using FluentValidation;

namespace RentBridge.Application.Command.Listing
{
    public sealed class CloseListingCommandValidator : AbstractValidator<CloseListingCommand>
    {
        public CloseListingCommandValidator()
        {
            RuleFor(x => x.ListingId)
                .NotEmpty()
                .WithMessage("Listing ID is required.");
        }
    }
}
