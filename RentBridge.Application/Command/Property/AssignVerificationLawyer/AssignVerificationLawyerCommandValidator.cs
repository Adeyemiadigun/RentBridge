using FluentValidation;

namespace RentBridge.Application.Command.Property;

public class AssignVerificationLawyerCommandValidator : AbstractValidator<AssignVerificationLawyerCommand>
{
    public AssignVerificationLawyerCommandValidator()
    {
        RuleFor(x => x.PropertyId).NotEmpty();
        RuleFor(x => x.LawyerId).NotEmpty();
    }
}