using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Exceptions;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos.Performance;
using PreflightApi.Infrastructure.Interfaces;

namespace PreflightApi.Infrastructure.Services;

public class E6bCalculatorService : IE6bCalculatorService
{
    private readonly PreflightApiDbContext _context;
    private readonly IMetarService _metarService;
    private readonly ILogger<E6bCalculatorService> _logger;

    // Standard atmosphere constants (ICAO Doc 7488 / ISA)
    private const double StandardPressureInHg = 29.92;
    private const double SeaLevelStandardTempCelsius = 15.0;
    private const double StandardTempKelvin = 288.15; // 15°C in Kelvin
    private const double LapseRateCelsiusPerThousandFt = 2.0;
    private const double DensityAltitudeFactor = 120.0;
    private const double PressureLapseConstant = 6.8756e-6;
    private const double PressureExponent = 5.2559;
    private const double CloudBaseFactor = 400.0; // °C spread to feet AGL
    private const double SpeedOfSoundSeaLevelKt = 661.47; // a₀ at sea level standard
    private const double CelsiusToKelvinOffset = 273.15;
    private const double TropopauseAltitudeFt = 36089.0; // ISA tropopause
    private const double TropopausePressureRatio = 0.22336; // δ at tropopause
    private const double StratosphereExpConstant = 4.80634e-5; // exponential decay rate above tropopause

    public E6bCalculatorService(
        PreflightApiDbContext context,
        IMetarService metarService,
        ILogger<E6bCalculatorService> logger)
    {
        _context = context;
        _metarService = metarService;
        _logger = logger;
    }

    public async Task<AirportCrosswindResponseDto> GetCrosswindForAirportAsync(string icaoCodeOrIdent)
    {
        _logger.LogInformation("Calculating crosswind for airport: {IcaoCodeOrIdent}", icaoCodeOrIdent);

        // Get airport data
        var airport = await _context.Airports
            .FirstOrDefaultAsync(a =>
                a.IcaoId == icaoCodeOrIdent.ToUpperInvariant() ||
                a.ArptId == icaoCodeOrIdent.ToUpperInvariant());

        if (airport == null)
        {
            throw new AirportNotFoundException(icaoCodeOrIdent);
        }

        // Get METAR data
        var metar = await _metarService.GetMetarForAirport(icaoCodeOrIdent);

        // Validate METAR has required wind data
        if (metar.WindSpeedKt == null)
        {
            throw new WeatherDataMissingException("wind speed", icaoCodeOrIdent);
        }

        // Get runways for this airport
        var runways = await _context.Runways
            .Include(r => r.RunwayEnds)
            .Where(r => r.SiteNo == airport.SiteNo)
            .ToListAsync();

        // Parse wind direction (handle VRB)
        bool isVariableWind = metar.WindDirDegrees == "VRB" ||
                              string.IsNullOrEmpty(metar.WindDirDegrees) ||
                              !int.TryParse(metar.WindDirDegrees, out _);
        int? windDirection = isVariableWind ? null : int.Parse(metar.WindDirDegrees!);

        var runwayCrosswinds = new List<RunwayCrosswindComponentDto>();

        foreach (var runway in runways)
        {
            foreach (var runwayEnd in runway.RunwayEnds)
            {
                // Skip runway ends without true alignment
                if (runwayEnd.TrueAlignment == null)
                {
                    _logger.LogDebug("Skipping runway end {RunwayEndId} - no true alignment",
                        runwayEnd.RunwayEndId);
                    continue;
                }

                // Convert true heading to magnetic heading
                int magneticHeading = ConvertTrueToMagnetic(
                    runwayEnd.TrueAlignment.Value,
                    airport.MagVarn,
                    airport.MagHemis);

                // Calculate crosswind components
                var components = CalculateCrosswindComponents(
                    windDirection,
                    metar.WindSpeedKt.Value,
                    metar.WindGustKt,
                    magneticHeading,
                    isVariableWind);

                runwayCrosswinds.Add(new RunwayCrosswindComponentDto
                {
                    RunwayEndId = runwayEnd.RunwayEndId,
                    MagneticHeadingDegrees = magneticHeading,
                    CrosswindKt = components.Crosswind,
                    HeadwindKt = components.Headwind,
                    GustCrosswindKt = components.GustCrosswind,
                    GustHeadwindKt = components.GustHeadwind,
                    AbsoluteCrosswindKt = Math.Abs(components.Crosswind),
                    HasHeadwind = components.Headwind > 0
                });
            }
        }

        // Sort by absolute crosswind (ascending) then by headwind (descending) to recommend best runway
        var sortedRunways = runwayCrosswinds
            .OrderBy(r => r.AbsoluteCrosswindKt)
            .ThenByDescending(r => r.HeadwindKt)
            .ToList();

        // Recommend the runway with lowest crosswind that has a headwind (or lowest crosswind if all tailwinds)
        var recommended = sortedRunways
            .FirstOrDefault(r => r.HasHeadwind)?.RunwayEndId
            ?? sortedRunways.FirstOrDefault()?.RunwayEndId;

        return new AirportCrosswindResponseDto
        {
            AirportIdentifier = airport.IcaoId ?? airport.ArptId ?? icaoCodeOrIdent,
            WindDirectionDegrees = windDirection,
            WindSpeedKt = metar.WindSpeedKt.Value,
            WindGustKt = metar.WindGustKt,
            IsVariableWind = isVariableWind,
            RawMetar = metar.RawText,
            ObservationTime = metar.ObservationTime,
            Runways = sortedRunways,
            RecommendedRunway = recommended
        };
    }

