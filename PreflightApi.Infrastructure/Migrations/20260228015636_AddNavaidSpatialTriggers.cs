using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PreflightApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNavaidSpatialTriggers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Trigger function to auto-compute location from lat/lon
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION update_navaid_location()
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

            migrationBuilder.Sql(@"
                CREATE TRIGGER trg_update_navaid_location
                BEFORE INSERT OR UPDATE OF lat_decimal, long_decimal
                ON navaids
                FOR EACH ROW
                EXECUTE FUNCTION update_navaid_location();
            ");

            // Trigger function to auto-compute tacan_dme_location from tacan lat/lon
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION update_navaid_tacan_dme_location()
                RETURNS TRIGGER AS $$
                BEGIN
                    IF NEW.tacan_dme_lat_decimal IS NOT NULL AND NEW.tacan_dme_long_decimal IS NOT NULL THEN
                        NEW.tacan_dme_location := ST_SetSRID(ST_MakePoint(NEW.tacan_dme_long_decimal::double precision, NEW.tacan_dme_lat_decimal::double precision), 4326)::geography;
                    ELSE
                        NEW.tacan_dme_location := NULL;
                    END IF;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
            ");

            migrationBuilder.Sql(@"
                CREATE TRIGGER trg_update_navaid_tacan_dme_location
                BEFORE INSERT OR UPDATE OF tacan_dme_lat_decimal, tacan_dme_long_decimal
                ON navaids
                FOR EACH ROW
                EXECUTE FUNCTION update_navaid_tacan_dme_location();
            ");

            // Backfill existing rows
            migrationBuilder.Sql(@"
                UPDATE navaids
                SET location = ST_SetSRID(ST_MakePoint(long_decimal::double precision, lat_decimal::double precision), 4326)::geography
                WHERE lat_decimal IS NOT NULL AND long_decimal IS NOT NULL;
            ");

            migrationBuilder.Sql(@"
                UPDATE navaids
                SET tacan_dme_location = ST_SetSRID(ST_MakePoint(tacan_dme_long_decimal::double precision, tacan_dme_lat_decimal::double precision), 4326)::geography
                WHERE tacan_dme_lat_decimal IS NOT NULL AND tacan_dme_long_decimal IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_update_navaid_location ON navaids;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS update_navaid_location();");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_update_navaid_tacan_dme_location ON navaids;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS update_navaid_tacan_dme_location();");

            migrationBuilder.Sql("UPDATE navaids SET location = NULL;");
            migrationBuilder.Sql("UPDATE navaids SET tacan_dme_location = NULL;");
        }
    }
}
