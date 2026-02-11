using PreflightApi.Domain.Entities;
using PreflightApi.Domain.Enums;

namespace PreflightApi.Infrastructure.Dtos.Mappers;

public static class AirportMapper
{
    public static AirportDto ToDto(Airport airport)
    {
        return new AirportDto
        {
            // Identification
            SiteNo = airport.SiteNo,
            IcaoId = airport.IcaoId,
            ArptId = airport.ArptId,
            ArptName = airport.ArptName,
            EffDate = airport.EffDate,

            // Classification
            SiteType = ParseSiteType(airport.SiteTypeCode),
            OwnershipType = ParseOwnershipType(airport.OwnershipTypeCode),
            FacilityUse = ParseFacilityUse(airport.FacilityUseCode),
            ArptStatus = ParseStatus(airport.ArptStatus),
            NaspCode = airport.NaspCode,

            // Location
            City = airport.City,
            StateCode = airport.StateCode,
            CountryCode = airport.CountryCode,
            StateName = airport.StateName,
            RegionCode = airport.RegionCode,
            AdoCode = airport.AdoCode,
            CountyName = airport.CountyName,
            CountyAssocState = airport.CountyAssocState,
            DistCityToAirport = airport.DistCityToAirport,
            DirectionCode = airport.DirectionCode,
            Acreage = airport.Acreage,

            // Coordinates
            LatDecimal = airport.LatDecimal,
            LongDecimal = airport.LongDecimal,
            LatDeg = airport.LatDeg,
            LatMin = airport.LatMin,
            LatSec = airport.LatSec,
            LatHemis = airport.LatHemis,
            LongDeg = airport.LongDeg,
            LongMin = airport.LongMin,
            LongSec = airport.LongSec,
            LongHemis = airport.LongHemis,
            PositionSurveyMethod = ParseSurveyMethod(airport.SurveyMethodCode),
            ArptPsnSource = airport.ArptPsnSource,
            PositionSrcDate = airport.PositionSrcDate,

            // Elevation & Magnetic Variation
            Elev = airport.Elev,
            ElevationSurveyMethod = ParseSurveyMethod(airport.ElevMethodCode),
            ArptElevSource = airport.ArptElevSource,
            ElevationSrcDate = airport.ElevationSrcDate,
            MagVarn = airport.MagVarn,
            MagHemis = airport.MagHemis,
            MagVarnYear = airport.MagVarnYear,
            Tpa = airport.Tpa,

            // Charting
            ChartName = airport.ChartName,

            // ATC & Communication
            RespArtccId = airport.RespArtccId,
            ArtccName = airport.ArtccName,
            TwrTypeCode = airport.TwrTypeCode,
            FssOnAirport = ParseBool(airport.FssOnArptFlag),
            FssId = airport.FssId,
            FssName = airport.FssName,
            FssPhoneNumber = airport.FssPhoneNumber,
            TollFreeNumber = airport.TollFreeNumber,
            AltFssId = airport.AltFssId,
            AltFssName = airport.AltFssName,
            AltTollFreeNumber = airport.AltTollFreeNumber,
            NotamId = airport.NotamId,
            NotamAvailable = ParseBool(airport.NotamFlag),

            // Customs & Military
            CustomsPortOfEntry = ParseBool(airport.CustomsFlag),
            CustomsLandingRights = ParseBool(airport.LndgRightsFlag),
            JointUse = ParseBool(airport.JointUseFlag),
            MilitaryLandingRights = ParseBool(airport.MilLndgFlag),

            // Inspection
            InspectionMethod = ParseInspectionMethod(airport.InspectMethodCode),
            InspectorAgency = ParseInspectorAgency(airport.InspectorCode),
            LastInspection = airport.LastInspection,
            LastInfoResponse = airport.LastInfoResponse,

            // Services & Fuel
            FuelTypes = airport.FuelTypes,
            ContractFuelAvailable = ParseBool(airport.ContrFuelAvbl),
            AirframeRepairService = ParseRepairService(airport.AirframeRepairSerCode),
            PowerPlantRepairService = ParseRepairService(airport.PwrPlantRepairSer),
            BottledOxygenType = ParseOxygenType(airport.BottledOxyType),
            BulkOxygenType = ParseOxygenType(airport.BulkOxyType),
            OtherServices = airport.OtherServices,

            // Transient Storage
            TransientStorageBuoys = ParseBool(airport.TrnsStrgBuoyFlag),
            TransientStorageHangars = ParseBool(airport.TrnsStrgHgrFlag),
            TransientStorageTiedowns = ParseBool(airport.TrnsStrgTieFlag),

            // Lighting & Visual Aids
            LgtSked = airport.LgtSked,
            BcnLgtSked = airport.BcnLgtSked,
            BeaconLensColor = ParseBeaconLensColor(airport.BcnLensColor),
            SegmentedCircleMarker = ParseSegmentedCircleMarker(airport.SegCircleMkrFlag),
            WindIndicator = ParseWindIndicator(airport.WindIndcrFlag),

            // Fees & Misc
            LandingFee = ParseBool(airport.LndgFeeFlag),
            MedicalUse = ParseBool(airport.MedicalUseFlag),
            ActivationDate = airport.ActivationDate,
            MinOpNetwork = airport.MinOpNetwork,
            UserFeeFlag = airport.UserFeeFlag,
            Cta = airport.Cta,
            ComputerId = airport.ComputerId,

            // Certification
            Far139TypeCode = airport.Far139TypeCode,
            Far139CarrierSerCode = airport.Far139CarrierSerCode,
            ArffCertTypeDate = airport.ArffCertTypeDate,
            AspAnalysisDtrmCode = airport.AspAnalysisDtrmCode,

            // Attendance
            SkedSeqNo = airport.SkedSeqNo,
            AttendanceMonth = airport.AttendanceMonth,
            AttendanceDay = airport.AttendanceDay,
            AttendanceHours = airport.AttendanceHours,

            // Contact
            ContactTitle = airport.ContactTitle,
            ContactName = airport.ContactName,
            ContactAddress1 = airport.ContactAddress1,
            ContactAddress2 = airport.ContactAddress2,
            ContactCity = airport.ContactCity,
            ContactState = airport.ContactState,
            ContactZipCode = airport.ContactZipCode,
            ContactZipPlusFour = airport.ContactZipPlusFour,
            ContactPhoneNumber = airport.ContactPhoneNumber,
        };
    }

