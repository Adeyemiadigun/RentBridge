using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RentBridge.Application.Common.Options;
using RentBridge.Application.Common.Payments;
using RentBridge.Domain.Common;

namespace RentBridge.Infrastructure.Services;

/// <summary>
/// Paystack implementation of IEscrowProvider. Amounts travel in minor units
/// (kobo); JSON uses snake_case as Paystack returns it. Webhooks are verified
/// with an HMAC-SHA-512 over the raw request body (x-paystack-signature).
/// </summary>
public sealed class PaystackEscrowProvider(
    HttpClient httpClient,
    PaymentOptions options) : IEscrowProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<Result<PaymentInitiationResult>> InitializeAsync(
        PaymentInitiationRequest request,
        CancellationToken cancellationToken)
    {
        var body = new Dictionary<string, object?>
        {
            ["email"] = request.PayerEmail,
            ["amount"] = ToMinorUnits(request.Amount),
            ["reference"] = request.Reference,
            ["callback_url"] = options.Paystack.CallbackUrl,
        };

        using var response = await httpClient.PostAsJsonAsync(
            $"{Base()}/transaction/initialize", body, JsonOptions, cancellationToken);

        var payload = await DeserializeAsync<InitializeResponse>(response, cancellationToken);
        if (!response.IsSuccessStatusCode || payload is not { Status: true })
        {
            return Result<PaymentInitiationResult>.Fail(payload?.Message ?? "Paystack transaction initialization failed.");
        }
        if (string.IsNullOrWhiteSpace(payload.Data?.AuthorizationUrl))
        {
            return Result<PaymentInitiationResult>.Fail("Paystack returned no authorization URL.");
        }

        return Result<PaymentInitiationResult>.Ok(new PaymentInitiationResult(payload.Data.AuthorizationUrl));
    }

    public async Task<Result<PaymentNotification>> VerifyAndParseAsync(
        string rawBody,
        string signatureHeader,
        CancellationToken cancellationToken)
    {
        // Paystack signs webhooks with the Secret Key (no separate webhook secret
        // exists); ResolvedWebhookSecret falls back to it when unset.
        var webhookSecret = options.Paystack.ResolvedWebhookSecret;
        if (string.IsNullOrWhiteSpace(webhookSecret) || string.IsNullOrWhiteSpace(signatureHeader))
        {
            return Result<PaymentNotification>.Fail("Paystack webhook signature cannot be verified.");
        }

        var expected = ComputeHmacSha512Hex(webhookSecret, rawBody);
        if (!FixedTimeEquals(expected, signatureHeader))
        {
            return Result<PaymentNotification>.Fail("Invalid Paystack webhook signature.");
        }

        using var document = JsonDocument.Parse(rawBody);
        var root = document.RootElement;
        var eventName = root.GetProperty("event").GetString();
        var data = root.TryGetProperty("data", out var dataElement) ? dataElement : default;

        var reference = data.TryGetProperty("reference", out var refEl) ? refEl.GetString() : null;
        var status = data.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : null;
        var amount = data.TryGetProperty("amount", out var amountEl) ? amountEl.GetInt64() : (long?)null;

        if (string.IsNullOrWhiteSpace(reference))
        {
            return Result<PaymentNotification>.Fail("Webhook payload has no payment reference.");
        }

        var kind = eventName switch
        {
            "transfer.success" => PaymentEventKind.TransferSuccess,
            "transfer.failed" => PaymentEventKind.TransferFailed,
            "transfer.reversed" => PaymentEventKind.TransferReversed,
            "charge.success" => PaymentEventKind.ChargeSuccess,
            "charge.failed" => PaymentEventKind.ChargeFailed,
            _ => status == "success" ? PaymentEventKind.ChargeSuccess : PaymentEventKind.ChargeFailed,
        };

        var paymentStatus = kind is PaymentEventKind.ChargeSuccess or PaymentEventKind.TransferSuccess
            ? PaymentStatus.Paid
            : PaymentStatus.Failed;

        decimal? paidMinor = amount is null ? null : FromMinorUnits(amount.Value);

        return Result<PaymentNotification>.Ok(
            new PaymentNotification(reference, paymentStatus, paidMinor, kind));
    }

    public async Task<Result<TransferResult>> TransferSplitAsync(
        SplitTransferRequest request,
        CancellationToken cancellationToken)
    {
        var body = new Dictionary<string, object?>
        {
            ["source"] = "balance",
            ["amount"] = ToMinorUnits(request.LandlordPayout),
            ["recipient"] = request.RecipientCode,
            ["reference"] = request.Reference,
        };

        using var response = await httpClient.PostAsJsonAsync(
            $"{Base()}/transfer", body, JsonOptions, cancellationToken);

        var payload = await DeserializeAsync<TransferResponse>(response, cancellationToken);
        if (!response.IsSuccessStatusCode || payload is not { Status: true })
        {
            return Result<TransferResult>.Fail(payload?.Message ?? "Paystack transfer failed.");
        }

        return Result<TransferResult>.Ok(
            new TransferResult(payload.Data?.Reference ?? request.Reference, payload.Data?.TransferCode ?? "Initiated"));
    }

    public async Task<Result<TransferResult>> GetTransferStatusAsync(
        string reference,
        CancellationToken cancellationToken)
    {
        var url = $"{Base()}/transfer/verify/{Uri.EscapeDataString(reference)}";
        using var response = await httpClient.GetAsync(url, cancellationToken);

        var payload = await DeserializeAsync<TransferResponse>(response, cancellationToken);
        if (!response.IsSuccessStatusCode || payload is not { Status: true } || payload.Data is null)
        {
            return Result<TransferResult>.Fail(payload?.Message ?? "Paystack transfer was not found.");
        }

        return Result<TransferResult>.Ok(
            new TransferResult(payload.Data.Reference ?? reference, payload.Data.Status ?? "unknown"));
    }

    public async Task<Result<IReadOnlyList<BankInfo>>> ListBanksAsync(
        string country,
        string currency,
        CancellationToken cancellationToken)
    {
        var url = $"{Base()}/bank?country={Uri.EscapeDataString(country)}&currency={Uri.EscapeDataString(currency)}";
        var payload = await GetAsync<BankListResponse>(url, cancellationToken);
        if (payload is not { Status: true } || payload.Data is null)
        {
            return Result<IReadOnlyList<BankInfo>>.Fail(payload?.Message ?? "Could not load banks from Paystack.");
        }

        var banks = payload.Data
            .Where(b => !string.IsNullOrWhiteSpace(b.Code) && !string.IsNullOrWhiteSpace(b.Name))
            .Select(b => new BankInfo(b.Code, b.Name, b.Slug))
            .ToList();

        return Result<IReadOnlyList<BankInfo>>.Ok(banks);
    }

    public async Task<Result<ResolvedAccount>> ResolveAccountAsync(
        string accountNumber,
        string bankCode,
        CancellationToken cancellationToken)
    {
        var url = $"{Base()}/bank/resolve?account_number={Uri.EscapeDataString(accountNumber)}&bank_code={Uri.EscapeDataString(bankCode)}";
        var payload = await GetAsync<ResolveResponse>(url, cancellationToken);
        if (payload is not { Status: true } || payload.Data is null || string.IsNullOrWhiteSpace(payload.Data.AccountName))
        {
            return Result<ResolvedAccount>.Fail(payload?.Message ?? "Could not resolve the bank account.");
        }

        return Result<ResolvedAccount>.Ok(
            new ResolvedAccount(payload.Data.AccountNumber, payload.Data.AccountName));
    }

    public async Task<Result<TransferRecipient>> CreateTransferRecipientAsync(
        CreateRecipientRequest request,
        CancellationToken cancellationToken)
    {
        var body = new Dictionary<string, object?>
        {
            ["type"] = "nuban",
            ["name"] = request.Name,
            ["account_number"] = request.AccountNumber,
            ["bank_code"] = request.BankCode,
            ["currency"] = request.Currency,
        };

        using var response = await httpClient.PostAsJsonAsync(
            $"{Base()}/transferrecipient", body, JsonOptions, cancellationToken);

        var payload = await DeserializeAsync<RecipientResponse>(response, cancellationToken);
        if (!response.IsSuccessStatusCode || payload is not { Status: true } || string.IsNullOrWhiteSpace(payload.Data?.RecipientCode))
        {
            return Result<TransferRecipient>.Fail(payload?.Message ?? "Could not create a transfer recipient.");
        }

        return Result<TransferRecipient>.Ok(new TransferRecipient(payload.Data!.RecipientCode));
    }

    private async Task<TRoot?> GetAsync<TRoot>(string url, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(url, cancellationToken);
        return await DeserializeAsync<TRoot>(response, cancellationToken);
    }

    private async Task<TRoot?> DeserializeAsync<TRoot>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            return JsonSerializer.Deserialize<TRoot>(body, JsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private string Base() => options.Paystack.BaseUrl.TrimEnd('/');

    private long ToMinorUnits(decimal amount) => (long)decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);

    private decimal FromMinorUnits(long minor) => minor / 100m;

    private static string ComputeHmacSha512Hex(string secret, string body)
    {
        var hash = HMACSHA512.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(body));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var left = Encoding.ASCII.GetBytes(a);
        var right = Encoding.ASCII.GetBytes(b);
        if (left.Length != right.Length) return false;
        return CryptographicOperations.FixedTimeEquals(left, right);
    }

    private sealed record InitializeResponse(bool Status, InitializeData? Data, string? Message);
    private sealed record InitializeData(string AuthorizationUrl, string Reference);
    private sealed record TransferResponse(bool Status, TransferData? Data, string? Message);
    private sealed record TransferData(string? Reference, string? TransferCode, string? Status);
    private sealed record BankListResponse(bool Status, List<BankData>? Data, string? Message);
    private sealed record BankData(string Name, string Code, string? Slug);
    private sealed record ResolveResponse(bool Status, ResolveData? Data, string? Message);
    private sealed record ResolveData(string AccountNumber, string AccountName);
    private sealed record RecipientResponse(bool Status, RecipientData? Data, string? Message);
    private sealed record RecipientData(string RecipientCode);
}