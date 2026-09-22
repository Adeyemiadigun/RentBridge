using FluentValidation;

namespace RentBridge.Application.Command.Lease;

public sealed class RequestInspectionCommandValidator : AbstractValidator<RequestInspectionCommand>
{
    public RequestInspectionCommandValidator()
    {
        RuleFor(x => x.LeaseId)
            .NotEmpty()
            .WithMessage("Lease ID is required.");

        RuleFor(x => x.PreferredDate)
            .NotEqual(DateTimeOffset.MinValue)
            .WithMessage("A preferred inspection date is required.")
            .Must(d => d > DateTimeOffset.UtcNow)
            .WithMessage("Preferred inspection date must be in the future.");

        RuleFor(x => x.Note)
            .MaximumLength(1000)
            .WithMessage("Note cannot exceed 1000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Note));
    }
}