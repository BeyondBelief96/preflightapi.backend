using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PreflightApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceAirportDiagramWithTerminalProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "airport_diagrams");

            migrationBuilder.CreateTable(
                name: "terminal_procedures",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    airport_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    icao_ident = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    airport_ident = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    chart_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    chart_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    pdf_file_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    amendment_number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    amendment_date = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_terminal_procedures", x => x.id);
                });

            migrationBuilder.UpdateData(
                table: "faa_publication_cycle",
                keyColumn: "Id",
                keyValue: 2,
                column: "publication_type",
                value: "TerminalProcedure");

            migrationBuilder.CreateIndex(
                name: "IX_terminal_procedures_airport_ident",
                table: "terminal_procedures",
                column: "airport_ident");

            migrationBuilder.CreateIndex(
                name: "IX_terminal_procedures_chart_code",
                table: "terminal_procedures",
                column: "chart_code");

            migrationBuilder.CreateIndex(
                name: "IX_terminal_procedures_icao_ident",
                table: "terminal_procedures",
                column: "icao_ident");

            migrationBuilder.CreateIndex(
                name: "IX_terminal_procedures_pdf_file_name_airport_ident_chart_name",
                table: "terminal_procedures",
                columns: new[] { "pdf_file_name", "airport_ident", "chart_name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "terminal_procedures");

            migrationBuilder.CreateTable(
                name: "airport_diagrams",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    airport_ident = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    airport_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    chart_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    file_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    icao_ident = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_airport_diagrams", x => x.id);
                });

            migrationBuilder.UpdateData(
                table: "faa_publication_cycle",
                keyColumn: "Id",
                keyValue: 2,
                column: "publication_type",
                value: "AirportDiagram");

            migrationBuilder.CreateIndex(
                name: "IX_airport_diagrams_airport_ident",
                table: "airport_diagrams",
                column: "airport_ident");

            migrationBuilder.CreateIndex(
                name: "IX_airport_diagrams_file_name",
                table: "airport_diagrams",
                column: "file_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_airport_diagrams_icao_ident",
                table: "airport_diagrams",
                column: "icao_ident");
        }
    }
}
