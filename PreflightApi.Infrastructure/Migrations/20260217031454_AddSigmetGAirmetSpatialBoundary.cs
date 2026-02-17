using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace PreflightApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSigmetGAirmetSpatialBoundary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Geometry>(
                name: "boundary",
                table: "sigmet",
                type: "geometry(Geometry, 4326)",
                nullable: true);

            migrationBuilder.AddColumn<Geometry>(
                name: "boundary",
                table: "gairmet",
                type: "geometry(Geometry, 4326)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_sigmet_boundary",
                table: "sigmet",
                column: "boundary")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_gairmet_boundary",
                table: "gairmet",
                column: "boundary")
                .Annotation("Npgsql:IndexMethod", "gist");

            // ── Sigmet trigger ──
            // The 'area' column is a JSONB array of areas, each with camelCase keys:
            // [{"numPoints":4,"points":[{"longitude":-97.0,"latitude":32.8}, ...]}, ...]
            // Build a polygon per area, then ST_Union them into one geometry.
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION update_sigmet_boundary()
                RETURNS TRIGGER AS $$
                DECLARE
                    area_elem jsonb;
                    points jsonb;
                    ring geometry;
                    poly geometry;
                    result geometry := NULL;
                BEGIN
                    IF NEW.area IS NOT NULL AND jsonb_typeof(NEW.area) = 'array' THEN
                        FOR area_elem IN SELECT * FROM jsonb_array_elements(NEW.area)
                        LOOP
                            points := area_elem->'points';
                            IF points IS NOT NULL AND jsonb_array_length(points) >= 3 THEN
                                -- Build a closed ring: all points + repeat the first point
                                ring := ST_MakeLine(
                                    ARRAY(
                                        SELECT ST_MakePoint(
                                            (p->>'longitude')::double precision,
                                            (p->>'latitude')::double precision
                                        )
                                        FROM jsonb_array_elements(points) AS p
                                    )
                                    || ARRAY[
                                        ST_MakePoint(
                                            (points->0->>'longitude')::double precision,
                                            (points->0->>'latitude')::double precision
                                        )
                                    ]
                                );
                                poly := ST_SetSRID(ST_MakePolygon(ring), 4326);
                                IF result IS NULL THEN
                                    result := poly;
                                ELSE
                                    result := ST_Union(result, poly);
                                END IF;
                            END IF;
                        END LOOP;
                    END IF;
                    NEW.boundary := result;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
            ");

            migrationBuilder.Sql(@"
                CREATE TRIGGER trg_update_sigmet_boundary
                BEFORE INSERT OR UPDATE OF area
                ON sigmet
                FOR EACH ROW
                EXECUTE FUNCTION update_sigmet_boundary();
            ");

            // ── GAirmet trigger ──
            // The 'area' column is a single JSONB object with camelCase keys:
            // {"numPoints":4,"points":[{"longitude":-97.0,"latitude":32.8}, ...]}
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION update_gairmet_boundary()
                RETURNS TRIGGER AS $$
                DECLARE
                    points jsonb;
                    ring geometry;
                BEGIN
                    points := NEW.area->'points';
                    IF points IS NOT NULL AND jsonb_array_length(points) >= 3 THEN
                        ring := ST_MakeLine(
                            ARRAY(
                                SELECT ST_MakePoint(
                                    (p->>'longitude')::double precision,
                                    (p->>'latitude')::double precision
                                )
                                FROM jsonb_array_elements(points) AS p
                            )
                            || ARRAY[
                                ST_MakePoint(
                                    (points->0->>'longitude')::double precision,
                                    (points->0->>'latitude')::double precision
                                )
                            ]
                        );
                        NEW.boundary := ST_SetSRID(ST_MakePolygon(ring), 4326);
                    ELSE
                        NEW.boundary := NULL;
                    END IF;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
            ");

            migrationBuilder.Sql(@"
                CREATE TRIGGER trg_update_gairmet_boundary
                BEFORE INSERT OR UPDATE OF area
                ON gairmet
                FOR EACH ROW
                EXECUTE FUNCTION update_gairmet_boundary();
            ");

            // ── Backfill existing sigmet rows ──
            // Fire the trigger logic by touching the area column
            migrationBuilder.Sql(@"
                UPDATE sigmet SET area = area WHERE area IS NOT NULL;
            ");

            // ── Backfill existing gairmet rows ──
            migrationBuilder.Sql(@"
                UPDATE gairmet SET area = area WHERE area IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_update_sigmet_boundary ON sigmet;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS update_sigmet_boundary();");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_update_gairmet_boundary ON gairmet;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS update_gairmet_boundary();");

            migrationBuilder.DropIndex(
                name: "IX_sigmet_boundary",
                table: "sigmet");

            migrationBuilder.DropIndex(
                name: "IX_gairmet_boundary",
                table: "gairmet");

            migrationBuilder.DropColumn(
                name: "boundary",
                table: "sigmet");

            migrationBuilder.DropColumn(
                name: "boundary",
                table: "gairmet");
        }
    }
}
