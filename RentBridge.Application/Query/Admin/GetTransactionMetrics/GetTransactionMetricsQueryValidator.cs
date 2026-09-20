using FluentValidation;

namespace RentBridge.Application.Query.Admin;

public sealed class GetTransactionMetricsQueryValidator : AbstractValidator<GetTransactionMetricsQuery>
{
    public GetTransactionMetricsQueryValidator()
    {
        RuleFor(x => x)
            .Must(q => !q.From.HasValue || !q.To.HasValue || q.From < q.To)
            .WithMessage("'from' must be earlier than 'to'.");
    }
}
