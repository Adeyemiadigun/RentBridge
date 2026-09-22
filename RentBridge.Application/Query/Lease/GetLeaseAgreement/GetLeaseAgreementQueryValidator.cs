using FluentValidation;

namespace RentBridge.Application.Query.Lease;

public sealed class GetLeaseAgreementQueryValidator : AbstractValidator<GetLeaseAgreementQuery>
{
    public GetLeaseAgreementQueryValidator()
    {
        RuleFor(x => x.LeaseId)
            .NotEmpty()
            .WithMessage("Lease ID is required.");
    }
}