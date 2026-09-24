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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransactionMetricsRow");

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
