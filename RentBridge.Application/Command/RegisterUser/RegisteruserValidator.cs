using FluentValidation;
using RentBridge.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace RentBridge.Application.Command.RegisterUser
{
    public sealed class RegisterUserCommandValidator
    : AbstractValidator<registerUserCommand>
    {
        public RegisterUserCommandValidator()
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
                .MaximumLength(100)
                .WithMessage("First name must be between 2 and 100 characters.");
            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithMessage("Last name is required.")
                .MinimumLength(2)
                .MaximumLength(100)
                .WithMessage("Last name must be between 2 and 100 characters.");
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

            RuleFor(x => x.Role)
                .NotEmpty()
                .WithMessage("Role is required.")
                .Must(role => Enum.TryParse<UserRole>(role, true, out var parsedRole)
                              && Enum.IsDefined(parsedRole))
                .WithMessage("Role must be a valid user role.");

            RuleFor(x => x.BarNumber)
                .NotEmpty()
                .When(x => Enum.TryParse<UserRole>(x.Role, true, out var r) && r == UserRole.Lawyer)
                .WithMessage("Bar number is required for lawyer registration.");
        }


    }
}
