using RentBridge.Domain.Common;

namespace RentBridge.Application.Common.Payments;

public sealed record PaymentInitiationRequest(
    Guid PaymentId,
    string Reference,
    string Currency,
    decimal Amount,
    string PayerEmail,
    string CallbackUrl);

public sealed record PaymentInitiationResult(string CheckoutUrl);

public sealed record PaymentNotification(string Reference, PaymentStatus Status, decimal? AmountPaid);

public enum PaymentStatus
{
    Paid,
    Failed,
}

public sealed record SplitTransferRequest(
    string Reference,
    string Currency,
    decimal LandlordPayout,
    string RecipientCode);

public sealed record TransferResult(string ProviderReference, string Status);

/// <summary>
/// Payment-provider boundary so Paystack can be swapped. All callers deal only
/// with normalized DTOs and Result — never provider-specific payloads.
/// </summary>
public interface IEscrowProvider
{
    Task<Result<PaymentInitiationResult>> InitializeAsync(
        PaymentInitiationRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Verifies the provider's webhook signature and normalizes it to a
    /// PaymentNotification. Must be cryptographically safe (HMAC of raw body).
    /// </summary>
    Task<Result<PaymentNotification>> VerifyAndParseAsync(
        string rawBody,
        string signatureHeader,
        CancellationToken cancellationToken);

    Task<Result<TransferResult>> TransferSplitAsync(
        SplitTransferRequest request,
        CancellationToken cancellationToken);
}