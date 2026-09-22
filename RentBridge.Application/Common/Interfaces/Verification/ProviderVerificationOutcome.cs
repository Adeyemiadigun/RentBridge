namespace RentBridge.Application.Common.Interfaces.Verification;

/// <summary>Provider-agnostic verdict from a synchronous verification call.</summary>
public sealed record ProviderVerificationOutcome(
    bool Passed,
    double Confidence,
    string ProviderRef,
    DateTimeOffset CompletedAt);
