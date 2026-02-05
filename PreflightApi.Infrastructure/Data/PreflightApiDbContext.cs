using Microsoft.EntityFrameworkCore;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.Entities;

namespace PreflightApi.Infrastructure.Data
{
    public class PreflightApiDbContext : DbContext
    {
        public PreflightApiDbContext(DbContextOptions<PreflightApiDbContext> options) : base(options)
        {
            
        }

        public virtual DbSet<Metar> Metars => Set<Metar>();

        public virtual DbSet<Taf> Tafs => Set<Taf>();

        public virtual DbSet<Pirep> Pireps => Set<Pirep>();

        public virtual DbSet<Airsigmet> Airsigmets => Set<Airsigmet>();

        public virtual DbSet<GAirmet> GAirmets => Set<GAirmet>();

        public virtual DbSet<ChartSupplement> ChartSupplements => Set<ChartSupplement>();

        public virtual DbSet<AirportDiagram> AirportDiagrams => Set<AirportDiagram>();

        public virtual DbSet<Airport> Airports => Set<Airport>();

        public virtual DbSet<CommunicationFrequency> CommunicationFrequencies => Set<CommunicationFrequency>();

        public virtual DbSet<Airspace> Airspaces => Set<Airspace>();

        public virtual DbSet<SpecialUseAirspace> SpecialUseAirspaces => Set<SpecialUseAirspace>();

        public virtual DbSet<FaaPublicationCycle> FaaPublicationCycles => Set<FaaPublicationCycle>();   
        
        public virtual DbSet<Flight> Flights => Set<Flight>();
        
        public virtual DbSet<AircraftPerformanceProfile> AircraftPerformanceProfiles => Set<AircraftPerformanceProfile>();

        public virtual DbSet<Aircraft> Aircraft => Set<Aircraft>();

        public virtual DbSet<Runway> Runways => Set<Runway>();

        public virtual DbSet<RunwayEnd> RunwayEnds => Set<RunwayEnd>();

        public virtual DbSet<Obstacle> Obstacles => Set<Obstacle>();

        public virtual DbSet<WeightBalanceProfile> WeightBalanceProfiles => Set<WeightBalanceProfile>();

        public virtual DbSet<WeightBalanceCalculation> WeightBalanceCalculations => Set<WeightBalanceCalculation>();

        public virtual DbSet<AircraftDocument> AircraftDocuments => Set<AircraftDocument>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(PreflightApiDbContext).Assembly);
        }
    }
}
