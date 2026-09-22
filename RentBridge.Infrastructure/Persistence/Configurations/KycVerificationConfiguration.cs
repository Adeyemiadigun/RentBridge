using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentBridge.Domain.Aggregates;

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

        b.OwnsOne(k => k.Outcome, r =>
        {
            r.Property(x => x.Provider).HasColumnName("outcome_provider");
            r.Property(x => x.ProviderRef).HasColumnName("outcome_provider_ref");
            r.Property(x => x.Outcome).HasColumnName("outcome_result");
            r.Property(x => x.CompletedAt).HasColumnName("outcome_completed_at");
        });
    }
}