    private static bool ParseBool(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Trim().ToUpperInvariant() == "Y";
    }

    private static AirportSiteType ParseSiteType(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return AirportSiteType.Unknown;

        return code.Trim().ToUpperInvariant() switch
        {
            "A" => AirportSiteType.Airport,
            "H" => AirportSiteType.Heliport,
            "S" => AirportSiteType.SeaplaneBase,
            "G" => AirportSiteType.Gliderport,
            "U" => AirportSiteType.Ultralight,
            _ => AirportSiteType.Unknown
        };
    }

    private static AirportOwnershipType ParseOwnershipType(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return AirportOwnershipType.Unknown;

        return code.Trim().ToUpperInvariant() switch
        {
            "PU" => AirportOwnershipType.PubliclyOwned,
            "PR" => AirportOwnershipType.PrivatelyOwned,
            "MA" => AirportOwnershipType.AirForce,
            "MN" => AirportOwnershipType.Navy,
            "MR" => AirportOwnershipType.Army,
            _ => AirportOwnershipType.Unknown
        };
    }

    private static AirportFacilityUse ParseFacilityUse(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return AirportFacilityUse.Unknown;

        return code.Trim().ToUpperInvariant() switch
        {
            "PU" => AirportFacilityUse.PublicUse,
            "PR" => AirportFacilityUse.PrivateUse,
            _ => AirportFacilityUse.Unknown
        };
    }

