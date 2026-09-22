using FluentValidation;

namespace RentBridge.Application.Command.Payments;

public sealed class HandlePaymentWebhookCommandValidator : AbstractValidator<HandlePaymentWebhookCommand>
{
    public HandlePaymentWebhookCommandValidator()
    {
        RuleFor(x => x.RawBody)
            .NotEmpty()
            .WithMessage("Webhook body is required.");
        RuleFor(x => x.Signature)
            .NotEmpty()
            .WithMessage("Webhook signature header is required.");
    }
}