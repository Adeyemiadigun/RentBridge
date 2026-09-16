using FluentValidation;

namespace RentBridge.Application.Command.Lease;

public sealed class ReleaseEscrowCommandValidator : AbstractValidator<ReleaseEscrowCommand>
{
    public ReleaseEscrowCommandValidator()
    {
        RuleFor(x => x.LeaseId)
            .NotEmpty()
            .WithMessage("Lease ID is required.");
        RuleFor(x => x.RecipientCode)
            .MaximumLength(100)
            .WithMessage("Recipient code is too long.")
            .When(x => x.RecipientCode is not null);
    }
}