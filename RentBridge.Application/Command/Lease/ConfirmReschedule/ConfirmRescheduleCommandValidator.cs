using FluentValidation;

namespace RentBridge.Application.Command.Lease;

public sealed class ConfirmRescheduleCommandValidator : AbstractValidator<ConfirmRescheduleCommand>
{
    public ConfirmRescheduleCommandValidator()
    {
        RuleFor(x => x.LeaseId)
            .NotEmpty()
            .WithMessage("Lease ID is required.");
    }
}