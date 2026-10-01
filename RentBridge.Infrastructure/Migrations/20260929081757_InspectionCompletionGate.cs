using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InspectionCompletionGate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "inspection_requests",
                newName: "notes");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "actual_date",
                table: "inspection_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "completion_notes",
                table: "inspection_requests",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "actual_date",
                table: "inspection_requests");

            migrationBuilder.DropColumn(
                name: "completion_notes",
                table: "inspection_requests");

            migrationBuilder.RenameColumn(
                name: "notes",
                table: "inspection_requests",
                newName: "Notes");
        }
    }
}
