using FluentValidation;

namespace RentBridge.Application.Command.PayoutAccount;

public sealed class SetPayoutAccountCommandValidator : AbstractValidator<SetPayoutAccountCommand>
{
    public SetPayoutAccountCommandValidator()
    {
        RuleFor(x => x.BankCode)
            .NotEmpty().WithMessage("Select a bank.")
            .MaximumLength(20);

        RuleFor(x => x.BankName)
            .NotEmpty().WithMessage("Bank name is required.")
            .MaximumLength(150);

        RuleFor(x => x.AccountNumber)
            .NotEmpty().WithMessage("Account number is required.")
            .Length(10).WithMessage("A NUBAN account number must be 10 digits.")
            .Matches("^[0-9]+$").WithMessage("Account number must contain digits only.");
    }
}
