namespace RentBridge.Domain.Aggregates;

/// <summary>
/// Immutable snapshot of the tenancy agreement content: a canonical serialized
/// terms payload plus its keyed hash (HMAC-SHA256, see Hashing:SecretKey).
/// Once certified/signed the hash is the pinned reference for tamper-evidence;
/// changes require a new (higher-version) document.
/// </summary>
public class AgreementDocument
{
    public Guid Id { get; private set; }
    public Guid AgreementId { get; private set; }
    public int Version { get; private set; }
    public string TermsJson { get; private set; }
    public string ContentHash { get; private set; }
    public DateTimeOffset DraftedAt { get; private set; }

    private AgreementDocument() { } // EF

    public AgreementDocument(Guid agreementId, int version, string termsJson, string contentHash)
    {
        Id = Guid.NewGuid();
        AgreementId = agreementId;
        Version = version;
        TermsJson = termsJson;
        ContentHash = contentHash;
        DraftedAt = DateTimeOffset.UtcNow;
    }
}