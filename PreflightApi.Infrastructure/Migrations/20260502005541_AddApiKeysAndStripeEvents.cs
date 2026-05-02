using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PreflightApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApiKeysAndStripeEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "api_keys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    stripe_customer_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    stripe_subscription_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    prefix = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    key_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tier = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    monthly_request_count = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    quota_reset_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_keys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "processed_stripe_events",
                columns: table => new
                {
                    event_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    event_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processed_stripe_events", x => x.event_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_api_keys_key_hash",
                table: "api_keys",
                column: "key_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_api_keys_owner_id",
                table: "api_keys",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "IX_api_keys_prefix",
                table: "api_keys",
                column: "prefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_api_keys_stripe_customer_id",
                table: "api_keys",
                column: "stripe_customer_id",
                filter: "\"stripe_customer_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_api_keys_stripe_subscription_id",
                table: "api_keys",
                column: "stripe_subscription_id",
                filter: "\"stripe_subscription_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_processed_stripe_events_processed_at",
                table: "processed_stripe_events",
                column: "processed_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_keys");

            migrationBuilder.DropTable(
                name: "processed_stripe_events");
        }
    }
}
