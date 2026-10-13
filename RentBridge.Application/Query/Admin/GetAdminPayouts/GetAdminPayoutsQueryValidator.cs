using FluentValidation;

namespace RentBridge.Application.Query.Admin;

public sealed class GetAdminPayoutsQueryValidator : AbstractValidator<GetAdminPayoutsQuery>
{
    public GetAdminPayoutsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
