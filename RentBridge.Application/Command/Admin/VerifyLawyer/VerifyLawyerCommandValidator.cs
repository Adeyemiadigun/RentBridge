using FluentValidation;

namespace RentBridge.Application.Command.Admin;

public sealed class VerifyLawyerCommandValidator : AbstractValidator<VerifyLawyerCommand>
{
    public VerifyLawyerCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");
    }
}