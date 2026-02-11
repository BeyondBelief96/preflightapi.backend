using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PreflightApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveVarcharConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "surface_type_code",
                table: "runways",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(12)",
                oldMaxLength: 12,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "surface_treatment_code",
                table: "runways",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(5)",
                oldMaxLength: 5,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "site_no",
                table: "runways",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(9)",
                oldMaxLength: 9);

            migrationBuilder.AlterColumn<string>(
                name: "runway_id",
                table: "runways",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(7)",
                oldMaxLength: 7);

            migrationBuilder.AlterColumn<string>(
                name: "pavement_classification",
                table: "runways",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(11)",
                oldMaxLength: 11,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "edge_light_intensity",
                table: "runways",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(5)",
                oldMaxLength: 5,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "determination_method_code",
                table: "runways",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "length_source_date",
                table: "runways",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pavement_type_code",
                table: "runways",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "runway_length_source",
                table: "runways",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subgrade_strength_code",
                table: "runways",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "surface_condition",
                table: "runways",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tire_pressure_code",
                table: "runways",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "visual_glide_slope_indicator",
                table: "runway_ends",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(5)",
                oldMaxLength: 5,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "site_no",
                table: "runway_ends",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(9)",
                oldMaxLength: 9);

            migrationBuilder.AlterColumn<string>(
                name: "runway_visual_range_equipment",
                table: "runway_ends",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(3)",
                oldMaxLength: 3,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "runway_markings_type",
                table: "runway_ends",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(5)",
                oldMaxLength: 5,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "runway_markings_condition",
                table: "runway_ends",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldMaxLength: 1,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "runway_id_ref",
                table: "runway_ends",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(7)",
                oldMaxLength: 7);

            migrationBuilder.AlterColumn<string>(
                name: "runway_end_id",
                table: "runway_ends",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<string>(
                name: "controlling_object_marked_lighted",
                table: "runway_ends",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(4)",
                oldMaxLength: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "controlling_object_description",
                table: "runway_ends",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(11)",
                oldMaxLength: 11,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "controlling_object_centerline_offset",
                table: "runway_ends",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(7)",
                oldMaxLength: 7,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "approach_type",
                table: "runway_ends",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "approach_light_system",
                table: "runway_ends",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(8)",
                oldMaxLength: 8,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "accelerate_stop_dist_available",
                table: "runway_ends",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "centerline_direction_code",
                table: "runway_ends",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "displaced_thr_elevation_date",
                table: "runway_ends",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "displaced_thr_elevation_source",
                table: "runway_ends",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "displaced_thr_lat_deg",
                table: "runway_ends",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "displaced_thr_lat_hemis",
                table: "runway_ends",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "displaced_thr_lat_min",
                table: "runway_ends",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "displaced_thr_lat_sec",
                table: "runway_ends",
                type: "numeric(6,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "displaced_thr_long_deg",
                table: "runway_ends",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "displaced_thr_long_hemis",
                table: "runway_ends",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "displaced_thr_long_min",
                table: "runway_ends",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "displaced_thr_long_sec",
                table: "runway_ends",
                type: "numeric(6,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "displaced_thr_position_date",
                table: "runway_ends",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "displaced_thr_position_source",
                table: "runway_ends",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "far_part_77_code",
                table: "runway_ends",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "lahso_available_landing_distance",
                table: "runway_ends",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lahso_description",
                table: "runway_ends",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lahso_intersecting_runway",
                table: "runway_ends",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "lahso_lat_decimal",
                table: "runway_ends",
                type: "numeric(10,8)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lahso_latitude",
                table: "runway_ends",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "lahso_long_decimal",
                table: "runway_ends",
                type: "numeric(11,8)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lahso_longitude",
                table: "runway_ends",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "lahso_position_date",
                table: "runway_ends",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "lahso_position_source",
                table: "runway_ends",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "landing_distance_available",
                table: "runway_ends",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "runway_gradient",
                table: "runway_ends",
                type: "numeric(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "runway_gradient_direction",
                table: "runway_ends",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "rwy_end_elevation_date",
                table: "runway_ends",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rwy_end_elevation_source",
                table: "runway_ends",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "rwy_end_lat_deg",
                table: "runway_ends",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rwy_end_lat_hemis",
                table: "runway_ends",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "rwy_end_lat_min",
                table: "runway_ends",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "rwy_end_lat_sec",
                table: "runway_ends",
                type: "numeric(6,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "rwy_end_long_deg",
                table: "runway_ends",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rwy_end_long_hemis",
                table: "runway_ends",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "rwy_end_long_min",
                table: "runway_ends",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "rwy_end_long_sec",
                table: "runway_ends",
                type: "numeric(6,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "rwy_end_position_date",
                table: "runway_ends",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rwy_end_position_source",
                table: "runway_ends",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "takeoff_distance_available",
                table: "runway_ends",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "takeoff_run_available",
                table: "runway_ends",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "touchdown_zone_elev_date",
                table: "runway_ends",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "touchdown_zone_elev_source",
                table: "runway_ends",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "vertical_accuracy",
                table: "obstacles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldMaxLength: 1,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "verification_status",
                table: "obstacles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldMaxLength: 1,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "state_id",
                table: "obstacles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(2)",
                oldMaxLength: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "obstacle_type",
                table: "obstacles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(18)",
                oldMaxLength: 18,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "obstacle_number",
                table: "obstacles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(6)",
                oldMaxLength: 6);

            migrationBuilder.AlterColumn<string>(
                name: "oas_code",
                table: "obstacles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(2)",
                oldMaxLength: 2);

            migrationBuilder.AlterColumn<string>(
                name: "mark_indicator",
                table: "obstacles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldMaxLength: 1,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "long_hemisphere",
                table: "obstacles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldMaxLength: 1,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "lighting",
                table: "obstacles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldMaxLength: 1,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "lat_hemisphere",
                table: "obstacles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldMaxLength: 1,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "julian_date",
                table: "obstacles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(7)",
                oldMaxLength: 7,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "horizontal_accuracy",
                table: "obstacles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldMaxLength: 1,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "faa_study_number",
                table: "obstacles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(14)",
                oldMaxLength: 14,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "country_id",
                table: "obstacles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(2)",
                oldMaxLength: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "city_name",
                table: "obstacles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(16)",
                oldMaxLength: 16,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "action",
                table: "obstacles",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldMaxLength: 1,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "oas_number",
                table: "obstacles",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "wind_indcr_flag",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(3)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "user_fee_flag",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(26)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "twr_type_code",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(12)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "trns_strg_tie_flag",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "trns_strg_hgr_flag",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "trns_strg_buoy_flag",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "survey_method_code",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "state_name",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "state_code",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "site_type_code",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1)");

            migrationBuilder.AlterColumn<string>(
                name: "seg_circle_mkr_flag",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(3)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "resp_artcc_id",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "region_code",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(3)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "pwr_plant_repair_ser",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(5)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ownership_type_code",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "other_services",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(110)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "notam_id",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "notam_flag",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "nasp_code",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(7)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "min_op_network",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "mil_lndg_flag",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "medical_use_flag",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "mag_hemis",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "long_hemis",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "lndg_rights_flag",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "lndg_fee_flag",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "lgt_sked",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(7)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "lat_hemis",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "joint_use_flag",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "inspector_code",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "inspect_method_code",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "icao_id",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(7)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "fuel_types",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "fss_on_arpt_flag",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "fss_name",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "fss_id",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "facility_use_code",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "elev_method_code",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "direction_code",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(3)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "customs_flag",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "cta",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "county_name",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(21)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "county_assoc_state",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "country_code",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contr_fuel_avbl",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contact_zip_plus_four",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contact_zip_code",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(5)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contact_title",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(10)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contact_state",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contact_phone_number",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(16)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contact_name",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(35)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contact_city",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contact_address2",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(35)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contact_address1",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(35)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "city",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "chart_name",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "bulk_oxy_type",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(8)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "bottled_oxy_type",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(8)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "bcn_lgt_sked",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(7)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "bcn_lens_color",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(3)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "attendance_month",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "attendance_hours",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(40)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "attendance_day",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(16)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "arpt_status",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "arpt_psn_source",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(16)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "arpt_name",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "arpt_id",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "arpt_elev_source",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(16)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "airframe_repair_ser_code",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(5)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ado_code",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(3)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "activation_date",
                table: "airports",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(7)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "site_no",
                table: "airports",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(9)");

            migrationBuilder.AddColumn<string>(
                name: "alt_fss_id",
                table: "airports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "alt_fss_name",
                table: "airports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "alt_toll_free_number",
                table: "airports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "arff_cert_type_date",
                table: "airports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "artcc_name",
                table: "airports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "asp_analysis_dtrm_code",
                table: "airports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "computer_id",
                table: "airports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "far_139_carrier_ser_code",
                table: "airports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "far_139_type_code",
                table: "airports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "fss_phone_number",
                table: "airports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "toll_free_number",
                table: "airports",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "determination_method_code",
                table: "runways");

            migrationBuilder.DropColumn(
                name: "length_source_date",
                table: "runways");

            migrationBuilder.DropColumn(
                name: "pavement_type_code",
                table: "runways");

            migrationBuilder.DropColumn(
                name: "runway_length_source",
                table: "runways");

            migrationBuilder.DropColumn(
                name: "subgrade_strength_code",
                table: "runways");

            migrationBuilder.DropColumn(
                name: "surface_condition",
                table: "runways");

            migrationBuilder.DropColumn(
                name: "tire_pressure_code",
                table: "runways");

            migrationBuilder.DropColumn(
                name: "accelerate_stop_dist_available",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "centerline_direction_code",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "displaced_thr_elevation_date",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "displaced_thr_elevation_source",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "displaced_thr_lat_deg",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "displaced_thr_lat_hemis",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "displaced_thr_lat_min",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "displaced_thr_lat_sec",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "displaced_thr_long_deg",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "displaced_thr_long_hemis",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "displaced_thr_long_min",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "displaced_thr_long_sec",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "displaced_thr_position_date",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "displaced_thr_position_source",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "far_part_77_code",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "lahso_available_landing_distance",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "lahso_description",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "lahso_intersecting_runway",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "lahso_lat_decimal",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "lahso_latitude",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "lahso_long_decimal",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "lahso_longitude",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "lahso_position_date",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "lahso_position_source",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "landing_distance_available",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "runway_gradient",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "runway_gradient_direction",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "rwy_end_elevation_date",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "rwy_end_elevation_source",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "rwy_end_lat_deg",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "rwy_end_lat_hemis",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "rwy_end_lat_min",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "rwy_end_lat_sec",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "rwy_end_long_deg",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "rwy_end_long_hemis",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "rwy_end_long_min",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "rwy_end_long_sec",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "rwy_end_position_date",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "rwy_end_position_source",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "takeoff_distance_available",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "takeoff_run_available",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "touchdown_zone_elev_date",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "touchdown_zone_elev_source",
                table: "runway_ends");

            migrationBuilder.DropColumn(
                name: "alt_fss_id",
                table: "airports");

            migrationBuilder.DropColumn(
                name: "alt_fss_name",
                table: "airports");

            migrationBuilder.DropColumn(
                name: "alt_toll_free_number",
                table: "airports");

            migrationBuilder.DropColumn(
                name: "arff_cert_type_date",
                table: "airports");

            migrationBuilder.DropColumn(
                name: "artcc_name",
                table: "airports");

            migrationBuilder.DropColumn(
                name: "asp_analysis_dtrm_code",
                table: "airports");

            migrationBuilder.DropColumn(
                name: "computer_id",
                table: "airports");

            migrationBuilder.DropColumn(
                name: "far_139_carrier_ser_code",
                table: "airports");

            migrationBuilder.DropColumn(
                name: "far_139_type_code",
                table: "airports");

            migrationBuilder.DropColumn(
                name: "fss_phone_number",
                table: "airports");

            migrationBuilder.DropColumn(
                name: "toll_free_number",
                table: "airports");

            migrationBuilder.AlterColumn<string>(
                name: "surface_type_code",
                table: "runways",
                type: "varchar(12)",
                maxLength: 12,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "surface_treatment_code",
                table: "runways",
                type: "varchar(5)",
                maxLength: 5,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "site_no",
                table: "runways",
                type: "varchar(9)",
                maxLength: 9,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "runway_id",
                table: "runways",
                type: "varchar(7)",
                maxLength: 7,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "pavement_classification",
                table: "runways",
                type: "varchar(11)",
                maxLength: 11,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "edge_light_intensity",
                table: "runways",
                type: "varchar(5)",
                maxLength: 5,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "visual_glide_slope_indicator",
                table: "runway_ends",
                type: "varchar(5)",
                maxLength: 5,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "site_no",
                table: "runway_ends",
                type: "varchar(9)",
                maxLength: 9,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "runway_visual_range_equipment",
                table: "runway_ends",
                type: "varchar(3)",
                maxLength: 3,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "runway_markings_type",
                table: "runway_ends",
                type: "varchar(5)",
                maxLength: 5,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "runway_markings_condition",
                table: "runway_ends",
                type: "varchar(1)",
                maxLength: 1,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "runway_id_ref",
                table: "runway_ends",
                type: "varchar(7)",
                maxLength: 7,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "runway_end_id",
                table: "runway_ends",
                type: "varchar(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "controlling_object_marked_lighted",
                table: "runway_ends",
                type: "varchar(4)",
                maxLength: 4,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "controlling_object_description",
                table: "runway_ends",
                type: "varchar(11)",
                maxLength: 11,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "controlling_object_centerline_offset",
                table: "runway_ends",
                type: "varchar(7)",
                maxLength: 7,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "approach_type",
                table: "runway_ends",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "approach_light_system",
                table: "runway_ends",
                type: "varchar(8)",
                maxLength: 8,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "vertical_accuracy",
                table: "obstacles",
                type: "varchar(1)",
                maxLength: 1,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "verification_status",
                table: "obstacles",
                type: "varchar(1)",
                maxLength: 1,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "state_id",
                table: "obstacles",
                type: "varchar(2)",
                maxLength: 2,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "obstacle_type",
                table: "obstacles",
                type: "varchar(18)",
                maxLength: 18,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "obstacle_number",
                table: "obstacles",
                type: "varchar(6)",
                maxLength: 6,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "oas_code",
                table: "obstacles",
                type: "varchar(2)",
                maxLength: 2,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "mark_indicator",
                table: "obstacles",
                type: "varchar(1)",
                maxLength: 1,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "long_hemisphere",
                table: "obstacles",
                type: "varchar(1)",
                maxLength: 1,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "lighting",
                table: "obstacles",
                type: "varchar(1)",
                maxLength: 1,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "lat_hemisphere",
                table: "obstacles",
                type: "varchar(1)",
                maxLength: 1,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "julian_date",
                table: "obstacles",
                type: "varchar(7)",
                maxLength: 7,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "horizontal_accuracy",
                table: "obstacles",
                type: "varchar(1)",
                maxLength: 1,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "faa_study_number",
                table: "obstacles",
                type: "varchar(14)",
                maxLength: 14,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "country_id",
                table: "obstacles",
                type: "varchar(2)",
                maxLength: 2,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "city_name",
                table: "obstacles",
                type: "varchar(16)",
                maxLength: 16,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "action",
                table: "obstacles",
                type: "varchar(1)",
                maxLength: 1,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "oas_number",
                table: "obstacles",
                type: "varchar(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "wind_indcr_flag",
                table: "airports",
                type: "varchar(3)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "user_fee_flag",
                table: "airports",
                type: "varchar(26)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "twr_type_code",
                table: "airports",
                type: "varchar(12)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "trns_strg_tie_flag",
                table: "airports",
                type: "varchar(1)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "trns_strg_hgr_flag",
                table: "airports",
                type: "varchar(1)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "trns_strg_buoy_flag",
                table: "airports",
                type: "varchar(1)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "survey_method_code",
                table: "airports",
                type: "varchar(1)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "state_name",
                table: "airports",
                type: "varchar(30)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "state_code",
                table: "airports",
                type: "varchar(2)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "site_type_code",
                table: "airports",
                type: "varchar(1)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "seg_circle_mkr_flag",
                table: "airports",
                type: "varchar(3)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "resp_artcc_id",
                table: "airports",
                type: "varchar(4)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "region_code",
                table: "airports",
                type: "varchar(3)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "pwr_plant_repair_ser",
                table: "airports",
                type: "varchar(5)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ownership_type_code",
                table: "airports",
                type: "varchar(2)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "other_services",
                table: "airports",
                type: "varchar(110)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "notam_id",
                table: "airports",
                type: "varchar(4)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "notam_flag",
                table: "airports",
                type: "varchar(1)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "nasp_code",
                table: "airports",
                type: "varchar(7)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "min_op_network",
                table: "airports",
                type: "varchar(1)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "mil_lndg_flag",
                table: "airports",
                type: "varchar(1)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "medical_use_flag",
                table: "airports",
                type: "varchar(1)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "mag_hemis",
                table: "airports",
                type: "varchar(1)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "long_hemis",
                table: "airports",
                type: "varchar(1)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "lndg_rights_flag",
                table: "airports",
                type: "varchar(1)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "lndg_fee_flag",
                table: "airports",
                type: "varchar(1)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "lgt_sked",
                table: "airports",
                type: "varchar(7)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "lat_hemis",
                table: "airports",
                type: "varchar(1)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "joint_use_flag",
                table: "airports",
                type: "varchar(1)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "inspector_code",
                table: "airports",
                type: "varchar(1)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "inspect_method_code",
                table: "airports",
                type: "varchar(1)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "icao_id",
                table: "airports",
                type: "varchar(7)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "fuel_types",
                table: "airports",
                type: "varchar(40)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "fss_on_arpt_flag",
                table: "airports",
                type: "varchar(1)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "fss_name",
                table: "airports",
                type: "varchar(30)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "fss_id",
                table: "airports",
                type: "varchar(4)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "facility_use_code",
                table: "airports",
                type: "varchar(2)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "elev_method_code",
                table: "airports",
                type: "varchar(1)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "direction_code",
                table: "airports",
                type: "varchar(3)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "customs_flag",
                table: "airports",
                type: "varchar(1)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "cta",
                table: "airports",
                type: "varchar(4)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "county_name",
                table: "airports",
                type: "varchar(21)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "county_assoc_state",
                table: "airports",
                type: "varchar(2)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "country_code",
                table: "airports",
                type: "varchar(2)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contr_fuel_avbl",
                table: "airports",
                type: "varchar(1)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contact_zip_plus_four",
                table: "airports",
                type: "varchar(4)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contact_zip_code",
                table: "airports",
                type: "varchar(5)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contact_title",
                table: "airports",
                type: "varchar(10)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contact_state",
                table: "airports",
                type: "varchar(2)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contact_phone_number",
                table: "airports",
                type: "varchar(16)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contact_name",
                table: "airports",
                type: "varchar(35)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contact_city",
                table: "airports",
                type: "varchar(30)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contact_address2",
                table: "airports",
                type: "varchar(35)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "contact_address1",
                table: "airports",
                type: "varchar(35)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "city",
                table: "airports",
                type: "varchar(40)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "chart_name",
                table: "airports",
                type: "varchar(30)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "bulk_oxy_type",
                table: "airports",
                type: "varchar(8)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "bottled_oxy_type",
                table: "airports",
                type: "varchar(8)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "bcn_lgt_sked",
                table: "airports",
                type: "varchar(7)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "bcn_lens_color",
                table: "airports",
                type: "varchar(3)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "attendance_month",
                table: "airports",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "attendance_hours",
                table: "airports",
                type: "varchar(40)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "attendance_day",
                table: "airports",
                type: "varchar(16)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "arpt_status",
                table: "airports",
                type: "varchar(2)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "arpt_psn_source",
                table: "airports",
                type: "varchar(16)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "arpt_name",
                table: "airports",
                type: "varchar(50)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "arpt_id",
                table: "airports",
                type: "varchar(4)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "arpt_elev_source",
                table: "airports",
                type: "varchar(16)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "airframe_repair_ser_code",
                table: "airports",
                type: "varchar(5)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ado_code",
                table: "airports",
                type: "varchar(3)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "activation_date",
                table: "airports",
                type: "varchar(7)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "site_no",
                table: "airports",
                type: "varchar(9)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
