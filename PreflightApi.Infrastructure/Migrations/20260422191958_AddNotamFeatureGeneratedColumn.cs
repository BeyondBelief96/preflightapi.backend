using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PreflightApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotamFeatureGeneratedColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "feature",
                table: "notams",
                type: "text",
                nullable: true,
                computedColumnSql: "(feature_json->'properties'->'coreNOTAMData'->'notam'->>'feature')",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_notams_classification_effective_end",
                table: "notams",
                columns: new[] { "classification", "effective_end" });

            migrationBuilder.CreateIndex(
                name: "IX_notams_feature",
                table: "notams",
                column: "feature");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notams_classification_effective_end",
                table: "notams");

            migrationBuilder.DropIndex(
                name: "IX_notams_feature",
                table: "notams");

            migrationBuilder.DropColumn(
                name: "feature",
                table: "notams");
        }
    }
}
