using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace RentBridge.Application.Command.Listing
{
    public class SubmitListingCommandValidator : AbstractValidator<SubmitListingCommand>
    {
        public SubmitListingCommandValidator()
        {
            RuleFor(x => x.ListingId)
                .NotEmpty()
                .WithMessage("Listing ID is required.")
                .Must(x => Guid.TryParse(x.ToString(), out _))
                .WithMessage("Invalid Listing ID format.");
        }
    }
}
