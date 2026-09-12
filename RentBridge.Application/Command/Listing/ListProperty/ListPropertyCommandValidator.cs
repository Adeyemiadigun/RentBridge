using FluentValidation;
using RentBridge.Application.Command.Listing;

public sealed class ListPropertyCommandValidator
    : AbstractValidator<ListPropertyCommand>
{
    public ListPropertyCommandValidator()
    {
        RuleFor(x => x.PropertyId)
            .NotEmpty()
            .WithMessage("Property ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Property title is required.")
            .MinimumLength(3)
            .WithMessage("Property title must be at least 3 characters.")
            .MaximumLength(200)
            .WithMessage("Property title cannot exceed 200 characters.");

        RuleFor(x => x.PriceAmount)
            .GreaterThan(0)
            .WithMessage("Property price must be greater than zero.");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .WithMessage("Description cannot exceed 2000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}