    public CrosswindCalculationResponseDto CalculateCrosswind(CrosswindCalculationRequestDto request)
    {
        _logger.LogInformation(
            "Calculating crosswind for wind {WindDir}@{WindSpeed} runway {RunwayHeading}",
            request.WindDirectionDegrees,
            request.WindSpeedKt,
            request.RunwayHeadingDegrees);

        bool isVariableWind = request.WindDirectionDegrees == null;

        var components = CalculateCrosswindComponents(
            request.WindDirectionDegrees,
            request.WindSpeedKt,
            request.WindGustKt,
            request.RunwayHeadingDegrees,
            isVariableWind);

        return new CrosswindCalculationResponseDto
        {
            CrosswindKt = components.Crosswind,
            HeadwindKt = components.Headwind,
            GustCrosswindKt = components.GustCrosswind,
            GustHeadwindKt = components.GustHeadwind,
            WindDirectionDegrees = request.WindDirectionDegrees,
            WindSpeedKt = request.WindSpeedKt,
            WindGustKt = request.WindGustKt,
            RunwayHeadingDegrees = request.RunwayHeadingDegrees,
            IsVariableWind = isVariableWind
        };
    }

    public async Task<DensityAltitudeResponseDto> GetDensityAltitudeForAirportAsync(
        string icaoCodeOrIdent,
        AirportDensityAltitudeRequestDto? request = null)
    {
        _logger.LogInformation("Calculating density altitude for airport: {IcaoCodeOrIdent}", icaoCodeOrIdent);

        // Get airport data
        var airport = await _context.Airports
            .FirstOrDefaultAsync(a =>
                a.IcaoId == icaoCodeOrIdent.ToUpperInvariant() ||
                a.ArptId == icaoCodeOrIdent.ToUpperInvariant());

        if (airport == null)
        {
            throw new AirportNotFoundException(icaoCodeOrIdent);
        }

        if (airport.Elev == null)
        {
            throw new InvalidPerformanceDataException($"Airport {icaoCodeOrIdent} is missing elevation data");
        }

        // Get METAR data
        var metar = await _metarService.GetMetarForAirport(icaoCodeOrIdent);

        // Get temperature (with optional override)
        double temperatureCelsius;
        if (request?.TemperatureCelsiusOverride != null)
        {
            temperatureCelsius = request.TemperatureCelsiusOverride.Value;
        }
        else if (metar.TempC != null)
        {
            temperatureCelsius = metar.TempC.Value;
        }
        else
        {
            throw new WeatherDataMissingException("temperature", $"{icaoCodeOrIdent} (no override provided)");
        }

        // Get altimeter (with optional override)
        double altimeterInHg;
        if (request?.AltimeterInHgOverride != null)
        {
            altimeterInHg = request.AltimeterInHgOverride.Value;
        }
        else if (metar.AltimInHg != null)
        {
            altimeterInHg = metar.AltimInHg.Value;
        }
        else
        {
            throw new WeatherDataMissingException("altimeter", $"{icaoCodeOrIdent} (no override provided)");
        }

        var result = CalculateDensityAltitudeInternal(
            (double)airport.Elev.Value,
            altimeterInHg,
            temperatureCelsius);

        return new DensityAltitudeResponseDto
        {
            AirportIdentifier = airport.IcaoId ?? airport.ArptId ?? icaoCodeOrIdent,
            FieldElevationFt = (double)airport.Elev.Value,
            PressureAltitudeFt = result.PressureAltitude,
            DensityAltitudeFt = result.DensityAltitude,
            IsaTemperatureCelsius = result.IsaTemperature,
            ActualTemperatureCelsius = temperatureCelsius,
            TemperatureDeviationCelsius = result.TemperatureDeviation,
            AltimeterInHg = altimeterInHg,
            RawMetar = metar.RawText,
            ObservationTime = metar.ObservationTime
        };
    }

