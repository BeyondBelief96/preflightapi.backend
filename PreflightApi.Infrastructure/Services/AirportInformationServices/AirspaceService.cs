using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using PreflightApi.Infrastructure.Data;
using PreflightApi.Infrastructure.Dtos;
using PreflightApi.Infrastructure.Dtos.Mappers;
using PreflightApi.Infrastructure.Dtos.Navlog;
using PreflightApi.Infrastructure.Dtos.Pagination;
using PreflightApi.Infrastructure.Interfaces;
using PreflightApi.Infrastructure.Utilities;

namespace PreflightApi.Infrastructure.Services.AirportInformationServices
{
    public class AirspaceService : IAirspaceService
    {
        private readonly PreflightApiDbContext _context;
        private readonly ILogger<AirspaceService> _logger;
        private readonly GeometryFactory _geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        public AirspaceService(
            PreflightApiDbContext context,
            ILogger<AirspaceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PaginatedResponse<AirspaceDto>> GetByClasses(string[] airspaceClasses, string? cursor = null, int limit = 100)
        {
            try
            {
                _logger.LogInformation("Getting airspaces by classes: {Classes}, cursor: {Cursor}, limit: {Limit}",
                    string.Join(", ", airspaceClasses), cursor, limit);

                var upperClasses = airspaceClasses.Select(c => c.ToUpperInvariant()).ToArray();
                var query = _context.Airspaces
                    .Where(a => a.Class != null && upperClasses.Contains(a.Class));

                return await query.ToPaginatedAsync(a => a.GlobalId, AirspaceMapper.ToDto, cursor, limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting airspaces by classes: {Classes}",
                    string.Join(", ", airspaceClasses));
                throw;
            }
        }

        public async Task<PaginatedResponse<AirspaceDto>> GetByCities(string[] cities, string? cursor = null, int limit = 100)
        {
            try
            {
                _logger.LogInformation("Getting airspaces by cities: {Cities}, cursor: {Cursor}, limit: {Limit}",
                    string.Join(", ", cities), cursor, limit);

                var upperCities = cities.Select(c => c.ToUpperInvariant()).ToArray();
                var query = _context.Airspaces
                    .Where(a => a.City != null && upperCities.Contains(a.City.ToUpper()));

                return await query.ToPaginatedAsync(a => a.GlobalId, AirspaceMapper.ToDto, cursor, limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting airspaces by cities: {Cities}",
                    string.Join(", ", cities));
                throw;
            }
        }

        public async Task<PaginatedResponse<AirspaceDto>> GetByStates(string[] states, string? cursor = null, int limit = 100)
        {
            try
            {
                _logger.LogInformation("Getting airspaces by states: {States}, cursor: {Cursor}, limit: {Limit}",
                    string.Join(", ", states), cursor, limit);

                var upperStates = states.Select(s => s.ToUpperInvariant()).ToArray();
                var query = _context.Airspaces
                    .Where(a => a.State != null && upperStates.Contains(a.State));

                return await query.ToPaginatedAsync(a => a.GlobalId, AirspaceMapper.ToDto, cursor, limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting airspaces by states: {States}",
                    string.Join(", ", states));
                throw;
            }
        }

        public async Task<PaginatedResponse<SpecialUseAirspaceDto>> GetByTypeCodes(string[] typeCodes, string? cursor = null, int limit = 100)
        {
            try
            {
                _logger.LogInformation("Getting special use airspaces by type codes: {TypeCodes}, cursor: {Cursor}, limit: {Limit}",
                    string.Join(", ", typeCodes), cursor, limit);

                var upperTypeCodes = typeCodes.Select(t => t.ToUpperInvariant()).ToArray();
                var query = _context.SpecialUseAirspaces
                    .Where(a => a.TypeCode != null && upperTypeCodes.Contains(a.TypeCode));

                return await query.ToPaginatedAsync(a => a.GlobalId, AirspaceMapper.ToDto, cursor, limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting special use airspaces by type codes: {TypeCodes}",
                    string.Join(", ", typeCodes));
                throw;
            }
        }

        public async Task<IEnumerable<AirspaceDto>> GetByIcaoOrIdents(string[] icaoOrIdents)
        {
            try
            {
                _logger.LogInformation("Getting airspaces by ICAO codes or idents: {IcaoOrIdents}",
                    string.Join(", ", icaoOrIdents));

                var upperCodes = icaoOrIdents.Select(i => i.ToUpperInvariant()).ToArray();
                var airspaces = await _context.Airspaces.AsNoTracking()
                    .Where(a =>
                        a.IcaoId != null && upperCodes.Contains(a.IcaoId) ||
                        a.Ident != null && upperCodes.Contains(a.Ident))
                    .OrderBy(a => a.Name)
                    .ToListAsync();

                return airspaces.Select(AirspaceMapper.ToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting airspaces by ICAO codes or idents: {IcaoOrIdents}",
                    string.Join(", ", icaoOrIdents));
                throw;
            }
        }

        public async Task<IEnumerable<AirspaceDto>> GetByGlobalIds(string[] globalIds)
        {
            try
            {
                _logger.LogInformation("Getting airspaces by global ids: {Ids}", string.Join(", ", globalIds));

                var upperIds = globalIds.Select(i => i.ToUpperInvariant()).ToArray();
                var airspaces = await _context.Airspaces.AsNoTracking()
                    .Where(a => a.GlobalId != null && upperIds.Contains(a.GlobalId.ToUpper()))
                    .ToListAsync();

                return airspaces.Select(AirspaceMapper.ToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting airspaces by global ids: {Ids}", string.Join(", ", globalIds));
                throw;
            }
        }

        public async Task<IEnumerable<SpecialUseAirspaceDto>> GetSpecialUseByGlobalIds(string[] globalIds)
        {
            try
            {
                _logger.LogInformation("Getting special use airspaces by global ids: {Ids}", string.Join(", ", globalIds));

                var upperIds = globalIds.Select(i => i.ToUpperInvariant()).ToArray();
                var airspaces = await _context.SpecialUseAirspaces.AsNoTracking()
                    .Where(a => a.GlobalId != null && upperIds.Contains(a.GlobalId.ToUpper()))
                    .ToListAsync();

                return airspaces.Select(AirspaceMapper.ToDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting special use airspaces by global ids: {Ids}", string.Join(", ", globalIds));
                throw;
            }
        }

        public async Task<IReadOnlyCollection<string>> GetAirspaceGlobalIdsForRouteAsync(
            IEnumerable<WaypointDto> waypoints,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var route = BuildRouteLineString(waypoints);
                if (route == null) return Array.Empty<string>();

                var routeEnvelope = route.Envelope as Polygon;
                if (routeEnvelope == null) return Array.Empty<string>();

                // Prefilter by route bounding box, then intersect the actual line
                var query = _context.Airspaces.AsNoTracking()
                    .Where(a => a.Geometry != null)
                    .Where(a => a.Geometry!.Intersects(routeEnvelope))
                    .Where(a => a.Geometry!.Intersects(route))
                    .Where(a => a.Class != "E") // Exclude "E" class airspaces
                    .Where(a =>
                        // Include if ICAO starts with US prefixes
                        (a.IcaoId != null && (
                            a.IcaoId.StartsWith("K") ||
                            a.IcaoId.StartsWith("PH") ||
                            a.IcaoId.StartsWith("PA")))
                        ||
                        // OR include if Ident starts with US prefixes
                        (a.Ident != null && (
                            a.Ident.StartsWith("K") ||
                            a.Ident.StartsWith("PH") ||
                            a.Ident.StartsWith("PA")))
                    )
                    .Select(a => a.GlobalId!)
                    .Distinct();

                // Include airspaces for airport waypoints explicitly by ICAO/Ident
                var airportCodes = ExtractAirportCodes(waypoints);
                if (airportCodes.Count > 0)
                {
                    var airportAirspaceIds = _context.Airspaces.AsNoTracking()
                        .Where(a => a.IcaoId != null && airportCodes.Contains(a.IcaoId)
                                    || a.Ident != null && airportCodes.Contains(a.Ident))
                        .Select(a => a.GlobalId!);

                    query = query.Union(airportAirspaceIds);
                }

                var ids = await query.ToListAsync(cancellationToken);
                return ids;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting airspace IDs for route");
                throw;
            }
        }

        public async Task<IReadOnlyCollection<string>> GetSpecialUseAirspaceGlobalIdsForRouteAsync(
            IEnumerable<WaypointDto> waypoints,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var route = BuildRouteLineString(waypoints);
                if (route == null) return Array.Empty<string>();

                var routeEnvelope = route.Envelope as Polygon;
                if (routeEnvelope == null) return Array.Empty<string>();

                var ids = await _context.SpecialUseAirspaces.AsNoTracking()
                    .Where(s => s.Geometry != null)
                    .Where(s => s.Geometry!.Intersects(routeEnvelope))
                    .Where(s => s.Geometry!.Intersects(route))
                    .Select(s => s.GlobalId!)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                return ids;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting special use airspace IDs for route");
                throw;
            }
        }

        private LineString? BuildRouteLineString(IEnumerable<WaypointDto> waypoints)
        {
            var coords = waypoints
                .Select(w => new Coordinate(w.Longitude, w.Latitude))
                .ToArray();

            if (coords.Length < 2) return null;
            return _geometryFactory.CreateLineString(coords);
        }

        private static HashSet<string> ExtractAirportCodes(IEnumerable<WaypointDto> waypoints)
        {
            var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var waypoint in waypoints)
            {
                if (waypoint.WaypointType?.ToString().Equals("Airport", StringComparison.OrdinalIgnoreCase) != true)
                {
                    continue;
                }

                var raw = waypoint.Name?.Trim();
                if (string.IsNullOrWhiteSpace(raw)) continue;

                var code = raw.ToUpperInvariant();

                // Case A: 4-letter ICAO provided
                if (code.Length == 4)
                {
                    // Always include the ICAO as-is
                    codes.Add(code);

                    // Derive likely FAA 3-letter identifier
                    if (code.StartsWith("K", StringComparison.Ordinal))
                    {
                        // Continental US: K + 3-letter FAA LID → strip K
                        codes.Add(code[1..]);
                    }
                    else if (code.StartsWith("PA", StringComparison.Ordinal) || code.StartsWith("PH", StringComparison.Ordinal))
                    {
                        // Alaska (PAxx) / Hawaii (PHxx): common FAA LID is last 3 letters
                        codes.Add(code[^3..]);
                    }
                }
                // Case B: 3-letter FAA LID provided
                else if (code.Length == 3)
                {
                    // Include the FAA LID
                    codes.Add(code);

                    // Also include a likely ICAO for lower 48 by prefixing K
                    codes.Add("K" + code);

                    // We intentionally don't guess PA/PH for 3-letter codes,
                    // as mapping isn't deterministic without a lookup.
                }
                else
                {
                    // Non-standard lengths: include as-is only
                    codes.Add(code);
                }
            }

            return codes;
        }
    }
}
