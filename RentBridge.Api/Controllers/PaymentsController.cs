using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentBridge.Application.Command.Payments;

namespace RentBridge.Api.Controllers;

/// <summary>
/// Payment-provider webhook ingress. Always answers 200 so Paystack does not
/// retry onto itself; actual failures are logged and surfaced by the escrow
/// status on the lease.
/// </summary>
[ApiController]
[ApiVersionNeutral]
[Route("api/payments/paystack/webhook")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status200OK)]
public sealed class PaymentsController(
    IMediator mediator,
    ILogger<PaymentsController> logger) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Handle(CancellationToken ct)
    {
        var rawBody = await new StreamReader(Request.Body).ReadToEndAsync(ct);
        Request.Headers.TryGetValue("x-paystack-signature", out var signature);

        var result = await mediator.Send(
            new HandlePaymentWebhookCommand(rawBody, signature.ToString()),
            ct);
        if (result.IsSuccess is false)
        {
            logger.LogWarning("Paystack webhook rejected: {Error}", result.Error);
        }

        return Ok(new { acknowledged = true });
    }
}