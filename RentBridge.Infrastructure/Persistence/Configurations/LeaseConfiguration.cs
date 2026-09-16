using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentBridge.Domain.Aggregates;
using RentBridge.Domain.Enums;

namespace RentBridge.Infrastructure.Persistence.Configurations;

public class LeaseConfiguration : IEntityTypeConfiguration<Lease>
{
    public void Configure(EntityTypeBuilder<Lease> b)
    {
        b.ToTable("leases");
        b.HasKey(l => l.Id);

        b.HasIndex(l => l.TenantUserId);
        b.HasIndex(l => l.LandlordUserId);
        b.HasIndex(l => l.ListingId);

        b.Property<uint>("Version")
         .HasColumnType("xid")
         .IsRowVersion();

        b.Property(l => l.Status)
         .HasConversion<string>()
         .HasMaxLength(30);

        // owned Agreement — columns on the leases table
        b.OwnsOne(l => l.Agreement, a =>
        {
            a.WithOwner().HasForeignKey("LeaseId");

            a.Property(x => x.ContentHash).HasColumnName("agreement_content_hash");
            a.Property(x => x.CertifyingLawyerId).HasColumnName("agreement_certifying_lawyer_id");
            a.Property(x => x.CertifiedAt).HasColumnName("agreement_certified_at");

            // signatures — own table (SignatureRecord is a value object;
            // EF creates the shadow FK + key automatically)
            a.OwnsMany(x => x.Signatures, s =>
            {
                s.ToTable("agreement_signatures");
                s.WithOwner().HasForeignKey("AgreementId");

                s.Property(x => x.Party).HasConversion<string>().HasMaxLength(20);
                s.Property(x => x.ImageHash).HasColumnName("image_hash");
                s.Property(x => x.SignedAt).HasColumnName("signed_at");
                s.Property(x => x.IpAddress).HasColumnName("ip_address");
            });
        });

        // escrow payments — separate owned table
        b.OwnsMany(l => l.EscrowPayments, p =>
        {
            p.ToTable("escrow_payments");
            p.WithOwner().HasForeignKey("LeaseId");
            p.HasKey(p => p.Id);

            p.HasIndex(p => p.Reference).IsUnique();
            p.HasIndex(p => new { p.UserId, p.IdempotencyKey }).IsUnique();

            p.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);

            p.OwnsOne(x => x.GrossAmount, m =>
            {
                m.Property(v => v.Amount).HasColumnName("gross_amount");
                m.Property(v => v.Currency).HasColumnName("gross_currency");
            });

            p.OwnsOne(x => x.Split, s =>
            {
                s.OwnsOne(v => v.PlatformCommission, m =>
                {
                    m.Property(x => x.Amount).HasColumnName("platform_commission").HasPrecision(18, 2);
                    m.Property(x => x.Currency).HasColumnName("platform_commission_currency");
                });
                s.OwnsOne(v => v.LegalFeeShare, m =>
                {
                    m.Property(x => x.Amount).HasColumnName("legal_fee_share").HasPrecision(18, 2);
                    m.Property(x => x.Currency).HasColumnName("legal_fee_share_currency");
                });
                s.OwnsOne(v => v.LandlordPayout, m =>
                {
                    m.Property(x => x.Amount).HasColumnName("landlord_payout").HasPrecision(18, 2);
                    m.Property(x => x.Currency).HasColumnName("landlord_payout_currency");
                });
            });
            p.Navigation(x => x.Split).IsRequired(true);
        });

        // inspection requests — owned children, separate table
        b.OwnsMany(l => l.InspectionRequests, r =>
        {
            r.ToTable("inspection_requests");
            r.WithOwner().HasForeignKey("LeaseId");
            r.HasKey(r => r.Id);

            r.HasIndex(r => r.TenantUserId);

            r.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        });
    }
}

