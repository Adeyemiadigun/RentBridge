using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentBridge.Domain.Entities;

namespace RentBridge.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_logs");
        b.HasKey(x => x.Id);

        b.Property(x => x.ActorUserId).HasColumnName("actor_user_id");
        b.Property(x => x.Action).HasColumnName("action").HasMaxLength(100).IsRequired();
        b.Property(x => x.TargetType).HasColumnName("target_type").HasMaxLength(50);
        b.Property(x => x.TargetId).HasColumnName("target_id");
        b.Property(x => x.Details).HasColumnName("details");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");

        b.HasIndex(x => x.ActorUserId);
        b.HasIndex(x => x.CreatedAt);
    }
}