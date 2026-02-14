using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PreflightApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAirspaceGistIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_special_use_airspaces_geometry",
                table: "special_use_airspaces",
                column: "geometry")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_airspaces_geometry",
                table: "airspaces",
                column: "geometry")
                .Annotation("Npgsql:IndexMethod", "gist");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_special_use_airspaces_geometry",
                table: "special_use_airspaces");

            migrationBuilder.DropIndex(
                name: "IX_airspaces_geometry",
                table: "airspaces");
        }
    }
}
