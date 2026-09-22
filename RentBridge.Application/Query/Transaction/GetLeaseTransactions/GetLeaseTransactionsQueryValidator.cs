using FluentValidation;

namespace RentBridge.Application.Query.Transaction;

public sealed class GetLeaseTransactionsQueryValidator : AbstractValidator<GetLeaseTransactionsQuery>
{
    public GetLeaseTransactionsQueryValidator()
    {
        RuleFor(x => x.LeaseId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