    public DensityAltitudeResponseDto CalculateDensityAltitude(DensityAltitudeRequestDto request)
    {
        _logger.LogInformation(
            "Calculating density altitude for elevation {Elev}ft, altimeter {Altim}inHg, temp {Temp}C",
            request.FieldElevationFt,
            request.AltimeterInHg,
            request.TemperatureCelsius);

        var result = CalculateDensityAltitudeInternal(
            request.FieldElevationFt,
            request.AltimeterInHg,
            request.TemperatureCelsius);

        return new DensityAltitudeResponseDto
        {
            FieldElevationFt = request.FieldElevationFt,
            PressureAltitudeFt = result.PressureAltitude,
            DensityAltitudeFt = result.DensityAltitude,
            IsaTemperatureCelsius = result.IsaTemperature,
            ActualTemperatureCelsius = request.TemperatureCelsius,
            TemperatureDeviationCelsius = result.TemperatureDeviation,
            AltimeterInHg = request.AltimeterInHg
        };
    }

    public WindTriangleResponseDto CalculateWindTriangle(WindTriangleRequestDto request)
    {
        if (request.TrueAirspeedKt <= 0)
            throw new ValidationException("TrueAirspeedKt", "True airspeed must be greater than 0.");
        if (request.WindSpeedKt < 0)
            throw new ValidationException("WindSpeedKt", "Wind speed cannot be negative.");
        if (request.TrueCourseDegrees < 0 || request.TrueCourseDegrees > 360)
            throw new ValidationException("TrueCourseDegrees", "True course must be between 0 and 360 degrees.");
        if (request.WindDirectionDegrees < 0 || request.WindDirectionDegrees > 360)
            throw new ValidationException("WindDirectionDegrees", "Wind direction must be between 0 and 360 degrees.");

        _logger.LogInformation(
            "Calculating wind triangle: TC={TC}° TAS={TAS}kt Wind={WindDir}°@{WindSpd}kt",
            request.TrueCourseDegrees, request.TrueAirspeedKt,
            request.WindDirectionDegrees, request.WindSpeedKt);

        var result = CalculateWindTriangleInternal(
            request.TrueCourseDegrees, request.TrueAirspeedKt,
            request.WindDirectionDegrees, request.WindSpeedKt);

        return new WindTriangleResponseDto
        {
            TrueHeadingDegrees = result.TrueHeading,
            GroundSpeedKt = result.GroundSpeed,
            WindCorrectionAngleDegrees = result.WindCorrectionAngle,
            HeadwindComponentKt = result.HeadwindComponent,
            CrosswindComponentKt = result.CrosswindComponent,
            TrueCourseDegrees = request.TrueCourseDegrees,
            TrueAirspeedKt = request.TrueAirspeedKt,
            WindDirectionDegrees = request.WindDirectionDegrees,
            WindSpeedKt = request.WindSpeedKt
        };
    }

