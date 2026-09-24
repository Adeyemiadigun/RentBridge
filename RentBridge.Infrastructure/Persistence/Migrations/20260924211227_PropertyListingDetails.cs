using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentBridge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PropertyListingDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "amenities",
                table: "properties",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "available_from",
                table: "properties",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "bathrooms",
                table: "properties",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "bedrooms",
                table: "properties",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "property_type",
                table: "properties",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "agent_fee_amount",
                table: "listings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "agent_fee_currency",
                table: "listings",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "caution_fee_amount",
                table: "listings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "caution_fee_currency",
                table: "listings",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "listing_type",
                table: "listings",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Rent");

            migrationBuilder.AddColumn<string>(
                name: "other_expenses",
                table: "listings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_plan",
                table: "listings",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Outright");

            migrationBuilder.AddColumn<decimal>(
                name: "real_house_fee_amount",
                table: "listings",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "real_house_fee_currency",
                table: "listings",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            // TransactionMetricsRow is a keyless SqlQueryRaw<> projection (LedgerRepository).
            // A physical table with this name already exists on the production database from an
            // earlier deploy, so create it idempotently rather than via CreateTable.
            migrationBuilder.Sql(
                "CREATE TABLE IF NOT EXISTS \"TransactionMetricsRow\" (\n" +
                "    \"Period\" timestamp with time zone NOT NULL,\n" +
                "    \"Funded\" numeric NOT NULL,\n" +
                "    \"PaidOut\" numeric NOT NULL,\n" +
                "    \"Commission\" numeric NOT NULL,\n" +
                "    \"LegalFees\" numeric NOT NULL,\n" +
                "    \"FailedAttempts\" bigint NOT NULL\n" +
                ");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"TransactionMetricsRow\";");

            migrationBuilder.DropColumn(
                name: "amenities",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "available_from",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "bathrooms",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "bedrooms",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "property_type",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "agent_fee_amount",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "agent_fee_currency",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "caution_fee_amount",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "caution_fee_currency",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "listing_type",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "other_expenses",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "payment_plan",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "real_house_fee_amount",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "real_house_fee_currency",
                table: "listings");
        }
    }
}
