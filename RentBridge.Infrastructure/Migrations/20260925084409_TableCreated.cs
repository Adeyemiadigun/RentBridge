using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RentBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TableCreated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    target_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    target_id = table.Column<Guid>(type: "uuid", nullable: true),
                    details = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "kyc_verifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    nin = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    outcome_provider = table.Column<string>(type: "text", nullable: true),
                    outcome_provider_ref = table.Column<string>(type: "text", nullable: true),
                    outcome_result = table.Column<bool>(type: "boolean", nullable: true),
                    outcome_completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kyc_verifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "leases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LandlordUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedLawyerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IdentityGatePassed = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    InspectionGatePassed = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LegalGatePassed = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Agreement_Id = table.Column<Guid>(type: "uuid", nullable: false),
                    agreement_content_hash = table.Column<string>(type: "text", nullable: true),
                    agreement_certifying_lawyer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    agreement_certified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    landlord_payout_recipient_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ledger_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    lease_id = table.Column<Guid>(type: "uuid", nullable: false),
                    escrow_payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    landlord_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    direction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    attempt = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ledger_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "listings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    price_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    price_currency = table.Column<string>(type: "text", nullable: false),
                    listing_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Rent"),
                    payment_plan = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Outright"),
                    caution_fee_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    caution_fee_currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    other_expenses = table.Column<string>(type: "text", nullable: true),
                    real_house_fee_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    real_house_fee_currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    agent_fee_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    agent_fee_currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CoverImageKey = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "platform_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform_commission_rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    legal_fee_rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platform_settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "properties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    street = table.Column<string>(type: "text", nullable: false),
                    city = table.Column<string>(type: "text", nullable: false),
                    area = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<string>(type: "text", nullable: false),
                    property_type = table.Column<string>(type: "text", nullable: true),
                    bedrooms = table.Column<int>(type: "integer", nullable: false),
                    bathrooms = table.Column<int>(type: "integer", nullable: false),
                    available_from = table.Column<string>(type: "text", nullable: true),
                    amenities = table.Column<string>(type: "jsonb", nullable: false),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    VerificationLawyerId = table.Column<Guid>(type: "uuid", nullable: true),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_properties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IssuedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Revoked = table.Column<bool>(type: "boolean", nullable: false),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedByIp = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransactionMetricsRow",
                columns: table => new
                {
                    Period = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Funded = table.Column<decimal>(type: "numeric", nullable: false),
                    PaidOut = table.Column<decimal>(type: "numeric", nullable: false),
                    Commission = table.Column<decimal>(type: "numeric", nullable: false),
                    LegalFees = table.Column<decimal>(type: "numeric", nullable: false),
                    FailedAttempts = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    phone = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    IdentityVerified = table.Column<bool>(type: "boolean", nullable: false),
                    KycVerificationId = table.Column<Guid>(type: "uuid", nullable: true),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    PasswordSalt = table.Column<string>(type: "text", nullable: false),
                    bar_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    lawyer_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    LawyerProfile_LastAssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    payout_provider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    payout_recipient_code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    payout_bank_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    payout_bank_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    payout_account_last4 = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    payout_account_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    payout_account_active = table.Column<bool>(type: "boolean", nullable: true),
                    payout_account_verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "agreement_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AgreementId = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    terms_json = table.Column<string>(type: "text", nullable: false),
                    content_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    drafted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agreement_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_agreement_documents_leases_AgreementId",
                        column: x => x.AgreementId,
                        principalTable: "leases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "agreement_signatures",
                columns: table => new
                {
                    AgreementId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Party = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    image_hash = table.Column<string>(type: "text", nullable: false),
                    signed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ip_address = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agreement_signatures", x => new { x.AgreementId, x.Id });
                    table.ForeignKey(
                        name: "FK_agreement_signatures_leases_AgreementId",
                        column: x => x.AgreementId,
                        principalTable: "leases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "escrow_payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LeaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    gross_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    gross_currency = table.Column<string>(type: "text", nullable: false),
                    platform_commission = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    platform_commission_currency = table.Column<string>(type: "text", nullable: false),
                    legal_fee_share = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    legal_fee_share_currency = table.Column<string>(type: "text", nullable: false),
                    landlord_payout = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    landlord_payout_currency = table.Column<string>(type: "text", nullable: false),
                    Reference = table.Column<string>(type: "text", nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CheckoutUrl = table.Column<string>(type: "text", nullable: true),
                    payout_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    payout_attempts = table.Column<int>(type: "integer", nullable: false),
                    payout_started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_escrow_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_escrow_payments_leases_LeaseId",
                        column: x => x.LeaseId,
                        principalTable: "leases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inspection_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreferredDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ScheduledDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    ProposedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RescheduleNote = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    LeaseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inspection_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inspection_requests_leases_LeaseId",
                        column: x => x.LeaseId,
                        principalTable: "leases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ownership_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileKey = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ownership_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ownership_documents_properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_agreement_documents_AgreementId",
                table: "agreement_documents",
                column: "AgreementId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_actor_user_id",
                table: "audit_logs",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_created_at",
                table: "audit_logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_escrow_payments_Reference",
                table: "escrow_payments",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_escrow_payments_UserId_IdempotencyKey",
                table: "escrow_payments",
                columns: new[] { "UserId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_escrow_payments_lease_active_payout",
                table: "escrow_payments",
                column: "LeaseId",
                unique: true,
                filter: "\"Status\" IN ('Releasing', 'Released')");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_requests_LeaseId",
                table: "inspection_requests",
                column: "LeaseId");

            migrationBuilder.CreateIndex(
                name: "IX_inspection_requests_TenantUserId",
                table: "inspection_requests",
                column: "TenantUserId");

            migrationBuilder.CreateIndex(
                name: "IX_kyc_verifications_UserId",
                table: "kyc_verifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_leases_LandlordUserId",
                table: "leases",
                column: "LandlordUserId");

            migrationBuilder.CreateIndex(
                name: "IX_leases_ListingId",
                table: "leases",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_leases_TenantUserId",
                table: "leases",
                column: "TenantUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ledger_entries_escrow_payment_id_type_attempt",
                table: "ledger_entries",
                columns: new[] { "escrow_payment_id", "type", "attempt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ledger_entries_landlord_user_id_occurred_at",
                table: "ledger_entries",
                columns: new[] { "landlord_user_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ledger_entries_tenant_user_id_occurred_at",
                table: "ledger_entries",
                columns: new[] { "tenant_user_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "IX_listings_OwnerUserId",
                table: "listings",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_listings_Status",
                table: "listings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ownership_documents_PropertyId",
                table: "ownership_documents",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_properties_is_verified",
                table: "properties",
                column: "is_verified");

            migrationBuilder.CreateIndex(
                name: "IX_properties_OwnerUserId",
                table: "properties",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_TokenHash",
                table: "refresh_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId",
                table: "refresh_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_phone",
                table: "users",
                column: "phone",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agreement_documents");

            migrationBuilder.DropTable(
                name: "agreement_signatures");

            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "escrow_payments");

            migrationBuilder.DropTable(
                name: "inspection_requests");

            migrationBuilder.DropTable(
                name: "kyc_verifications");

            migrationBuilder.DropTable(
                name: "ledger_entries");

            migrationBuilder.DropTable(
                name: "listings");

            migrationBuilder.DropTable(
                name: "ownership_documents");

            migrationBuilder.DropTable(
                name: "platform_settings");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "TransactionMetricsRow");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "leases");

            migrationBuilder.DropTable(
                name: "properties");
        }
    }
}
