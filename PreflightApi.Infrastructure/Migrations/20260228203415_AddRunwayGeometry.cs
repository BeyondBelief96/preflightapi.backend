using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace PreflightApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRunwayGeometry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Geometry>(
                name: "geometry",
                table: "runways",
                type: "geometry(Polygon, 4326)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_runways_geometry",
                table: "runways",
                column: "geometry")
                .Annotation("Npgsql:IndexMethod", "gist");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_runways_geometry",
                table: "runways");

            migrationBuilder.DropColumn(
                name: "geometry",
                table: "runways");
        }
    }
}
