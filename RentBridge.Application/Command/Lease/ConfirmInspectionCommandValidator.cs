using FluentValidation;

namespace RentBridge.Application.Command.Lease;

public sealed class ConfirmInspectionCommandValidator : AbstractValidator<ConfirmInspectionCommand>
{
    public ConfirmInspectionCommandValidator()
    {
        RuleFor(x => x.LeaseId)
            .NotEmpty()
            .WithMessage("Lease ID is required.");
    }
}