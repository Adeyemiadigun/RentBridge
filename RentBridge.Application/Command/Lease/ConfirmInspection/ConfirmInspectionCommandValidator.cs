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
            .GreaterThan(DateTimeOffset.UtcNow)
            .When(x => x.ScheduledDate.HasValue)
            .WithMessage("The scheduled inspection date must be in the future.");
    }
}