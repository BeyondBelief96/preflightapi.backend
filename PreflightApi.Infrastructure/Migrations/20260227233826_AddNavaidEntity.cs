using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace PreflightApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNavaidEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "navaids",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    effective_date = table.Column<DateTime>(type: "date", nullable: false),
                    nav_id = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: false),
                    nav_type = table.Column<string>(type: "varchar(25)", maxLength: 25, nullable: false),
                    state_code = table.Column<string>(type: "varchar(2)", maxLength: 2, nullable: true),
                    city = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    country_code = table.Column<string>(type: "varchar(2)", maxLength: 2, nullable: false),
                    nav_status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    state_name = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true),
                    region_code = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: true),
                    country_name = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    fan_marker = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true),
                    owner = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    @operator = table.Column<string>(name: "operator", type: "varchar(50)", maxLength: 50, nullable: true),
                    nas_use_flag = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: false),
                    public_use_flag = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: false),
                    ndb_class_code = table.Column<string>(type: "varchar(11)", maxLength: 11, nullable: true),
                    oper_hours = table.Column<string>(type: "varchar(11)", maxLength: 11, nullable: true),
                    high_alt_artcc_id = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: true),
                    high_artcc_name = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true),
                    low_alt_artcc_id = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: true),
                    low_artcc_name = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true),
                    lat_deg = table.Column<int>(type: "integer", nullable: true),
                    lat_min = table.Column<int>(type: "integer", nullable: true),
                    lat_sec = table.Column<decimal>(type: "numeric(6,4)", nullable: true),
                    lat_hemis = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: true),
                    lat_decimal = table.Column<decimal>(type: "numeric(10,8)", nullable: true),
                    long_deg = table.Column<int>(type: "integer", nullable: true),
                    long_min = table.Column<int>(type: "integer", nullable: true),
                    long_sec = table.Column<decimal>(type: "numeric(6,4)", nullable: true),
                    long_hemis = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: true),
                    long_decimal = table.Column<decimal>(type: "numeric(11,8)", nullable: true),
                    survey_accuracy_code = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: true),
                    tacan_dme_status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true),
                    tacan_dme_lat_deg = table.Column<int>(type: "integer", nullable: true),
                    tacan_dme_lat_min = table.Column<int>(type: "integer", nullable: true),
                    tacan_dme_lat_sec = table.Column<decimal>(type: "numeric(6,4)", nullable: true),
                    tacan_dme_lat_hemis = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: true),
                    tacan_dme_lat_decimal = table.Column<decimal>(type: "numeric(10,8)", nullable: true),
                    tacan_dme_long_deg = table.Column<int>(type: "integer", nullable: true),
                    tacan_dme_long_min = table.Column<int>(type: "integer", nullable: true),
                    tacan_dme_long_sec = table.Column<decimal>(type: "numeric(6,4)", nullable: true),
                    tacan_dme_long_hemis = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: true),
                    tacan_dme_long_decimal = table.Column<decimal>(type: "numeric(11,8)", nullable: true),
                    elev = table.Column<decimal>(type: "numeric(6,1)", nullable: true),
                    mag_varn = table.Column<int>(type: "integer", nullable: true),
                    mag_varn_hemis = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: true),
                    mag_varn_year = table.Column<int>(type: "integer", nullable: true),
                    simul_voice_flag = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: true),
                    pwr_output = table.Column<int>(type: "integer", nullable: true),
                    auto_voice_id_flag = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: true),
                    mnt_cat_code = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: true),
                    voice_call = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: true),
                    chan = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: true),
                    freq = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    mkr_ident = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true),
                    mkr_shape = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: true),
                    mkr_brg = table.Column<int>(type: "integer", nullable: true),
                    alt_code = table.Column<string>(type: "varchar(2)", maxLength: 2, nullable: true),
                    dme_ssv = table.Column<string>(type: "varchar(2)", maxLength: 2, nullable: true),
                    low_nav_on_high_chart_flag = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: true),
                    z_mkr_flag = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: true),
                    fss_id = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: true),
                    fss_name = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true),
                    fss_hours = table.Column<string>(type: "varchar(65)", maxLength: 65, nullable: true),
                    notam_id = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: true),
                    quad_ident = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    pitch_flag = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: true),
                    catch_flag = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: true),
                    sua_atcaa_flag = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: true),
                    restriction_flag = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: true),
                    hiwas_flag = table.Column<string>(type: "varchar(1)", maxLength: 1, nullable: true),
                    location = table.Column<Point>(type: "geography(Point, 4326)", nullable: true),
                    tacan_dme_location = table.Column<Point>(type: "geography(Point, 4326)", nullable: true),
                    checkpoints_json = table.Column<string>(type: "jsonb", nullable: true),
                    remarks_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_navaids", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "data_sync_status",
                columns: new[] { "sync_type", "consecutive_failures", "last_alert_sent_utc", "last_alert_severity", "last_attempted_sync_utc", "last_error_message", "last_successful_record_count", "last_successful_sync_utc", "last_sync_succeeded", "publication_type", "staleness_mode", "staleness_threshold_minutes", "updated_at" },
                values: new object[] { "Navaid", 0, null, null, null, null, 0, null, true, "NasrSubscription_Navaids", "CycleBased", null, new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified) });

            migrationBuilder.CreateIndex(
                name: "IX_navaids_location",
                table: "navaids",
                column: "location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_navaids_nav_id",
                table: "navaids",
                column: "nav_id");

            migrationBuilder.CreateIndex(
                name: "IX_navaids_nav_id_nav_type_country_code_city",
                table: "navaids",
                columns: new[] { "nav_id", "nav_type", "country_code", "city" },
                unique: true,
                filter: "\"nav_id\" IS NOT NULL AND \"nav_type\" IS NOT NULL AND \"country_code\" IS NOT NULL AND \"city\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_navaids_nav_type",
                table: "navaids",
                column: "nav_type");

            migrationBuilder.CreateIndex(
                name: "IX_navaids_state_code",
                table: "navaids",
                column: "state_code");

            migrationBuilder.CreateIndex(
                name: "IX_navaids_tacan_dme_location",
                table: "navaids",
                column: "tacan_dme_location")
                .Annotation("Npgsql:IndexMethod", "gist");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "navaids");

            migrationBuilder.DeleteData(
                table: "data_sync_status",
                keyColumn: "sync_type",
                keyValue: "Navaid");
        }
    }
}