    private static AirportStatus ParseStatus(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return AirportStatus.Unknown;

        return code.Trim().ToUpperInvariant() switch
        {
            "O" => AirportStatus.Operational,
            "CI" => AirportStatus.ClosedIndefinitely,
            "CP" => AirportStatus.ClosedPermanently,
            _ => AirportStatus.Unknown
        };
    }

    private static AirportInspectionMethod ParseInspectionMethod(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return AirportInspectionMethod.Unknown;

        return code.Trim().ToUpperInvariant() switch
        {
            "F" => AirportInspectionMethod.Federal,
            "S" => AirportInspectionMethod.State,
            "C" => AirportInspectionMethod.Contractor,
            "1" => AirportInspectionMethod.PublicUseMailout,
            "2" => AirportInspectionMethod.PrivateUseMailout,
            _ => AirportInspectionMethod.Unknown
        };
    }

    private static AirportInspectorAgency ParseInspectorAgency(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return AirportInspectorAgency.Unknown;

        return code.Trim().ToUpperInvariant() switch
        {
            "F" => AirportInspectorAgency.Faa,
            "S" => AirportInspectorAgency.State,
            "C" => AirportInspectorAgency.Contractor,
            _ => AirportInspectorAgency.Unknown
        };
    }

    private static RepairServiceAvailability ParseRepairService(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return RepairServiceAvailability.Unknown;

        return code.Trim().ToUpperInvariant() switch
        {
            "NONE" => RepairServiceAvailability.None,
            "MAJOR" => RepairServiceAvailability.Major,
            "MINOR" => RepairServiceAvailability.Minor,
            _ => RepairServiceAvailability.Unknown
        };
    }

    private static OxygenPressureType ParseOxygenType(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return OxygenPressureType.Unknown;

        return code.Trim().ToUpperInvariant() switch
        {
            "NONE" => OxygenPressureType.None,
            "HIGH" => OxygenPressureType.High,
            "LOW" => OxygenPressureType.Low,
            "HIGH/LOW" => OxygenPressureType.HighAndLow,
            _ => OxygenPressureType.Unknown
        };
    }

    private static BeaconLensColor ParseBeaconLensColor(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BeaconLensColor.Unknown;

        return code.Trim().ToUpperInvariant() switch
        {
            "CG" => BeaconLensColor.ClearGreen,
            "CY" => BeaconLensColor.ClearYellow,
            "CGY" => BeaconLensColor.ClearGreenYellow,
            "SCG" => BeaconLensColor.SplitClearGreen,
            "C" => BeaconLensColor.Clear,
            _ => BeaconLensColor.Unknown
        };
    }

    private static SegmentedCircleMarkerType ParseSegmentedCircleMarker(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return SegmentedCircleMarkerType.Unknown;

        return code.Trim().ToUpperInvariant() switch
        {
            "N" => SegmentedCircleMarkerType.None,
            "Y" => SegmentedCircleMarkerType.Yes,
            "Y-L" => SegmentedCircleMarkerType.YesLighted,
            _ => SegmentedCircleMarkerType.Unknown
        };
    }

    private static WindIndicatorType ParseWindIndicator(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return WindIndicatorType.Unknown;

        return code.Trim().ToUpperInvariant() switch
        {
            "N" => WindIndicatorType.None,
            "Y" => WindIndicatorType.Unlighted,
            "Y-L" => WindIndicatorType.Lighted,
            _ => WindIndicatorType.Unknown
        };
    }

    private static SurveyMethod ParseSurveyMethod(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return SurveyMethod.Unknown;

        return code.Trim().ToUpperInvariant() switch
        {
            "E" => SurveyMethod.Estimated,
            "S" => SurveyMethod.Surveyed,
            _ => SurveyMethod.Unknown
        };
    }
}
