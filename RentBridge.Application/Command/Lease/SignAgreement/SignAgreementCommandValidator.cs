using FluentValidation;

namespace RentBridge.Application.Command.Lease;

public sealed class SignAgreementCommandValidator : AbstractValidator<SignAgreementCommand>
{
    public SignAgreementCommandValidator()
    {
        RuleFor(x => x.LeaseId)
            .NotEmpty()
            .WithMessage("Lease ID is required.");

        RuleFor(x => x.SignatureImage)
            .MaximumLength(2_000_000)
            .WithMessage("Signature image cannot exceed 2,000,000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.SignatureImage));
    }
}