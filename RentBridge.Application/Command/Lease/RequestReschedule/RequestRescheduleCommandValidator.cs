using FluentValidation;

namespace RentBridge.Application.Command.Lease;

public sealed class RequestRescheduleCommandValidator : AbstractValidator<RequestRescheduleCommand>
{
    public RequestRescheduleCommandValidator()
    {
        RuleFor(x => x.LeaseId)
            .NotEmpty()
            .WithMessage("Lease ID is required.");

        RuleFor(x => x.NewDate)
            .GreaterThan(DateTimeOffset.UtcNow)
            .WithMessage("The proposed inspection date must be in the future.");
    }
}