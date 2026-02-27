using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PreflightApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceHealthAlertState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "service_health_alert_state",
                columns: table => new
                {
                    service_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_known_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    last_alert_sent_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_alert_severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_health_alert_state", x => x.service_name);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "service_health_alert_state");
        }
    }
}
