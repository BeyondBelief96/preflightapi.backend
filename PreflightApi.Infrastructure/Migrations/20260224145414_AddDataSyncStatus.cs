using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PreflightApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDataSyncStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "data_sync_status",
                columns: table => new
                {
                    sync_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    staleness_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    staleness_threshold_minutes = table.Column<int>(type: "integer", nullable: true),
                    publication_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    last_successful_sync_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_attempted_sync_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_sync_succeeded = table.Column<bool>(type: "boolean", nullable: false),
                    consecutive_failures = table.Column<int>(type: "integer", nullable: false),
                    last_error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    last_successful_record_count = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_data_sync_status", x => x.sync_type);
                });

            migrationBuilder.InsertData(
                table: "data_sync_status",
                columns: new[] { "sync_type", "consecutive_failures", "last_attempted_sync_utc", "last_error_message", "last_successful_record_count", "last_successful_sync_utc", "last_sync_succeeded", "publication_type", "staleness_mode", "staleness_threshold_minutes", "updated_at" },
                values: new object[,]
                {
                    { "Airport", 0, null, null, 0, null, true, "NasrSubscription_Airport", "CycleBased", null, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "Airspace", 0, null, null, 0, null, true, "Airspaces", "CycleBased", null, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "ChartSupplement", 0, null, null, 0, null, true, "ChartSupplement", "CycleBased", null, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "Frequency", 0, null, null, 0, null, true, "NasrSubscription_Frequencies", "CycleBased", null, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "GAirmet", 0, null, null, 0, null, true, null, "TimeBased", 120, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "Metar", 0, null, null, 0, null, true, null, "TimeBased", 50, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "NotamDelta", 0, null, null, 0, null, true, null, "TimeBased", 15, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "Obstacle", 0, null, null, 0, null, true, "Obstacles", "CycleBased", null, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "ObstacleDailyChange", 0, null, null, 0, null, true, null, "TimeBased", 2880, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "Pirep", 0, null, null, 0, null, true, null, "TimeBased", 30, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "Sigmet", 0, null, null, 0, null, true, null, "TimeBased", 120, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "SpecialUseAirspace", 0, null, null, 0, null, true, "SpecialUseAirspaces", "CycleBased", null, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "Taf", 0, null, null, 0, null, true, null, "TimeBased", 120, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) },
                    { "TerminalProcedure", 0, null, null, 0, null, true, "TerminalProcedure", "CycleBased", null, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "data_sync_status");
        }
    }
}
