using FluentValidation;

namespace RentBridge.Application.Command.PayoutAccount;

public sealed class ResolvePayoutAccountCommandValidator : AbstractValidator<ResolvePayoutAccountCommand>
{
    public ResolvePayoutAccountCommandValidator()
    {
        RuleFor(x => x.BankCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.AccountNumber)
            .NotEmpty()
            .Length(10).WithMessage("A NUBAN account number must be 10 digits.")
            .Matches("^[0-9]+$").WithMessage("Account number must contain digits only.");
    }
}
