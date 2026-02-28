using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PreflightApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRunwayGeometrySyncStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "data_sync_status",
                columns: new[] { "sync_type", "consecutive_failures", "last_alert_sent_utc", "last_alert_severity", "last_attempted_sync_utc", "last_error_message", "last_successful_record_count", "last_successful_sync_utc", "last_sync_succeeded", "publication_type", "staleness_mode", "staleness_threshold_minutes", "updated_at" },
                values: new object[] { "RunwayGeometry", 0, null, null, null, null, 0, null, true, "RunwayGeometry", "CycleBased", null, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.InsertData(
                table: "faa_publication_cycle",
                columns: new[] { "Id", "cycle_length_days", "known_valid_date", "last_successful_update", "publication_type" },
                values: new object[] { 9, 56, new DateTime(2024, 12, 26, 0, 0, 0, 0, DateTimeKind.Utc), null, "RunwayGeometry" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "data_sync_status",
                keyColumn: "sync_type",
                keyValue: "RunwayGeometry");

            migrationBuilder.DeleteData(
                table: "faa_publication_cycle",
                keyColumn: "Id",
                keyValue: 9);
        }
    }
}
