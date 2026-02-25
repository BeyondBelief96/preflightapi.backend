using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PreflightApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertTrackingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "last_alert_sent_utc",
                table: "data_sync_status",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_alert_severity",
                table: "data_sync_status",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "data_sync_status",
                keyColumn: "sync_type",
                keyValue: "Airport",
                columns: new[] { "last_alert_sent_utc", "last_alert_severity" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "data_sync_status",
                keyColumn: "sync_type",
                keyValue: "Airspace",
                columns: new[] { "last_alert_sent_utc", "last_alert_severity" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "data_sync_status",
                keyColumn: "sync_type",
                keyValue: "ChartSupplement",
                columns: new[] { "last_alert_sent_utc", "last_alert_severity" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "data_sync_status",
                keyColumn: "sync_type",
                keyValue: "Frequency",
                columns: new[] { "last_alert_sent_utc", "last_alert_severity" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "data_sync_status",
                keyColumn: "sync_type",
                keyValue: "GAirmet",
                columns: new[] { "last_alert_sent_utc", "last_alert_severity" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "data_sync_status",
                keyColumn: "sync_type",
                keyValue: "Metar",
                columns: new[] { "last_alert_sent_utc", "last_alert_severity" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "data_sync_status",
                keyColumn: "sync_type",
                keyValue: "NotamDelta",
                columns: new[] { "last_alert_sent_utc", "last_alert_severity" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "data_sync_status",
                keyColumn: "sync_type",
                keyValue: "Obstacle",
                columns: new[] { "last_alert_sent_utc", "last_alert_severity" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "data_sync_status",
                keyColumn: "sync_type",
                keyValue: "ObstacleDailyChange",
                columns: new[] { "last_alert_sent_utc", "last_alert_severity" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "data_sync_status",
                keyColumn: "sync_type",
                keyValue: "Pirep",
                columns: new[] { "last_alert_sent_utc", "last_alert_severity" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "data_sync_status",
                keyColumn: "sync_type",
                keyValue: "Sigmet",
                columns: new[] { "last_alert_sent_utc", "last_alert_severity" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "data_sync_status",
                keyColumn: "sync_type",
                keyValue: "SpecialUseAirspace",
                columns: new[] { "last_alert_sent_utc", "last_alert_severity" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "data_sync_status",
                keyColumn: "sync_type",
                keyValue: "Taf",
                columns: new[] { "last_alert_sent_utc", "last_alert_severity" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "data_sync_status",
                keyColumn: "sync_type",
                keyValue: "TerminalProcedure",
                columns: new[] { "last_alert_sent_utc", "last_alert_severity" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_alert_sent_utc",
                table: "data_sync_status");

            migrationBuilder.DropColumn(
                name: "last_alert_severity",
                table: "data_sync_status");
        }
    }
}
