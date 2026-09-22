using FluentValidation;

namespace RentBridge.Application.Command.Admin;

public sealed class RejectLawyerCommandValidator : AbstractValidator<RejectLawyerCommand>
{
    public RejectLawyerCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.");
    }
}
