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

        // A lease is only reachable through its listing. Declaring the relationship
        // makes the database enforce it and cascade, so deleting a property can no
        // longer strand leases behind (previously this FK did not exist at all).
        b.HasOne<Listing>()
            .WithMany()
            .HasForeignKey(l => l.ListingId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Property(l => l.LandlordPayoutRecipientCode)
            .HasColumnName("landlord_payout_recipient_code")
            .HasMaxLength(100);

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

            // document-based signing flow — source document the lawyer uploads
            a.Property(x => x.OriginalDocumentUrl).HasColumnName("agreement_original_document_url").HasMaxLength(1000);
            a.Property(x => x.OriginalDocumentFileName).HasColumnName("agreement_original_document_file_name").HasMaxLength(400);
            a.Property(x => x.OriginalDocumentUploadedAt).HasColumnName("agreement_original_document_uploaded_at");

            // the retained final agreement (tenant-signed copy with both signatures)
            a.Property(x => x.FinalDocumentUrl).HasColumnName("agreement_final_document_url").HasMaxLength(1000);
            a.Property(x => x.FinalDocumentFileName).HasColumnName("agreement_final_document_file_name").HasMaxLength(400);
            a.Property(x => x.FinalizedAt).HasColumnName("agreement_finalized_at");

            // per-party review progress, stored as columns on the leases table
            a.OwnsOne(x => x.LandlordReview, r =>
            {
                r.Property(p => p.Party).HasConversion<string>().HasMaxLength(20).HasColumnName("agreement_landlord_review_party");
                r.Property(p => p.DownloadedAt).HasColumnName("agreement_landlord_review_downloaded_at");
                r.Property(p => p.UploadedUrl).HasColumnName("agreement_landlord_review_uploaded_url").HasMaxLength(1000);
                r.Property(p => p.UploadedFileName).HasColumnName("agreement_landlord_review_uploaded_file_name").HasMaxLength(400);
                r.Property(p => p.UploadedAt).HasColumnName("agreement_landlord_review_uploaded_at");
                r.Property(p => p.SubmittedForReviewAt).HasColumnName("agreement_landlord_review_submitted_at");
                r.Property(p => p.ApprovedAt).HasColumnName("agreement_landlord_review_approved_at");
                r.Property(p => p.ApprovedByLawyerId).HasColumnName("agreement_landlord_review_approved_by_lawyer_id");
            });

            a.OwnsOne(x => x.TenantReview, r =>
            {
                r.Property(p => p.Party).HasConversion<string>().HasMaxLength(20).HasColumnName("agreement_tenant_review_party");
                r.Property(p => p.DownloadedAt).HasColumnName("agreement_tenant_review_downloaded_at");
                r.Property(p => p.UploadedUrl).HasColumnName("agreement_tenant_review_uploaded_url").HasMaxLength(1000);
                r.Property(p => p.UploadedFileName).HasColumnName("agreement_tenant_review_uploaded_file_name").HasMaxLength(400);
                r.Property(p => p.UploadedAt).HasColumnName("agreement_tenant_review_uploaded_at");
                r.Property(p => p.SubmittedForReviewAt).HasColumnName("agreement_tenant_review_submitted_at");
                r.Property(p => p.ApprovedAt).HasColumnName("agreement_tenant_review_approved_at");
                r.Property(p => p.ApprovedByLawyerId).HasColumnName("agreement_tenant_review_approved_by_lawyer_id");
            });

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

            // agreement files — own table. Every upload is retained as a row so a
            // superseded file is never overwritten; intermediate files are flagged
            // for deletion (pending_deletion_at) and purged once finalized.
            a.OwnsMany(x => x.Files, f =>
            {
                f.ToTable("agreement_files");
                f.WithOwner().HasForeignKey("AgreementId");
                f.HasKey(x => x.Id);

                // The domain assigns Id (Guid.NewGuid). Without ValueGeneratedNever
                // EF treats a non-default Guid as store-generated, assumes the row
                // already exists and emits UPDATE instead of INSERT.
                f.Property(x => x.Id).ValueGeneratedNever();

                f.Property(x => x.Kind).HasConversion<string>().HasMaxLength(30);
                f.Property(x => x.Url).HasColumnName("url").HasMaxLength(1000).IsRequired();
                f.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(400);
                f.Property(x => x.UploadedAt).HasColumnName("uploaded_at");
                f.Property(x => x.UploadedByUserId).HasColumnName("uploaded_by_user_id");
                f.Property(x => x.IsFinal).HasColumnName("is_final");
                f.Property(x => x.PendingDeletionAt).HasColumnName("pending_deletion_at");
            });

            // agreement document — canonical terms snapshot, separate table
            a.OwnsOne(x => x.Document, d =>
            {
                d.ToTable("agreement_documents");
                d.WithOwner().HasForeignKey("AgreementId");
                d.HasKey(d => d.Id);

                // The domain assigns Id (Guid.NewGuid) in the constructor. Without
                // ValueGeneratedNever EF treats a non-default Guid as store-generated,
                // assumes the row already exists and emits UPDATE instead of INSERT —
                // which matched 0 rows on the first compose. See
                // FK_agreement_documents_leases_AgreementId: AgreementId is leases.Id.
                d.Property(x => x.Id).ValueGeneratedNever();

                d.Property(x => x.Version).HasColumnName("version");
                d.Property(x => x.TermsJson).HasColumnName("terms_json").HasColumnType("text").IsRequired();
                d.Property(x => x.ContentHash).HasColumnName("content_hash").HasMaxLength(128).IsRequired();
                d.Property(x => x.DraftedAt).HasColumnName("drafted_at");
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

            // DB guard rail: at most one in-flight-or-completed payout per lease.
            // Retries stay legal because PayoutFailed is outside the filter.
            p.HasIndex(p => p.LeaseId)
                .HasDatabaseName("ux_escrow_payments_lease_active_payout")
                .IsUnique()
                .HasFilter("\"Status\" IN ('Releasing', 'Released')");

            p.Property(x => x.PayoutReference).HasColumnName("payout_reference").HasMaxLength(100);
            p.Property(x => x.PayoutAttempts).HasColumnName("payout_attempts");
            p.Property(x => x.PayoutStartedAt).HasColumnName("payout_started_at");

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

            // The aggregate assumes at most one *live* request per lease: a
            // Pending, Confirmed or ReschedulePending row is the one the
            // lifecycle acts on, and the lookups in Lease pick exactly one.
            // Without this the database would happily hold two and the domain
            // would have to cope. Filtered (not a plain unique index) because a
            // lease legitimately accumulates many terminal requests over its
            // life — Declined, Cancelled and Completed may all repeat.
            // LeaseId is a shadow property (configured via HasForeignKey above),
            // so the index is declared by name rather than by lambda.
            r.HasIndex("LeaseId")
                .IsUnique()
                .HasFilter("\"Status\" IN ('Pending', 'Confirmed', 'ReschedulePending')")
                .HasDatabaseName("IX_inspection_requests_LeaseId_live");

            // Completion fields — populated only by InspectionRequest.Complete.
            r.Property(x => x.ActualDate).HasColumnName("actual_date");
            r.Property(x => x.Notes).HasColumnName("notes");
            r.Property(x => x.CompletionNotes).HasColumnName("completion_notes");
        });
    }
}

