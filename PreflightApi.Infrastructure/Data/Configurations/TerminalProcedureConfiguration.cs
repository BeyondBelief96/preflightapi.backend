using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using PreflightApi.Domain.Entities;

namespace PreflightApi.Infrastructure.Data.Configurations
{
    public class TerminalProcedureConfiguration : IEntityTypeConfiguration<TerminalProcedure>
    {
        public void Configure(EntityTypeBuilder<TerminalProcedure> builder)
        {
            builder.ToTable("terminal_procedures");

            builder.Property(e => e.Id)
                .HasColumnName("id");

            builder.Property(e => e.AirportName)
                .HasColumnName("airport_name")
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(e => e.IcaoIdent)
                .HasColumnName("icao_ident")
                .HasMaxLength(4);

            builder.Property(e => e.AirportIdent)
                .HasColumnName("airport_ident")
                .HasMaxLength(4);

            builder.Property(e => e.ChartCode)
                .HasColumnName("chart_code")
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(e => e.ChartName)
                .HasColumnName("chart_name")
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(e => e.PdfFileName)
                .HasColumnName("pdf_file_name")
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(e => e.AmendmentNumber)
                .HasColumnName("amendment_number")
                .HasMaxLength(10);

            builder.Property(e => e.AmendmentDate)
                .HasColumnName("amendment_date")
                .HasMaxLength(20);

            // Indexes
            builder.HasIndex(e => e.IcaoIdent);
            builder.HasIndex(e => e.AirportIdent);
            builder.HasIndex(e => e.ChartCode);
            builder.HasIndex(e => new { e.PdfFileName, e.AirportIdent, e.ChartName })
                .IsUnique();
        }
    }
}
