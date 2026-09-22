using FluentValidation;

namespace RentBridge.Application.Command.Lease;

public sealed class BeginLegalReviewCommandValidator : AbstractValidator<BeginLegalReviewCommand>
{
    public BeginLegalReviewCommandValidator()
    {
        RuleFor(x => x.LeaseId)
            .NotEmpty()
            .WithMessage("Lease ID is required.");
    }
}