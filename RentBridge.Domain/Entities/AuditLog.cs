using RentBridge.Domain.Common;

namespace RentBridge.Domain.Entities;

/// <summary>
/// Immutable record of a privileged (admin) action. Written by admin command
/// handlers so overrides and sensitive changes are always auditable.
/// </summary>
public class AuditLog : Entity<Guid>
{
    public Guid ActorUserId { get; private set; }
    public string Action { get; private set; }
    public string? TargetType { get; private set; }
    public Guid? TargetId { get; private set; }
    public string? Details { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private AuditLog() { }

    public AuditLog(
        Guid actorUserId,
        string action,
        string? targetType = null,
        Guid? targetId = null,
        string? details = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Audit action cannot be empty.", nameof(action));

        Id = Guid.NewGuid();
        ActorUserId = actorUserId;
        Action = action.Trim();
        TargetType = targetType;
        TargetId = targetId;
        Details = details;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}