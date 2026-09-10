using FluentValidation;

namespace RentBridge.Application.Query.Listing;

public sealed class SearchListingsQueryValidator : AbstractValidator<SearchListingsQuery>
{
    public SearchListingsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page size must be at least 1.")
            .LessThanOrEqualTo(100)
            .WithMessage("Page size cannot exceed 100.");

        RuleFor(x => x.MinPrice)
            .GreaterThan(0)
            .WithMessage("Minimum price must be greater than zero.")
            .When(x => x.MinPrice.HasValue);

        RuleFor(x => x.MaxPrice)
            .GreaterThan(0)
            .WithMessage("Maximum price must be greater than zero.")
            .When(x => x.MaxPrice.HasValue);

        RuleFor(x => x)
            .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MinPrice <= x.MaxPrice)
            .WithMessage("Minimum price cannot be greater than maximum price.");
    }
}