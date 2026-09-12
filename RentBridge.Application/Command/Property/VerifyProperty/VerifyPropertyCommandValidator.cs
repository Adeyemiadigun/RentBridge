using FluentValidation;
using RentBridge.Application.Command.Property;

public sealed class VerifyPropertyCommandValidator : AbstractValidator<VerifyPropertyCommand>
{
    public VerifyPropertyCommandValidator()
    {
        RuleFor(x => x.PropertyId)
            .NotEmpty()
            .WithMessage("Property ID is required.")
            .NotEqual(Guid.Empty)
            .WithMessage("Property ID cannot be an empty GUID.");

    }
}