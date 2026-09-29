using FluentValidation;

namespace RentBridge.Application.Command.Lease;

public sealed class ConfirmInspectionCommandValidator : AbstractValidator<ConfirmInspectionCommand>
{
    public ConfirmInspectionCommandValidator()
    {
        RuleFor(x => x.LeaseId)
            .NotEmpty()
            .WithMessage("Lease ID is required.");

        RuleFor(x => x.ScheduledDate)
            .NotEmpty()
            .WithMessage("A scheduled inspection date is required.")
            .GreaterThan(DateTimeOffset.UtcNow)
            .WithMessage("The scheduled inspection date must be in the future.");
    }
}