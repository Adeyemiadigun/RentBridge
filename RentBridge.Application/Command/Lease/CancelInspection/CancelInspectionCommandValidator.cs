using FluentValidation;

namespace RentBridge.Application.Command.Lease;

public sealed class CancelInspectionCommandValidator : AbstractValidator<CancelInspectionCommand>
{
    public CancelInspectionCommandValidator()
    {
        RuleFor(x => x.LeaseId)
            .NotEmpty()
            .WithMessage("Lease ID is required.");
    }
}