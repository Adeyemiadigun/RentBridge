using FluentValidation;
using RentBridge.Application.Command.Property;

public sealed class CreatePropertyCommandValidator
    : AbstractValidator<CreatePropertyCommand>
{
    public CreatePropertyCommandValidator()
    {
        RuleFor(x => x.Street)
            .NotEmpty()
            .WithMessage("Street is required.")
            .MaximumLength(200)
            .WithMessage("Street cannot exceed 200 characters.");

        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage("City is required.")
            .MaximumLength(100)
            .WithMessage("City cannot exceed 100 characters.");

        RuleFor(x => x.Area)
            .NotEmpty()
            .WithMessage("Area is required.")
            .MaximumLength(100)
            .WithMessage("Area cannot exceed 100 characters.");

        RuleFor(x => x.State)
            .NotEmpty()
            .WithMessage("State is required.")
            .MaximumLength(100)
            .WithMessage("State cannot exceed 100 characters.");

        RuleFor(x => x.DocumentUrls)
            .NotNull()
            .WithMessage("Document URLs are required.")
            .NotEmpty()
            .WithMessage("At least one document URL is required.");

        RuleForEach(x => x.DocumentUrls)
            .NotEmpty()
            .WithMessage("Document URL cannot be empty.")
            .Must(BeValidUrl)
            .WithMessage("Document URL must be a valid URL.");
    }

    private static bool BeValidUrl(string url)
    {
        return Uri.TryCreate(
            url,
            UriKind.Absolute,
            out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp ||
                uri.Scheme == Uri.UriSchemeHttps);
    }
}