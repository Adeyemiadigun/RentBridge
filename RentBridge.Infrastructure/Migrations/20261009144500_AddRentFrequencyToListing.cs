using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentBridge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRentFrequencyToListing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RentFrequency",
                table: "Listings",
                type: "integer",
                nullable: false,
                defaultValue: 4); // Annually = 4

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'rent_frequency') THEN
                        CREATE TYPE rent_frequency AS ENUM (
                            'Monthly',
                            'Quarterly',
                            'SemiAnnually',
                            'Annually'
                        );
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RentFrequency",
                table: "Listings");
            
            migrationBuilder.Sql("DROP TYPE IF EXISTS rent_frequency;");
        }
    }
}