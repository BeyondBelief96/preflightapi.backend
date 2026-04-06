using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PreflightApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotamGeographyIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Functional GiST index on geometry cast to geography.
            // The briefing en-route query uses ST_DWithin(geometry::geography, routeLine::geography, meters)
            // which cannot use the existing GiST index on the geometry column because of the ::geography cast.
            // This index allows PostGIS to use index-assisted distance searches on the geography projection.
            migrationBuilder.Sql(
                "CREATE INDEX ix_notams_geometry_geography ON notams USING GIST ((geometry::geography)) WHERE geometry IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_notams_geometry_geography;");
        }
    }
}
