using FluentValidation;

namespace RentBridge.Application.Command.Property;

public class SubmitPropertyForReviewCommandValidator : AbstractValidator<SubmitPropertyForReviewCommand>
{
    public SubmitPropertyForReviewCommandValidator()
    {
        RuleFor(x => x.PropertyId)
            .NotEmpty().WithMessage("Property ID is required.");
    }
}