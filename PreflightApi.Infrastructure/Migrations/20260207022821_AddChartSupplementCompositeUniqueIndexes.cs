using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PreflightApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChartSupplementCompositeUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_chart_supplement_file_name_airport_code",
                table: "chart_supplement",
                columns: new[] { "file_name", "airport_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chart_supplement_file_name_navigational_aid_name",
                table: "chart_supplement",
                columns: new[] { "file_name", "navigational_aid_name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_chart_supplement_file_name_airport_code",
                table: "chart_supplement");

            migrationBuilder.DropIndex(
                name: "IX_chart_supplement_file_name_navigational_aid_name",
                table: "chart_supplement");
        }
    }
}
