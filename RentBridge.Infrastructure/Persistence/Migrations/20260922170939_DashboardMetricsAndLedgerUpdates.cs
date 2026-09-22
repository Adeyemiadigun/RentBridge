using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentBridge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DashboardMetricsAndLedgerUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }
    }
}
