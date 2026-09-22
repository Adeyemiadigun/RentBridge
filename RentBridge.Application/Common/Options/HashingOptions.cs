namespace RentBridge.Application.Common.Options;

/// <summary>
/// Keying material for agreement content hashing. The key is the shared secret
/// that proves agreement hash values were produced by this platform (HMAC).
/// Keep it stable for the lifetime of stored agreements: rotating it invalidates
/// every previously pinned content hash.
/// </summary>
public sealed class HashingOptions
{
    public const string SectionName = "Hashing";

    public string SecretKey { get; set; } = string.Empty;
}