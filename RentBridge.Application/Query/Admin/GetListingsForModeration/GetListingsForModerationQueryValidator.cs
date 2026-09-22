using FluentValidation;

namespace RentBridge.Application.Query.Admin;

public sealed class GetListingsForModerationQueryValidator : AbstractValidator<GetListingsForModerationQuery>
{
    public GetListingsForModerationQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
