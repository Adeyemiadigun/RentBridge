using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentBridge.Domain.Entities;

namespace RentBridge.Infrastructure.Persistence.Configurations;

public class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> b)
    {
        b.ToTable("ledger_entries");
        b.HasKey(x => x.Id);

        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.LeaseId).HasColumnName("lease_id");
        b.Property(x => x.EscrowPaymentId).HasColumnName("escrow_payment_id");
        b.Property(x => x.TenantUserId).HasColumnName("tenant_user_id");
        b.Property(x => x.LandlordUserId).HasColumnName("landlord_user_id");
        b.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(40).IsRequired();
        b.Property(x => x.Direction).HasColumnName("direction").HasConversion<string>().HasMaxLength(10).IsRequired();
        b.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2);
        b.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(8).IsRequired();
        b.Property(x => x.Reference).HasColumnName("reference").HasMaxLength(120);
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30).IsRequired();
        b.Property(x => x.Attempt).HasColumnName("attempt");
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(300).IsRequired();
        b.Property(x => x.OccurredAt).HasColumnName("occurred_at");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");

        // History is always read "my lines, newest first" for a tenant or owner.
        b.HasIndex(x => new { x.TenantUserId, x.OccurredAt });
        b.HasIndex(x => new { x.LandlordUserId, x.OccurredAt });

        // One line per event per attempt — makes replayed writes idempotent.
        b.HasIndex(x => new { x.EscrowPaymentId, x.Type, x.Attempt }).IsUnique();
    }
}
