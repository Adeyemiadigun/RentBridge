using RentBridge.Domain.Common;

namespace RentBridge.Domain.ValueObjects
{
    public sealed record VerificationResult
    {
        public string Provider { get; }
        public string ProviderRef { get; }
        public bool Outcome { get; }
        public DateTimeOffset CompletedAt { get; }

        private VerificationResult(string provider, string providerRef, bool outcome, DateTimeOffset completedAt)
        {
            Provider = provider;
            ProviderRef = providerRef;
            Outcome = outcome;
            CompletedAt = completedAt;
        }

        public static Result<VerificationResult> Create(
            string? provider,
            string? providerRef,
            bool outcome,
            DateTimeOffset? completedAt = null)
        {
            if (string.IsNullOrWhiteSpace(provider))
            {
                return Result<VerificationResult>.Fail("Verification provider cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(providerRef))
            {
                return Result<VerificationResult>.Fail("Provider reference ID cannot be empty.");
            }

            return Result<VerificationResult>.Ok(new VerificationResult(
                provider.Trim(),
                providerRef.Trim(),
                outcome,
                completedAt ?? DateTimeOffset.UtcNow
            ));
        }
    }
}
