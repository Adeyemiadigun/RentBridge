using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentBridge.Domain.Aggregates;

namespace RentBridge.Infrastructure.Persistence.Configurations;

public class PlatformSettingsConfiguration : IEntityTypeConfiguration<PlatformSettings>
{
    public void Configure(EntityTypeBuilder<PlatformSettings> b)
    {
        b.ToTable("platform_settings");
        b.HasKey(x => x.Id);

        b.Property(x => x.PlatformCommissionRate)
            .HasColumnName("platform_commission_rate")
            .HasPrecision(5, 2);
        b.Property(x => x.LegalFeeRate)
            .HasColumnName("legal_fee_rate")
            .HasPrecision(5, 2);
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}