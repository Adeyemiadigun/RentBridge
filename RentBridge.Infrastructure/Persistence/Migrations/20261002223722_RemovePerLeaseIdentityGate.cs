using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentBridge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemovePerLeaseIdentityGate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdentityGatePassed",
                table: "leases");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "IdentityVerifiedAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdentityVerifiedAt",
                table: "users");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "IdentityGatePassed",
                table: "leases",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
