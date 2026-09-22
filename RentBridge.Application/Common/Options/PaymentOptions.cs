namespace RentBridge.Application.Common.Options;

/// <summary>
/// Escrow/payment provider configuration. Provider "paystack" requires real
/// (non-placeholder) keys — startup fails fast otherwise so no payment flow
/// runs with a blank secret.
/// </summary>
public sealed class PaymentOptions
{
    public const string SectionName = "Payment";

    public string Provider { get; set; } = "paystack";

    public PaystackSection Paystack { get; set; } = new();

    public decimal PlatformCommissionRate { get; set; } = 8m;
    public decimal LegalFeeRate { get; set; } = 2m;

    public sealed class PaystackSection
    {
        public string BaseUrl { get; set; } = "https://api.paystack.co";
        public string SecretKey { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
        public string CallbackUrl { get; set; } = string.Empty;
    }

    public void GuardValid()
    {
        if (!string.Equals(Provider, "paystack", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Payment:Provider must be 'paystack' (got '{Provider}').");

        if (string.IsNullOrWhiteSpace(Paystack.SecretKey)
            || Paystack.SecretKey.Contains("REPLACE_ME", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "Payment:Paystack:SecretKey must be a real Paystack key. " +
                "Add it to appsettings.development.json (or the Payment__Paystack__SecretKey env var) " +
                "before the escrow phase can run.");
    }
}