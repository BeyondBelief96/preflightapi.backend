using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace PreflightApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAirportSpatialLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_airports_lat_decimal_long_decimal",
                table: "airports");

            migrationBuilder.AddColumn<Point>(
                name: "location",
                table: "airports",
                type: "geography(Point, 4326)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_airports_location",
                table: "airports",
                column: "location")
                .Annotation("Npgsql:IndexMethod", "gist");

            // Trigger function to auto-compute location from lat/lon
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION update_airport_location()
                RETURNS TRIGGER AS $$
                BEGIN
                    IF NEW.lat_decimal IS NOT NULL AND NEW.long_decimal IS NOT NULL THEN
                        NEW.location := ST_SetSRID(ST_MakePoint(NEW.long_decimal::double precision, NEW.lat_decimal::double precision), 4326)::geography;
                    ELSE
                        NEW.location := NULL;
                    END IF;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
            ");

            // Trigger fires before insert or update of lat/lon columns
            migrationBuilder.Sql(@"
                CREATE TRIGGER trg_update_airport_location
                BEFORE INSERT OR UPDATE OF lat_decimal, long_decimal
                ON airports
                FOR EACH ROW
                EXECUTE FUNCTION update_airport_location();
            ");

            // Backfill existing rows
            migrationBuilder.Sql(@"
                UPDATE airports
                SET location = ST_SetSRID(ST_MakePoint(long_decimal::double precision, lat_decimal::double precision), 4326)::geography
                WHERE lat_decimal IS NOT NULL AND long_decimal IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_update_airport_location ON airports;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS update_airport_location();");

            migrationBuilder.DropIndex(
                name: "IX_airports_location",
                table: "airports");

            migrationBuilder.DropColumn(
                name: "location",
                table: "airports");

            migrationBuilder.CreateIndex(
                name: "IX_airports_lat_decimal_long_decimal",
                table: "airports",
                columns: new[] { "lat_decimal", "long_decimal" });
        }
    }
}
