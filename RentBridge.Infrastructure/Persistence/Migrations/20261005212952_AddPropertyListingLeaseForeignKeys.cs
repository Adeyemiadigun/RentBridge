using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentBridge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyListingLeaseForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_listings_PropertyId",
                table: "listings",
                column: "PropertyId");

            migrationBuilder.AddForeignKey(
                name: "FK_leases_listings_ListingId",
                table: "leases",
                column: "ListingId",
                principalTable: "listings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_listings_properties_PropertyId",
                table: "listings",
                column: "PropertyId",
                principalTable: "properties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_leases_listings_ListingId",
                table: "leases");

            migrationBuilder.DropForeignKey(
                name: "FK_listings_properties_PropertyId",
                table: "listings");

            migrationBuilder.DropIndex(
                name: "IX_listings_PropertyId",
                table: "listings");
        }
    }
}
