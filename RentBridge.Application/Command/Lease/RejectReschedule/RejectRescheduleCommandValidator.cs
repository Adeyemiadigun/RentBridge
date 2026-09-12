using FluentValidation;

namespace RentBridge.Application.Command.Lease;

public sealed class RejectRescheduleCommandValidator : AbstractValidator<RejectRescheduleCommand>
{
    public RejectRescheduleCommandValidator()
    {
        RuleFor(x => x.LeaseId)
            .NotEmpty()
            .WithMessage("Lease ID is required.");
    }
}