    public TrueAirspeedResponseDto CalculateTrueAirspeed(TrueAirspeedRequestDto request)
    {
        if (request.CalibratedAirspeedKt <= 0)
            throw new ValidationException("CalibratedAirspeedKt", "Calibrated airspeed must be greater than 0.");
        if (request.OutsideAirTemperatureCelsius < -70 || request.OutsideAirTemperatureCelsius > 60)
            throw new ValidationException("OutsideAirTemperatureCelsius", "Temperature must be between -70°C and 60°C.");

        _logger.LogInformation(
            "Calculating TAS: CAS={CAS}kt PA={PA}ft OAT={OAT}°C",
            request.CalibratedAirspeedKt, request.PressureAltitudeFt, request.OutsideAirTemperatureCelsius);

        var result = CalculateTrueAirspeedInternal(
            request.CalibratedAirspeedKt, request.PressureAltitudeFt, request.OutsideAirTemperatureCelsius);

        return new TrueAirspeedResponseDto
        {
            TrueAirspeedKt = result.TrueAirspeed,
            DensityAltitudeFt = result.DensityAltitude,
            MachNumber = result.MachNumber,
            CalibratedAirspeedKt = request.CalibratedAirspeedKt,
            PressureAltitudeFt = request.PressureAltitudeFt,
            OutsideAirTemperatureCelsius = request.OutsideAirTemperatureCelsius
        };
    }

    public CloudBaseResponseDto CalculateCloudBase(CloudBaseRequestDto request)
    {
        if (request.DewpointCelsius > request.TemperatureCelsius)
            throw new ValidationException("DewpointCelsius", "Dewpoint cannot exceed temperature.");

        _logger.LogInformation(
            "Calculating cloud base: Temp={Temp}°C Dewpoint={Dewpoint}°C",
            request.TemperatureCelsius, request.DewpointCelsius);

        double spread = request.TemperatureCelsius - request.DewpointCelsius;
        double cloudBaseFtAgl = Math.Round(spread * CloudBaseFactor, 0);

        return new CloudBaseResponseDto
        {
            EstimatedCloudBaseFtAgl = cloudBaseFtAgl,
            TemperatureDewpointSpreadCelsius = Math.Round(spread, 1),
            TemperatureCelsius = request.TemperatureCelsius,
            DewpointCelsius = request.DewpointCelsius
        };
    }

    public PressureAltitudeResponseDto CalculatePressureAltitude(PressureAltitudeRequestDto request)
    {
        if (request.AltimeterInHg < 25.0 || request.AltimeterInHg > 35.0)
            throw new ValidationException("AltimeterInHg", "Altimeter setting must be between 25.0 and 35.0 inHg.");

        _logger.LogInformation(
            "Calculating pressure altitude: Elevation={Elev}ft Altimeter={Altim}inHg",
            request.FieldElevationFt, request.AltimeterInHg);

        double altimeterCorrection = (StandardPressureInHg - request.AltimeterInHg) * 1000;
        double pressureAltitude = request.FieldElevationFt + altimeterCorrection;

        return new PressureAltitudeResponseDto
        {
            PressureAltitudeFt = Math.Round(pressureAltitude, 0),
            AltimeterCorrectionFt = Math.Round(altimeterCorrection, 0),
            FieldElevationFt = request.FieldElevationFt,
            AltimeterInHg = request.AltimeterInHg
        };
    }

    private static int ConvertTrueToMagnetic(int trueHeading, decimal? magVarn, string? magHemis)
    {
        if (magVarn == null)
        {
            return trueHeading;
        }

        int variation = (int)magVarn.Value;

        // East variation: subtract from true to get magnetic
        // West variation: add to true to get magnetic
        int magneticHeading = magHemis?.ToUpperInvariant() == "E"
            ? trueHeading - variation
            : trueHeading + variation;

        // Normalize to 0-360
        while (magneticHeading <= 0)
            magneticHeading += 360;
        while (magneticHeading > 360)
            magneticHeading -= 360;

        return magneticHeading;
    }

