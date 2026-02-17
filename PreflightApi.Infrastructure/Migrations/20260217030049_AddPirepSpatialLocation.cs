using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace PreflightApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPirepSpatialLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_pirep_latitude_longitude",
                table: "pirep");

            migrationBuilder.AddColumn<Point>(
                name: "location",
                table: "pirep",
                type: "geography(Point, 4326)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_pirep_location",
                table: "pirep",
                column: "location")
                .Annotation("Npgsql:IndexMethod", "gist");

            // Trigger function to auto-compute location from lat/lon
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION update_pirep_location()
                RETURNS TRIGGER AS $$
                BEGIN
                    IF NEW.latitude IS NOT NULL AND NEW.longitude IS NOT NULL THEN
                        NEW.location := ST_SetSRID(ST_MakePoint(NEW.longitude::double precision, NEW.latitude::double precision), 4326)::geography;
                    ELSE
                        NEW.location := NULL;
                    END IF;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
            ");

            // Trigger fires before insert or update of lat/lon columns
            migrationBuilder.Sql(@"
                CREATE TRIGGER trg_update_pirep_location
                BEFORE INSERT OR UPDATE OF latitude, longitude
                ON pirep
                FOR EACH ROW
                EXECUTE FUNCTION update_pirep_location();
            ");

            // Backfill existing rows
            migrationBuilder.Sql(@"
                UPDATE pirep
                SET location = ST_SetSRID(ST_MakePoint(longitude::double precision, latitude::double precision), 4326)::geography
                WHERE latitude IS NOT NULL AND longitude IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_update_pirep_location ON pirep;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS update_pirep_location();");

            migrationBuilder.DropIndex(
                name: "IX_pirep_location",
                table: "pirep");

            migrationBuilder.DropColumn(
                name: "location",
                table: "pirep");

            migrationBuilder.CreateIndex(
                name: "IX_pirep_latitude_longitude",
                table: "pirep",
                columns: new[] { "latitude", "longitude" });
        }
    }
}
