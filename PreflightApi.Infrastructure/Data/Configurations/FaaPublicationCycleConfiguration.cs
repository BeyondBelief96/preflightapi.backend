using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.ValueObjects.FaaPublications;

namespace PreflightApi.Infrastructure.Data.Configurations
{
    internal class FaaPublicationCycleConfiguration : IEntityTypeConfiguration<FaaPublicationCycle>
    {
        public void Configure(EntityTypeBuilder<FaaPublicationCycle> builder)
        {
            builder.Property(e => e.PublicationType)
            .HasConversion<string>();

            builder.HasIndex(e => e.PublicationType).IsUnique();

            builder.HasData(
            new FaaPublicationCycle
            {
                Id = 1,
                PublicationType = PublicationType.ChartSupplement,
                CycleLengthDays = 56,
                KnownValidDate = new DateTime(2024, 12, 26, 0, 0, 0, DateTimeKind.Utc),
                LastSuccessfulUpdate = null
            },
            new FaaPublicationCycle
            {
                Id = 2,
                PublicationType = PublicationType.AirportDiagram,
                CycleLengthDays = 28,
                KnownValidDate = new DateTime(2024, 12, 26, 0, 0, 0, DateTimeKind.Utc),
                LastSuccessfulUpdate = null
            },
            new FaaPublicationCycle
            {
                Id = 3,
                PublicationType = PublicationType.NasrSubscription_Airport,
                CycleLengthDays = 28,
                KnownValidDate = new DateTime(2025, 1, 23, 0, 0, 0, DateTimeKind.Utc)
            },
            new FaaPublicationCycle
            {
                Id = 4,
                PublicationType = PublicationType.NasrSubscription_Frequencies,
                CycleLengthDays = 28,
                KnownValidDate = new DateTime(2025, 1, 23, 0, 0, 0, DateTimeKind.Utc)
            },
            new FaaPublicationCycle
            {
                Id = 5,
                PublicationType = PublicationType.Airspaces,
                CycleLengthDays = 56,
                KnownValidDate = new DateTime(2024, 12, 26, 0, 0, 0, DateTimeKind.Utc),
                LastSuccessfulUpdate = null
            },
            new FaaPublicationCycle
            {
                Id = 6,
                PublicationType = PublicationType.SpecialUseAirspaces,
                CycleLengthDays = 56,
                KnownValidDate = new DateTime(2024, 12, 26, 0, 0, 0, DateTimeKind.Utc),
                LastSuccessfulUpdate = null
            },
            new FaaPublicationCycle
            {
                Id = 7,
                PublicationType = PublicationType.Obstacles,
                CycleLengthDays = 56,
                // KnownValidDate uses the release date (2 days after "Reflects Changes To")
                KnownValidDate = new DateTime(2025, 10, 28, 0, 0, 0, DateTimeKind.Utc),
                LastSuccessfulUpdate = null
            });
        }
    }
}
