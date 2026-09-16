using FluentValidation;

namespace RentBridge.Application.Query.Lease;

public sealed class GetLeaseQueryValidator : AbstractValidator<GetLeaseQuery>
{
    public GetLeaseQueryValidator()
    {
        RuleFor(x => x.LeaseId)
            .NotEmpty()
            .WithMessage("Lease ID is required.");
    }
}