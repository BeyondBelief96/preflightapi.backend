using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PreflightApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateObstacleKnownValidDateToReleaseDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "faa_publication_cycle",
                keyColumn: "Id",
                keyValue: 7,
                column: "known_valid_date",
                value: new DateTime(2025, 10, 28, 0, 0, 0, 0, DateTimeKind.Utc));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "faa_publication_cycle",
                keyColumn: "Id",
                keyValue: 7,
                column: "known_valid_date",
                value: new DateTime(2025, 10, 26, 0, 0, 0, 0, DateTimeKind.Utc));
        }
    }
}
