using FluentValidation;

namespace RentBridge.Application.Query.Admin;

public sealed class GetLawyersQueryValidator : AbstractValidator<GetLawyersQuery>
{
    public GetLawyersQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
