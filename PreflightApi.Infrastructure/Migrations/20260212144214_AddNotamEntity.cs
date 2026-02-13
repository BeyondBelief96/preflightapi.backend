using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace PreflightApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotamEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notams",
                columns: table => new
                {
                    nms_id = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    location = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true),
                    icao_location = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true),
                    classification = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    notam_type = table.Column<string>(type: "varchar(5)", maxLength: 5, nullable: true),
                    effective_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    effective_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    text = table.Column<string>(type: "text", nullable: true),
                    last_updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    feature_json = table.Column<string>(type: "jsonb", nullable: false),
                    geometry = table.Column<Geometry>(type: "geometry(Geometry, 4326)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notams", x => x.nms_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notams_classification",
                table: "notams",
                column: "classification");

            migrationBuilder.CreateIndex(
                name: "IX_notams_effective_end",
                table: "notams",
                column: "effective_end");

            migrationBuilder.CreateIndex(
                name: "IX_notams_effective_start",
                table: "notams",
                column: "effective_start");

            migrationBuilder.CreateIndex(
                name: "IX_notams_geometry",
                table: "notams",
                column: "geometry")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_notams_icao_location",
                table: "notams",
                column: "icao_location");

            migrationBuilder.CreateIndex(
                name: "IX_notams_last_updated",
                table: "notams",
                column: "last_updated");

            migrationBuilder.CreateIndex(
                name: "IX_notams_location",
                table: "notams",
                column: "location");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notams");
        }
    }
}
