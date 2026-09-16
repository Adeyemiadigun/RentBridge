using FluentValidation;

namespace RentBridge.Application.Command.Lease;

public sealed class CertifyAgreementCommandValidator : AbstractValidator<CertifyAgreementCommand>
{
    public CertifyAgreementCommandValidator()
    {
        RuleFor(x => x.LeaseId)
            .NotEmpty()
            .WithMessage("Lease ID is required.");
    }
}