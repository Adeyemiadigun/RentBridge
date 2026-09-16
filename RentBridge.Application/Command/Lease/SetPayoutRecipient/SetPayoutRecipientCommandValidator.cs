using FluentValidation;

namespace RentBridge.Application.Command.Lease;

public sealed class SetPayoutRecipientCommandValidator : AbstractValidator<SetPayoutRecipientCommand>
{
    public SetPayoutRecipientCommandValidator()
    {
        RuleFor(x => x.LeaseId)
            .NotEmpty()
            .WithMessage("Lease ID is required.");
        RuleFor(x => x.RecipientCode)
            .NotEmpty()
            .WithMessage("Recipient code is required.")
            .MaximumLength(100)
            .WithMessage("Recipient code is too long.");
    }
}