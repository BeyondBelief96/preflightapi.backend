using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Utilities;

namespace PreflightApi.Infrastructure.Dtos.Mappers;

public static class PirepMapper
{
    private static readonly Dictionary<string, PirepReportType> ReportTypeMap = new()
    {
        ["UA"] = PirepReportType.UA,
        ["UUA"] = PirepReportType.UUA,
        ["PIREP"] = PirepReportType.PIREP,
        ["AIREP"] = PirepReportType.AIREP,
        ["URGENT PIREP"] = PirepReportType.UUA
    };

    public static PirepDto ToDto(Pirep pirep, ILogger logger)
    {
        return new PirepDto
        {
            Id = pirep.Id,
            RawText = pirep.RawText,
            ReceiptTime = pirep.ReceiptTime,
            ObservationTime = pirep.ObservationTime,
            QualityControlFlags = pirep.QualityControlFlags,
            AircraftRef = pirep.AircraftRef,
            Latitude = pirep.Latitude,
            Longitude = pirep.Longitude,
            AltitudeFtMsl = pirep.AltitudeFtMsl,
            SkyConditions = pirep.SkyConditions,
            TurbulenceConditions = pirep.TurbulenceConditions,
            IcingConditions = pirep.IcingConditions,
            VisibilityStatuteMi = pirep.VisibilityStatuteMi,
            WxString = pirep.WxString,
            TempC = pirep.TempC,
            WindDirDegrees = pirep.WindDirDegrees,
            WindSpeedKt = pirep.WindSpeedKt,
            VertGustKt = pirep.VertGustKt,
            ReportType = EnumParseHelper.Parse(pirep.ReportType, logger, nameof(pirep.ReportType), nameof(Pirep), pirep.Id.ToString(), ReportTypeMap)
        };
    }
}
