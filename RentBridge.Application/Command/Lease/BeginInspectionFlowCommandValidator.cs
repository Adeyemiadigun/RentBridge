using FluentValidation;

namespace RentBridge.Application.Command.Lease;

public sealed class BeginInspectionFlowCommandValidator : AbstractValidator<BeginInspectionFlowCommand>
{
    public BeginInspectionFlowCommandValidator()
    {
        RuleFor(x => x.LeaseId)
            .NotEmpty()
            .WithMessage("Lease ID is required.");
    }
}