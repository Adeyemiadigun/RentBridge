using FluentValidation;

namespace RentBridge.Application.Command.Listing
{
    public sealed class UnpublishListingCommandValidator : AbstractValidator<UnpublishListingCommand>
    {
        public UnpublishListingCommandValidator()
        {
            RuleFor(x => x.ListingId)
                .NotEmpty()
                .WithMessage("Listing ID is required.");
        }
    }
}
