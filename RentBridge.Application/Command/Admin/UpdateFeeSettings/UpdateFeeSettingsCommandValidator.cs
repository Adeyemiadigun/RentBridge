using FluentValidation;

namespace RentBridge.Application.Command.Admin;

public sealed class UpdateFeeSettingsCommandValidator : AbstractValidator<UpdateFeeSettingsCommand>
{
    public UpdateFeeSettingsCommandValidator()
    {
        RuleFor(x => x.PlatformCommissionRate)
            .GreaterThanOrEqualTo(0).LessThan(100)
            .WithMessage("Commission rate must be between 0 and 100.");
        RuleFor(x => x.LegalFeeRate)
            .GreaterThanOrEqualTo(0).LessThan(100)
            .WithMessage("Legal fee rate must be between 0 and 100.");
        RuleFor(x => x)
            .Must(x => x.PlatformCommissionRate + x.LegalFeeRate < 100)
            .WithMessage("Commission plus legal fee must be below 100%.");
    }
}