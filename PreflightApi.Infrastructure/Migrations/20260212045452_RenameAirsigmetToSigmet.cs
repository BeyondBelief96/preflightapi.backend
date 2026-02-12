using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PreflightApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameAirsigmetToSigmet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_airsigmet",
                table: "airsigmet");

            migrationBuilder.RenameTable(
                name: "airsigmet",
                newName: "sigmet");

            migrationBuilder.RenameColumn(
                name: "airsigmet_type",
                table: "sigmet",
                newName: "sigmet_type");

            migrationBuilder.RenameIndex(
                name: "IX_airsigmet_valid_time_to",
                table: "sigmet",
                newName: "IX_sigmet_valid_time_to");

            migrationBuilder.RenameIndex(
                name: "IX_airsigmet_valid_time_from",
                table: "sigmet",
                newName: "IX_sigmet_valid_time_from");

            migrationBuilder.RenameIndex(
                name: "IX_airsigmet_airsigmet_type",
                table: "sigmet",
                newName: "IX_sigmet_sigmet_type");

            migrationBuilder.AddPrimaryKey(
                name: "PK_sigmet",
                table: "sigmet",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_sigmet",
                table: "sigmet");

            migrationBuilder.RenameTable(
                name: "sigmet",
                newName: "airsigmet");

            migrationBuilder.RenameColumn(
                name: "sigmet_type",
                table: "airsigmet",
                newName: "airsigmet_type");

            migrationBuilder.RenameIndex(
                name: "IX_sigmet_valid_time_to",
                table: "airsigmet",
                newName: "IX_airsigmet_valid_time_to");

            migrationBuilder.RenameIndex(
                name: "IX_sigmet_valid_time_from",
                table: "airsigmet",
                newName: "IX_airsigmet_valid_time_from");

            migrationBuilder.RenameIndex(
                name: "IX_sigmet_sigmet_type",
                table: "airsigmet",
                newName: "IX_airsigmet_airsigmet_type");

            migrationBuilder.AddPrimaryKey(
                name: "PK_airsigmet",
                table: "airsigmet",
                column: "Id");
        }
    }
}
