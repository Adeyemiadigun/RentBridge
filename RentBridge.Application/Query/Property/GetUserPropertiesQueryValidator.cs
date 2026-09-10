using FluentValidation;

namespace RentBridge.Application.Query.Property;

public sealed class GetUserPropertiesQueryValidator : AbstractValidator<GetUserPropertiesQuery>
{
    public GetUserPropertiesQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page size must be at least 1.")
            .LessThanOrEqualTo(100)
            .WithMessage("Page size cannot exceed 100.");
    }
}