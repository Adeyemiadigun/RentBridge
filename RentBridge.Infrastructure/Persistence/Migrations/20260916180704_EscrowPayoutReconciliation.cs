using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentBridge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EscrowPayoutReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "payout_started_at",
                table: "escrow_payments",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "payout_started_at",
                table: "escrow_payments");
        }
    }
}
