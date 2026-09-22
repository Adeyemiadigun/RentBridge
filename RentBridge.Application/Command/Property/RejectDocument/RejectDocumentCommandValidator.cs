using FluentValidation;
using RentBridge.Application.Command.Property;

public sealed class RejectDocumentCommandValidator : AbstractValidator<RejectDocumentCommand>
{
    public RejectDocumentCommandValidator()
    {
        RuleFor(x => x.DocumentId)
            .NotEmpty()
            .WithMessage("Document ID is required.")
            .NotEqual(Guid.Empty)
            .WithMessage("Document ID cannot be an empty GUID.");

        RuleFor(x => x.PropertyId)
            .NotEmpty()
            .WithMessage("Property ID is required.")
            .NotEqual(Guid.Empty)
            .WithMessage("Property ID cannot be an empty GUID.");

        RuleFor(x => x.Reason)
            .MaximumLength(1000)
            .WithMessage("Rejection reason cannot exceed 1000 characters.");
    }
}