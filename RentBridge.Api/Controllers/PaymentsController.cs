using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentBridge.Application.Command.Payments;
using RentBridge.Application.Common.Payments;

namespace RentBridge.Api.Controllers;

/// <summary>
/// Payment-provider webhook ingress and checkout callback. Always answers 200 so
/// Paystack does not retry onto itself; actual failures are logged and surfaced
/// by the escrow status on the lease.
/// </summary>
[ApiController]
[ApiVersionNeutral]
[Route("api/payments/paystack/webhook")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status200OK)]
public sealed class PaymentsController(
    IMediator mediator,
    IEscrowProvider escrowProvider,
    IConfiguration configuration,
    ILogger<PaymentsController> logger) : ControllerBase
{
    private const string MobileScheme = "rentbridge";
    // Public origin of the hosted web client. Paystack returns the payer to this
    // API's checkout callback, which verifies the charge and forwards them here.
    // Override via App:WebBaseUrl for a different origin; the previous hardcoded
    // "https://app.rentbridge.com" does not resolve (DNS NXDOMAIN) and stranded
    // payers on a dead page after a successful payment.
    private const string DefaultWebBaseUrl = "https://rent-bridge-d9w4.onrender.com";

    private string WebBaseUrl =>
        configuration["App:WebBaseUrl"] is { Length: > 0 } configured
            ? configured.TrimEnd('/')
            : DefaultWebBaseUrl;

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

    /// <summary>
    /// Browser landing page after a Paystack checkout. Paystack redirects the
    /// payer here with query parameters (trxref, reference, leaseId, paymentId).
    /// Verifies the charge, then redirects back to the mobile app (via deep link)
    /// or the web app with a query parameter indicating success/failure.
    /// </summary>
    [HttpGet("/api/payments/paystack/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(
        string? trxref,
        string? reference,
        string? leaseId,
        string? paymentId,
        CancellationToken ct)
    {
        var refToVerify = reference ?? trxref;
        if (string.IsNullOrWhiteSpace(refToVerify))
        {
            logger.LogWarning("Paystack callback missing reference/trxref");
            return Redirect($"{WebBaseUrl}/dashboard?payment=error&error=no_reference");
        }

        // Verify the charge with Paystack
        var verifyResult = await escrowProvider.VerifyChargeAsync(refToVerify, ct);
        var isSuccess = verifyResult.IsSuccess && verifyResult.Value.Status == PaymentStatus.Paid;

        // Build redirect targets. The web app's agreement screen lives at
        // /dashboard/agreement/{leaseId}; the previous /agreements path had no
        // route and stranded payers on the not-found page.
        var paymentStatus = isSuccess ? "success" : "failed";
        var refParam = $"reference={Uri.EscapeDataString(refToVerify)}";
        var errorParam = isSuccess ? "" : $"&error={Uri.EscapeDataString(verifyResult.Error ?? "Payment was not successful")}";

        var mobileUrl = $"{MobileScheme}://payment/{paymentStatus}?{refParam}{errorParam}&leaseId={Uri.EscapeDataString(leaseId ?? "")}";
        var webPath = string.IsNullOrWhiteSpace(leaseId)
            ? "/dashboard"
            : $"/dashboard/agreement/{leaseId}";
        var webUrl = $"{WebBaseUrl}{webPath}?payment={paymentStatus}&{refParam}{errorParam}";

        // Prefer mobile deep link if the request looks like it came from the app,
        // otherwise send to the web app.
        var userAgent = Request.Headers["User-Agent"].ToString();
        var isMobile = userAgent.Contains("Mobile", StringComparison.OrdinalIgnoreCase) ||
                       userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
                       userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase) ||
                       userAgent.Contains("Expo", StringComparison.OrdinalIgnoreCase);

        var redirectUrl = isMobile ? mobileUrl : webUrl;

        logger.LogInformation(
            "Paystack callback for reference {Reference}: {Status} -> redirecting to {Url}",
            refToVerify,
            paymentStatus,
            redirectUrl);

        return Redirect(redirectUrl);
    }
}