using FluentValidation;

namespace RentBridge.Application.Command.Admin;

public sealed class CreateAdminCommandValidator : AbstractValidator<CreateAdminCommand>
{
    public CreateAdminCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("A valid email address is required.")
            .MaximumLength(255);

        RuleFor(x => x.Phone)
            .NotEmpty()
            .WithMessage("Phone number is required.")
            .Matches(@"^\+?[0-9]{10,15}$")
            .WithMessage("Phone number must be a valid international phone number.");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required.")
            .MinimumLength(2)
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required.")
            .MinimumLength(2)
            .MaximumLength(100);

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long.")
            .Matches(@"[A-Z]+")
            .WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]+")
            .WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]+")
            .WithMessage("Password must contain at least one number.")
            .Matches(@"[\W_]+")
            .WithMessage("Password must contain at least one special character.");
    }
}
