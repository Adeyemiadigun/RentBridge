using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentBridge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EscrowPayoutGuardRails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_escrow_payments_LeaseId",
                table: "escrow_payments");

            migrationBuilder.CreateIndex(
                name: "ux_escrow_payments_lease_active_payout",
                table: "escrow_payments",
                column: "LeaseId",
                unique: true,
                filter: "\"Status\" IN ('Releasing', 'Released')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_escrow_payments_lease_active_payout",
                table: "escrow_payments");

            migrationBuilder.CreateIndex(
                name: "IX_escrow_payments_LeaseId",
                table: "escrow_payments",
                column: "LeaseId");
        }
    }
}
