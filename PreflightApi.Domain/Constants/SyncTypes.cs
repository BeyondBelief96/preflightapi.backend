namespace PreflightApi.Domain.Constants
{
    public static class SyncTypes
    {
        // Time-based (weather / real-time data)
        public const string Metar = "Metar";
        public const string Taf = "Taf";
        public const string Pirep = "Pirep";
        public const string Sigmet = "Sigmet";
        public const string GAirmet = "GAirmet";
        public const string NotamDelta = "NotamDelta";
        public const string ObstacleDailyChange = "ObstacleDailyChange";

        // Cycle-based (FAA publication data)
        public const string Airport = "Airport";
        public const string Frequency = "Frequency";
        public const string Airspace = "Airspace";
        public const string SpecialUseAirspace = "SpecialUseAirspace";
        public const string Obstacle = "Obstacle";
        public const string ChartSupplement = "ChartSupplement";
        public const string TerminalProcedure = "TerminalProcedure";
        public const string Navaid = "Navaid";
        public const string RunwayGeometry = "RunwayGeometry";
    }
}
