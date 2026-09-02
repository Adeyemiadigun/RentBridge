using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentBridge.Domain.Aggregates.Users;

namespace RentBridge.Infrastructure.Persistence.Configurations;

public class KycVerificationConfiguration : IEntityTypeConfiguration<KycVerification>
{
    public void Configure(EntityTypeBuilder<KycVerification> b)
    {
        b.ToTable("kyc_verifications");
        b.HasKey(k => k.Id);

        b.HasIndex(k => k.UserId);

        b.OwnsOne(k => k.Nin, n =>
            n.Property(x => x.Value).HasColumnName("nin"));

        b.OwnsOne(k => k.IdentityResult, r =>
        {
            r.Property(x => x.Provider).HasColumnName("identity_provider");
            r.Property(x => x.ProviderRef).HasColumnName("identity_provider_ref");
            r.Property(x => x.Outcome).HasColumnName("identity_outcome");
            r.Property(x => x.CompletedAt).HasColumnName("identity_completed_at");
        });

        b.OwnsOne(k => k.FacialResult, r =>
        {
            r.Property(x => x.Provider).HasColumnName("facial_provider");
            r.Property(x => x.ProviderRef).HasColumnName("facial_provider_ref");
            r.Property(x => x.Outcome).HasColumnName("facial_outcome");
            r.Property(x => x.CompletedAt).HasColumnName("facial_completed_at");
        });
    }
}
