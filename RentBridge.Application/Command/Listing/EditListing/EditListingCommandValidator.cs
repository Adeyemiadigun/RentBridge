using FluentValidation;

namespace RentBridge.Application.Command.Listing
{
    public sealed class EditListingCommandValidator : AbstractValidator<EditListingCommand>
    {
        public EditListingCommandValidator()
        {
            RuleFor(x => x.ListingId)
                .NotEmpty()
                .WithMessage("Listing ID is required.");

            RuleFor(x => x.Title)
                .MinimumLength(3)
                .WithMessage("Property title must be at least 3 characters.")
                .MaximumLength(200)
                .WithMessage("Property title cannot exceed 200 characters.")
                .When(x => x.Title is not null);

            RuleFor(x => x.PriceAmount)
                .GreaterThan(0)
                .WithMessage("Property price must be greater than zero.")
                .When(x => x.PriceAmount.HasValue);

            RuleFor(x => x.Description)
                .MaximumLength(2000)
                .WithMessage("Description cannot exceed 2000 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.Description));
        }
    }
}
