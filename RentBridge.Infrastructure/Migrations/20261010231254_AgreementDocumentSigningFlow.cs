using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgreementDocumentSigningFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "agreement_landlord_review_approved_at",
                table: "leases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "agreement_landlord_review_approved_by_lawyer_id",
                table: "leases",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "agreement_landlord_review_downloaded_at",
                table: "leases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "agreement_landlord_review_party",
                table: "leases",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Landlord");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "agreement_landlord_review_submitted_at",
                table: "leases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "agreement_landlord_review_uploaded_at",
                table: "leases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "agreement_landlord_review_uploaded_file_name",
                table: "leases",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "agreement_landlord_review_uploaded_url",
                table: "leases",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "agreement_original_document_file_name",
                table: "leases",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "agreement_original_document_uploaded_at",
                table: "leases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "agreement_original_document_url",
                table: "leases",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "agreement_tenant_review_approved_at",
                table: "leases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "agreement_tenant_review_approved_by_lawyer_id",
                table: "leases",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "agreement_tenant_review_downloaded_at",
                table: "leases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "agreement_tenant_review_party",
                table: "leases",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Tenant");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "agreement_tenant_review_submitted_at",
                table: "leases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "agreement_tenant_review_uploaded_at",
                table: "leases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "agreement_tenant_review_uploaded_file_name",
                table: "leases",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "agreement_tenant_review_uploaded_url",
                table: "leases",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "agreement_landlord_review_approved_at",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "agreement_landlord_review_approved_by_lawyer_id",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "agreement_landlord_review_downloaded_at",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "agreement_landlord_review_party",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "agreement_landlord_review_submitted_at",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "agreement_landlord_review_uploaded_at",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "agreement_landlord_review_uploaded_file_name",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "agreement_landlord_review_uploaded_url",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "agreement_original_document_file_name",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "agreement_original_document_uploaded_at",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "agreement_original_document_url",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "agreement_tenant_review_approved_at",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "agreement_tenant_review_approved_by_lawyer_id",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "agreement_tenant_review_downloaded_at",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "agreement_tenant_review_party",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "agreement_tenant_review_submitted_at",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "agreement_tenant_review_uploaded_at",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "agreement_tenant_review_uploaded_file_name",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "agreement_tenant_review_uploaded_url",
                table: "leases");
        }
    }
}
