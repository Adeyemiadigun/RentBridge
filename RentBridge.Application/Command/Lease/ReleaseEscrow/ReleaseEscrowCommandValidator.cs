using FluentValidation;

namespace RentBridge.Application.Command.Lease;

public sealed class ReleaseEscrowCommandValidator : AbstractValidator<ReleaseEscrowCommand>
{
    public ReleaseEscrowCommandValidator()
    {
        RuleFor(x => x.LeaseId)
            .NotEmpty()
            .WithMessage("Lease ID is required.");
    }
}