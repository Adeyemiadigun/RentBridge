using FluentValidation;

namespace RentBridge.Application.Command.Admin;

public sealed class ReassignLeaseLawyerCommandValidator : AbstractValidator<ReassignLeaseLawyerCommand>
{
    public ReassignLeaseLawyerCommandValidator()
    {
        RuleFor(x => x.LeaseId)
            .NotEmpty()
            .WithMessage("Lease ID is required.");

        RuleFor(x => x.LawyerId)
            .NotEmpty()
            .WithMessage("Lawyer ID is required.");
    }
}
