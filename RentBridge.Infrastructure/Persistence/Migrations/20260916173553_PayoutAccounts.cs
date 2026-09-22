using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentBridge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PayoutAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "payout_account_active",
                table: "users",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payout_account_last4",
                table: "users",
                type: "character varying(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payout_account_name",
                table: "users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "payout_account_verified_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payout_bank_code",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payout_bank_name",
                table: "users",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payout_provider",
                table: "users",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payout_recipient_code",
                table: "users",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "payout_attempts",
                table: "escrow_payments",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "payout_account_active",
                table: "users");

            migrationBuilder.DropColumn(
                name: "payout_account_last4",
                table: "users");

            migrationBuilder.DropColumn(
                name: "payout_account_name",
                table: "users");

            migrationBuilder.DropColumn(
                name: "payout_account_verified_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "payout_bank_code",
                table: "users");

            migrationBuilder.DropColumn(
                name: "payout_bank_name",
                table: "users");

            migrationBuilder.DropColumn(
                name: "payout_provider",
                table: "users");

            migrationBuilder.DropColumn(
                name: "payout_recipient_code",
                table: "users");

            migrationBuilder.DropColumn(
                name: "payout_attempts",
                table: "escrow_payments");
        }
    }
}
