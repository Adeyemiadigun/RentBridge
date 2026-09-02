using RentBridge.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

public class RefreshToken: Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;      // SHA-256 of raw token
    public DateTimeOffset IssuedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public bool Revoked { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }
    public string? CreatedByIp { get; private set; }
    public string? UserAgent { get; private set; }

    private RefreshToken() { }
    public RefreshToken(Guid userId, string tokenHash)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = tokenHash;
        IssuedAtUtc = DateTimeOffset.UtcNow;
        ExpiresAtUtc = DateTime.UtcNow.AddDays(7);
        Revoked = false;


    }
    [NotMapped]
    public bool IsActive => !Revoked && DateTime.UtcNow < ExpiresAtUtc;

    public void Revoke(string? replacedByTokenHash = null)
    {
        Revoked = true;
        RevokedAtUtc = DateTime.UtcNow;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
