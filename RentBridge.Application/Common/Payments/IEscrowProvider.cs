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

public sealed record PaymentNotification(
    string Reference,
    PaymentStatus Status,
    decimal? AmountPaid,
    PaymentEventKind Kind = PaymentEventKind.ChargeSuccess);

public enum PaymentStatus
{
    Paid,
    Failed,
}

/// <summary>
/// Which provider event a webhook maps to. Charge events fund escrow; transfer
/// events confirm (or fail) the landlord payout that follows.
/// </summary>
public enum PaymentEventKind
{
    ChargeSuccess,
    ChargeFailed,
    TransferSuccess,
    TransferFailed,
    TransferReversed,
}

public sealed record BankInfo(string Code, string Name, string? Slug);

public sealed record ResolvedAccount(string AccountNumber, string AccountName);

public sealed record CreateRecipientRequest(
    string Name,
    string AccountNumber,
    string BankCode,
    string Currency);

public sealed record TransferRecipient(string RecipientCode);

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

    /// <summary>
    /// Looks up a transfer's authoritative status at the provider by reference.
    /// Used by the reconciliation sweep to resolve payouts that were claimed but
    /// never finalized by a webhook. Returns failure when the provider has no
    /// record of the transfer (i.e. it was never actually sent).
    /// </summary>
    Task<Result<TransferResult>> GetTransferStatusAsync(
        string reference,
        CancellationToken cancellationToken);

    /// <summary>Lists the banks available for payout for a country/currency.</summary>
    Task<Result<IReadOnlyList<BankInfo>>> ListBanksAsync(
        string country,
        string currency,
        CancellationToken cancellationToken);

    /// <summary>
    /// Name enquiry: resolves the account holder's name for a bank + account
    /// number. Used to verify an account before it is registered for payouts.
    /// </summary>
    Task<Result<ResolvedAccount>> ResolveAccountAsync(
        string accountNumber,
        string bankCode,
        CancellationToken cancellationToken);

    /// <summary>Creates (or reuses) the provider transfer recipient for an account.</summary>
    Task<Result<TransferRecipient>> CreateTransferRecipientAsync(
        CreateRecipientRequest request,
        CancellationToken cancellationToken);
}