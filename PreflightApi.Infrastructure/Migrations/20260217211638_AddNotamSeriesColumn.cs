using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PreflightApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotamSeriesColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notams_notam_number_notam_year",
                table: "notams");

            migrationBuilder.AddColumn<string>(
                name: "series",
                table: "notams",
                type: "varchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_notams_notam_number_notam_year_series",
                table: "notams",
                columns: new[] { "notam_number", "notam_year", "series" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notams_notam_number_notam_year_series",
                table: "notams");

            migrationBuilder.DropColumn(
                name: "series",
                table: "notams");

            migrationBuilder.CreateIndex(
                name: "IX_notams_notam_number_notam_year",
                table: "notams",
                columns: new[] { "notam_number", "notam_year" });
        }
    }
}
