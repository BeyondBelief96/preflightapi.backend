using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PreflightApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotamSearchColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "account_id",
                table: "notams",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "airport_name",
                table: "notams",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "notam_number",
                table: "notams",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "notam_year",
                table: "notams",
                type: "varchar(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_notams_account_id",
                table: "notams",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_notams_notam_number_notam_year",
                table: "notams",
                columns: new[] { "notam_number", "notam_year" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notams_account_id",
                table: "notams");

            migrationBuilder.DropIndex(
                name: "IX_notams_notam_number_notam_year",
                table: "notams");

            migrationBuilder.DropColumn(
                name: "account_id",
                table: "notams");

            migrationBuilder.DropColumn(
                name: "airport_name",
                table: "notams");

            migrationBuilder.DropColumn(
                name: "notam_number",
                table: "notams");

            migrationBuilder.DropColumn(
                name: "notam_year",
                table: "notams");
        }
    }
}
