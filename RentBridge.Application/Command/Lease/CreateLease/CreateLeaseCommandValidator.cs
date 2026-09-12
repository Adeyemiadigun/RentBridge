using FluentValidation;

namespace RentBridge.Application.Command.Lease;

public sealed class CreateLeaseCommandValidator : AbstractValidator<CreateLeaseCommand>
{
    public CreateLeaseCommandValidator()
    {
        RuleFor(x => x.ListingId)
            .NotEmpty()
            .WithMessage("Listing ID is required.");
    }
}