using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace RentBridge.Application.Command.Listing
{
    public class PublishListingCommandValidator : AbstractValidator<PublishListingCommand>
    {
        public PublishListingCommandValidator()
        {
            RuleFor(x => x.ListingId)
                .NotEmpty()
                .WithMessage("Listing ID is required.")
                .Must(x => Guid.TryParse(x.ToString(), out _))
                .WithMessage("Invalid Listing ID format.");
        }
    }
}