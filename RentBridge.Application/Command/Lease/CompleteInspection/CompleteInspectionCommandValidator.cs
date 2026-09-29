using FluentValidation;

namespace RentBridge.Application.Command.Lease;

public sealed class CompleteInspectionCommandValidator : AbstractValidator<CompleteInspectionCommand>
{
    public CompleteInspectionCommandValidator()
    {
        RuleFor(x => x.LeaseId)
            .NotEmpty()
            .WithMessage("Lease ID is required.");

        // The inspection already happened, so a future date is a data-entry
        // mistake. This is a CALENDAR date, not an instant, so it is compared at
        // date granularity with a one-day tolerance: a client ahead of UTC would
        // otherwise have "today" rejected during the first hours of their morning.
        RuleFor(x => x.ActualDate)
            .NotEmpty()
            .WithMessage("The actual inspection date is required.")
            .Must(d => d.UtcDateTime.Date <= DateTimeOffset.UtcNow.UtcDateTime.Date.AddDays(1))
            .WithMessage("The actual inspection date cannot be in the future.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .WithMessage("Notes cannot exceed 1000 characters.");
    }
}
