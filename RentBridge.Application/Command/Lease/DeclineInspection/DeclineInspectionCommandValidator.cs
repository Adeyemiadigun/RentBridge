using FluentValidation;

namespace RentBridge.Application.Command.Lease;

public sealed class DeclineInspectionCommandValidator : AbstractValidator<DeclineInspectionCommand>
{
    public DeclineInspectionCommandValidator()
    {
        RuleFor(x => x.LeaseId)
            .NotEmpty()
            .WithMessage("Lease ID is required.");
    }
}