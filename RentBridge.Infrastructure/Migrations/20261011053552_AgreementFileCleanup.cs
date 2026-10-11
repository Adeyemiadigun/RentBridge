using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgreementFileCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "agreement_final_document_file_name",
                table: "leases",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "agreement_final_document_url",
                table: "leases",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "agreement_finalized_at",
                table: "leases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "agreement_files",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    file_name = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    uploaded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_final = table.Column<bool>(type: "boolean", nullable: false),
                    pending_deletion_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AgreementId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agreement_files", x => x.Id);
                    table.ForeignKey(
                        name: "FK_agreement_files_leases_AgreementId",
                        column: x => x.AgreementId,
                        principalTable: "leases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_agreement_files_AgreementId",
                table: "agreement_files",
                column: "AgreementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agreement_files");

            migrationBuilder.DropColumn(
                name: "agreement_final_document_file_name",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "agreement_final_document_url",
                table: "leases");

            migrationBuilder.DropColumn(
                name: "agreement_finalized_at",
                table: "leases");
        }
    }
}
