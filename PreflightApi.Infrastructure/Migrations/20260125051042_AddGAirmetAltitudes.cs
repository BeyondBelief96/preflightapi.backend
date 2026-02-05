using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PreflightApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGAirmetAltitudes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "altitudes",
                table: "gairmet",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "altitudes",
                table: "gairmet");
        }
    }
}
