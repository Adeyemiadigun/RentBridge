using FluentValidation;

namespace RentBridge.Application.Command.SubmitIdentityVerification;

public sealed class SubmitIdentityVerificationCommandValidator
    : AbstractValidator<SubmitIdentityVerificationCommand>
{
    public SubmitIdentityVerificationCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User id is required.");

        RuleFor(x => x.Nin)
            .NotEmpty()
            .WithMessage("NIN is required.")
            .Length(11)
            .WithMessage("NIN must be exactly 11 digits.")
            .Matches("^[0-9]+$")
            .WithMessage("NIN must contain only digits.");
    }
}
