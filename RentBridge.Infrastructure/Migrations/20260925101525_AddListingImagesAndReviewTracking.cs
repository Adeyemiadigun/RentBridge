using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddListingImagesAndReviewTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "verified_by_name",
                table: "properties",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "verified_by_role",
                table: "properties",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "verified_by_user_id",
                table: "properties",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                table: "ownership_documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "reviewed_at",
                table: "ownership_documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "verified_by_name",
                table: "ownership_documents",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "verified_by_role",
                table: "ownership_documents",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "verified_by_user_id",
                table: "ownership_documents",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "listing_images",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_listing_images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_listing_images_listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_listing_images_ListingId",
                table: "listing_images",
                column: "ListingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "listing_images");

            migrationBuilder.DropColumn(
                name: "verified_by_name",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "verified_by_role",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "verified_by_user_id",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "rejection_reason",
                table: "ownership_documents");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                table: "ownership_documents");

            migrationBuilder.DropColumn(
                name: "verified_by_name",
                table: "ownership_documents");

            migrationBuilder.DropColumn(
                name: "verified_by_role",
                table: "ownership_documents");

            migrationBuilder.DropColumn(
                name: "verified_by_user_id",
                table: "ownership_documents");
        }
    }
}