    private static (double Crosswind, double Headwind, double? GustCrosswind, double? GustHeadwind)
        CalculateCrosswindComponents(int? windDirection, int windSpeed, int? gustSpeed, int runwayHeading, bool isVariableWind)
    {
        // For calm winds, all components are zero
        if (windSpeed == 0)
        {
            return (0, 0, null, null);
        }

        // For variable winds, assume worst case (full crosswind)
        if (isVariableWind)
        {
            double? gustCrosswind = gustSpeed.HasValue ? (double)gustSpeed.Value : null;
            return (windSpeed, 0, gustCrosswind, 0);
        }

        // Calculate wind angle relative to runway
        int windAngle = windDirection!.Value - runwayHeading;

        // Normalize to -180 to +180
        while (windAngle > 180)
            windAngle -= 360;
        while (windAngle < -180)
            windAngle += 360;

        // Convert to radians
        double windAngleRadians = windAngle * Math.PI / 180.0;

        // Calculate components
        // Crosswind: positive = from the right, negative = from the left
        double crosswind = Math.Round(windSpeed * Math.Sin(windAngleRadians), 1);
        // Headwind: positive = headwind, negative = tailwind
        double headwind = Math.Round(windSpeed * Math.Cos(windAngleRadians), 1);

        // Calculate gust components if applicable
        double? gustCrosswindResult = null;
        double? gustHeadwindResult = null;
        if (gustSpeed.HasValue)
        {
            gustCrosswindResult = Math.Round(gustSpeed.Value * Math.Sin(windAngleRadians), 1);
            gustHeadwindResult = Math.Round(gustSpeed.Value * Math.Cos(windAngleRadians), 1);
        }

        return (crosswind, headwind, gustCrosswindResult, gustHeadwindResult);
    }

    private static (double PressureAltitude, double DensityAltitude, double IsaTemperature, double TemperatureDeviation)
        CalculateDensityAltitudeInternal(double fieldElevationFt, double altimeterInHg, double temperatureCelsius)
    {
        // Calculate pressure altitude
        // PA = Field Elevation + ((29.92 - Altimeter) * 1000)
        double pressureAltitude = fieldElevationFt + ((StandardPressureInHg - altimeterInHg) * 1000);

        // Calculate ISA (standard) temperature at this pressure altitude
        // ISA Temp = 15 - (PA / 1000) * 2
        double isaTemperature = SeaLevelStandardTempCelsius - (pressureAltitude / 1000) * LapseRateCelsiusPerThousandFt;

        // Calculate temperature deviation from standard
        double temperatureDeviation = temperatureCelsius - isaTemperature;

        // Calculate density altitude
        // DA = PA + (120 * Temperature Deviation)
        double densityAltitude = pressureAltitude + (DensityAltitudeFactor * temperatureDeviation);

        return (
            Math.Round(pressureAltitude, 0),
            Math.Round(densityAltitude, 0),
            Math.Round(isaTemperature, 1),
            Math.Round(temperatureDeviation, 1)
        );
    }

    private static (double TrueHeading, double GroundSpeed, double WindCorrectionAngle,
        double HeadwindComponent, double CrosswindComponent)
        CalculateWindTriangleInternal(double trueCourse, double tas, double windDirection, double windSpeed)
    {
        if (windSpeed == 0)
            return (NormalizeDegrees(trueCourse), tas, 0, 0, 0);

        double tcRad = trueCourse * Math.PI / 180.0;
        double wdRad = windDirection * Math.PI / 180.0;

        // Wind angle relative to course (wind blowing FROM windDirection TOWARD the aircraft's course)
        double windAngle = wdRad - tcRad;

        // Wind correction angle: arcsin((windSpeed * sin(windDir - trueCourse)) / TAS)
        double sinWca = windSpeed * Math.Sin(windAngle) / tas;

        // Clamp to [-1, 1] for safety when wind speed approaches/exceeds TAS
        sinWca = Math.Clamp(sinWca, -1.0, 1.0);
        double wca = Math.Asin(sinWca);
        double wcaDegrees = wca * 180.0 / Math.PI;

        // True heading = true course + WCA
        double trueHeading = NormalizeDegrees(trueCourse + wcaDegrees);

        // Ground speed = TAS * cos(WCA) - windSpeed * cos(windDirection - trueCourse)
        // Minus because wind direction is FROM, so the wind vector opposes the course component
        double groundSpeed = tas * Math.Cos(wca) - windSpeed * Math.Cos(windAngle);
        groundSpeed = Math.Max(0, groundSpeed); // Ground speed can't be negative

        // Headwind component (positive = headwind, negative = tailwind)
        // Wind blowing FROM windDirection; headwind is the component along the course direction
        double headwindComponent = windSpeed * Math.Cos(windAngle);

        // Crosswind component (positive = from right, negative = from left)
        double crosswindComponent = windSpeed * Math.Sin(windAngle);

        return (
            Math.Round(trueHeading, 1),
            Math.Round(groundSpeed, 1),
            Math.Round(wcaDegrees, 1),
            Math.Round(headwindComponent, 1),
            Math.Round(crosswindComponent, 1)
        );
    }

