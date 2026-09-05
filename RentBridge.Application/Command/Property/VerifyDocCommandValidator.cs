using FluentValidation;
using RentBridge.Application.Command.Property;

public sealed class VerifyDocCommandValidator : AbstractValidator<VerifyDocCommand>
{
    public VerifyDocCommandValidator()
    {
        RuleFor(x => x.DocumentId)
            .NotEmpty()
            .WithMessage("Document ID is required.")
            .NotEqual(Guid.Empty)
            .WithMessage("Document ID cannot be an empty GUID.");
    }
}