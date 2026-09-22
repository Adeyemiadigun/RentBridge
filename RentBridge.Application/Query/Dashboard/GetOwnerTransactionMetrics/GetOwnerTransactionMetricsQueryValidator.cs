using FluentValidation;

namespace RentBridge.Application.Query.Dashboard;

public sealed class GetOwnerTransactionMetricsQueryValidator : AbstractValidator<GetOwnerTransactionMetricsQuery>
{
    public GetOwnerTransactionMetricsQueryValidator()
    {
        RuleFor(x => x)
            .Must(q => !q.From.HasValue || !q.To.HasValue || q.From < q.To)
            .WithMessage("'from' must be earlier than 'to'.");
    }
}