    private static (double TrueAirspeed, double DensityAltitude, double MachNumber)
        CalculateTrueAirspeedInternal(double cas, double pressureAltitude, double oatCelsius)
    {
        // Full compressible CAS-to-TAS conversion using isentropic flow relations.
        // Accurate at all subsonic speeds and altitudes including above the tropopause.
        // Reference: ICAO Doc 7488 (Standard Atmosphere), isentropic flow equations.
        //
        // Steps:
        //   1. CAS → impact pressure (qc) using sea-level standard conditions
        //   2. Pressure ratio (δ) at altitude (troposphere + stratosphere)
        //   3. Impact pressure ratio at altitude → Mach number
        //   4. Mach × local speed of sound → TAS

        double oatKelvin = oatCelsius + CelsiusToKelvinOffset;

        // Step 1: Impact pressure ratio from CAS at sea-level standard
        // qc/P₀ = (1 + 0.2 × (CAS/a₀)²)^3.5 − 1
        double casRatio = cas / SpeedOfSoundSeaLevelKt;
        double qcOverP0 = Math.Pow(1 + 0.2 * casRatio * casRatio, 3.5) - 1;

        // Step 2: Pressure ratio (δ) at altitude
        double delta = CalculatePressureRatio(pressureAltitude);

        // Step 3: Impact pressure ratio at altitude and Mach number
        // qc/P = (qc/P₀) / δ
        double qcOverP = qcOverP0 / delta;

        // M = √(5 × ((qc/P + 1)^(2/7) − 1))
        double machNumber = Math.Sqrt(5.0 * (Math.Pow(qcOverP + 1, 2.0 / 7.0) - 1));

        // Step 4: TAS from Mach number and OAT
        // θ = OAT_K / 288.15 (temperature ratio)
        // a_local = a₀ × √θ (local speed of sound)
        // TAS = M × a_local
        double theta = oatKelvin / StandardTempKelvin;
        double localSpeedOfSound = SpeedOfSoundSeaLevelKt * Math.Sqrt(theta);
        double tas = machNumber * localSpeedOfSound;

        // Density altitude using ISA temperature deviation method
        double isaTempCelsius = pressureAltitude <= TropopauseAltitudeFt
            ? SeaLevelStandardTempCelsius - (pressureAltitude / 1000.0) * LapseRateCelsiusPerThousandFt
            : -56.5; // ISA temperature is constant above the tropopause
        double tempDeviation = oatCelsius - isaTempCelsius;
        double densityAltitude = pressureAltitude + DensityAltitudeFactor * tempDeviation;

        return (
            Math.Round(tas, 1),
            Math.Round(densityAltitude, 0),
            Math.Round(machNumber, 3)
        );
    }

    /// <summary>
    /// Calculates the ISA pressure ratio (δ) at a given pressure altitude.
    /// Handles both the troposphere (below 36,089 ft) and lower stratosphere (above).
    /// </summary>
    private static double CalculatePressureRatio(double pressureAltitudeFt)
    {
        if (pressureAltitudeFt <= TropopauseAltitudeFt)
        {
            // Troposphere: δ = (1 − 6.8756×10⁻⁶ × PA)^5.2559
            return Math.Pow(1 - PressureLapseConstant * pressureAltitudeFt, PressureExponent);
        }

        // Stratosphere (isothermal layer): δ = 0.22336 × e^(−4.80634×10⁻⁵ × (PA − 36089))
        return TropopausePressureRatio * Math.Exp(-StratosphereExpConstant * (pressureAltitudeFt - TropopauseAltitudeFt));
    }

    private static double NormalizeDegrees(double degrees)
    {
        double result = degrees % 360.0;
        if (result <= 0)
            result += 360.0;
        return result;
    }
}
