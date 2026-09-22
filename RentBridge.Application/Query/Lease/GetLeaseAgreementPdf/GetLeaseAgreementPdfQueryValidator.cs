using FluentValidation;

namespace RentBridge.Application.Query.Lease;

public sealed class GetLeaseAgreementPdfQueryValidator : AbstractValidator<GetLeaseAgreementPdfQuery>
{
    public GetLeaseAgreementPdfQueryValidator()
    {
        RuleFor(x => x.LeaseId)
            .NotEmpty()
            .WithMessage("Lease ID is required.");
    }
}