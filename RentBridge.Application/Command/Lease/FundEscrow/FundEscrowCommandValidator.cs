using FluentValidation;

namespace RentBridge.Application.Command.Lease;

public sealed class FundEscrowCommandValidator : AbstractValidator<FundEscrowCommand>
{
    public FundEscrowCommandValidator()
    {
        RuleFor(x => x.LeaseId)
            .NotEmpty()
            .WithMessage("Lease ID is required.");
    }
}