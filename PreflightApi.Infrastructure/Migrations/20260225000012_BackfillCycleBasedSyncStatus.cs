using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PreflightApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillCycleBasedSyncStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill last_successful_sync_utc for cycle-based sync types from the
            // existing faa_publication_cycle.last_successful_update values so the health
            // check reports fresh immediately after deployment (instead of waiting for the
            // next cycle-based cron run, which could be weeks away).
            migrationBuilder.Sql("""
                UPDATE data_sync_status dss
                SET last_successful_sync_utc = fpc.last_successful_update,
                    last_sync_succeeded = true,
                    updated_at = NOW()
                FROM faa_publication_cycle fpc
                WHERE dss.publication_type = fpc.publication_type::text
                  AND dss.staleness_mode = 'CycleBased'
                  AND dss.last_successful_sync_utc IS NULL
                  AND fpc.last_successful_update IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
