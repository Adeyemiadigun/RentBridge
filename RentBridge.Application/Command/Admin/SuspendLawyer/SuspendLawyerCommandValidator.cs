using FluentValidation;

namespace RentBridge.Application.Command.Admin;

public sealed class SuspendLawyerCommandValidator : AbstractValidator<SuspendLawyerCommand>
{
    public SuspendLawyerCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");
    }
}
