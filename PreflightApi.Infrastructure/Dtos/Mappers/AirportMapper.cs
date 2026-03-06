using Microsoft.Extensions.Logging;
using PreflightApi.Domain.Entities;
using PreflightApi.Domain.Enums;
using PreflightApi.Infrastructure.Utilities;

namespace PreflightApi.Infrastructure.Dtos.Mappers;

public static class AirportMapper
{
    private static readonly Dictionary<string, AirportSiteType> SiteTypeMap = new()
    {
        ["A"] = AirportSiteType.Airport,
        ["H"] = AirportSiteType.Heliport,
        ["C"] = AirportSiteType.SeaplaneBase,
        ["B"] = AirportSiteType.Balloonport,
        ["G"] = AirportSiteType.Gliderport,
        ["U"] = AirportSiteType.Ultralight
    };

    private static readonly Dictionary<string, AirportOwnershipType> OwnershipTypeMap = new()
    {
        ["PU"] = AirportOwnershipType.PubliclyOwned,
        ["PR"] = AirportOwnershipType.PrivatelyOwned,
        ["MA"] = AirportOwnershipType.AirForce,
        ["MN"] = AirportOwnershipType.Navy,
        ["MR"] = AirportOwnershipType.Army,
        ["CG"] = AirportOwnershipType.CoastGuard
    };

    private static readonly Dictionary<string, AirportFacilityUse> FacilityUseMap = new()
    {
        ["PU"] = AirportFacilityUse.PublicUse,
        ["PR"] = AirportFacilityUse.PrivateUse
    };

    private static readonly Dictionary<string, AirportStatus> StatusMap = new()
    {
        ["O"] = AirportStatus.Operational,
        ["CI"] = AirportStatus.ClosedIndefinitely,
        ["CP"] = AirportStatus.ClosedPermanently
    };

    private static readonly Dictionary<string, AirportInspectionMethod> InspectionMethodMap = new()
    {
        ["F"] = AirportInspectionMethod.Federal,
        ["S"] = AirportInspectionMethod.State,
        ["C"] = AirportInspectionMethod.Contractor,
        ["1"] = AirportInspectionMethod.PublicUseMailout,
        ["2"] = AirportInspectionMethod.PrivateUseMailout
    };

    private static readonly Dictionary<string, AirportInspectorAgency> InspectorAgencyMap = new()
    {
        ["F"] = AirportInspectorAgency.Faa,
        ["S"] = AirportInspectorAgency.State,
        ["C"] = AirportInspectorAgency.Contractor,
        ["N"] = AirportInspectorAgency.Owner
    };

    private static readonly Dictionary<string, RepairServiceAvailability> RepairServiceMap = new()
    {
        ["NONE"] = RepairServiceAvailability.None,
        ["MAJOR"] = RepairServiceAvailability.Major,
        ["MINOR"] = RepairServiceAvailability.Minor
    };

    private static readonly Dictionary<string, OxygenPressureType> OxygenTypeMap = new()
    {
        ["NONE"] = OxygenPressureType.None,
        ["HIGH"] = OxygenPressureType.High,
        ["LOW"] = OxygenPressureType.Low,
        ["HIGH/LOW"] = OxygenPressureType.HighAndLow
    };

    private static readonly Dictionary<string, BeaconLensColor> BeaconLensColorMap = new()
    {
        ["WG"] = BeaconLensColor.WhiteGreen,
        ["WY"] = BeaconLensColor.WhiteYellow,
        ["WGY"] = BeaconLensColor.WhiteGreenYellow,
        ["SWG"] = BeaconLensColor.SplitWhiteGreen,
        ["W"] = BeaconLensColor.White,
        ["Y"] = BeaconLensColor.Yellow,
        ["G"] = BeaconLensColor.Green,
        ["N"] = BeaconLensColor.None
    };

    private static readonly Dictionary<string, SegmentedCircleMarkerType> SegmentedCircleMap = new()
    {
        ["N"] = SegmentedCircleMarkerType.None,
        ["Y"] = SegmentedCircleMarkerType.Yes,
        ["Y-L"] = SegmentedCircleMarkerType.YesLighted
    };

    private static readonly Dictionary<string, WindIndicatorType> WindIndicatorMap = new()
    {
        ["N"] = WindIndicatorType.None,
        ["Y"] = WindIndicatorType.Unlighted,
        ["Y-L"] = WindIndicatorType.Lighted
    };

    private static readonly Dictionary<string, SurveyMethod> SurveyMethodMap = new()
    {
        ["E"] = SurveyMethod.Estimated,
        ["S"] = SurveyMethod.Surveyed
    };

    public static AirportDto ToDto(Airport airport, ILogger logger)
    {
        var id = airport.SiteNo;
        return new AirportDto
        {
            // Identification
            SiteNo = airport.SiteNo,
            IcaoId = airport.IcaoId,
            ArptId = airport.ArptId,
            ArptName = airport.ArptName,
            EffDate = airport.EffDate,

            // Classification
            SiteType = EnumParseHelper.Parse(airport.SiteTypeCode, logger, nameof(airport.SiteTypeCode), nameof(Airport), id, SiteTypeMap),
            OwnershipType = EnumParseHelper.Parse(airport.OwnershipTypeCode, logger, nameof(airport.OwnershipTypeCode), nameof(Airport), id, OwnershipTypeMap),
            FacilityUse = EnumParseHelper.Parse(airport.FacilityUseCode, logger, nameof(airport.FacilityUseCode), nameof(Airport), id, FacilityUseMap),
            ArptStatus = EnumParseHelper.Parse(airport.ArptStatus, logger, nameof(airport.ArptStatus), nameof(Airport), id, StatusMap),
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
            DistCityToAirport = (double?)airport.DistCityToAirport,
            DirectionCode = airport.DirectionCode,
            Acreage = airport.Acreage,

            // Coordinates
            LatDecimal = (double?)airport.LatDecimal,
            LongDecimal = (double?)airport.LongDecimal,
            LatDeg = airport.LatDeg,
            LatMin = airport.LatMin,
            LatSec = (double?)airport.LatSec,
            LatHemis = airport.LatHemis,
            LongDeg = airport.LongDeg,
            LongMin = airport.LongMin,
            LongSec = (double?)airport.LongSec,
            LongHemis = airport.LongHemis,
            PositionSurveyMethod = EnumParseHelper.Parse(airport.SurveyMethodCode, logger, nameof(airport.SurveyMethodCode), nameof(Airport), id, SurveyMethodMap),
            ArptPsnSource = airport.ArptPsnSource,
            PositionSrcDate = airport.PositionSrcDate,

            // Elevation & Magnetic Variation
            Elev = (double?)airport.Elev,
            ElevationSurveyMethod = EnumParseHelper.Parse(airport.ElevMethodCode, logger, nameof(airport.ElevMethodCode), nameof(Airport), id, SurveyMethodMap),
            ArptElevSource = airport.ArptElevSource,
            ElevationSrcDate = airport.ElevationSrcDate,
            MagVarn = (double?)airport.MagVarn,
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
            InspectionMethod = EnumParseHelper.Parse(airport.InspectMethodCode, logger, nameof(airport.InspectMethodCode), nameof(Airport), id, InspectionMethodMap),
            InspectorAgency = EnumParseHelper.Parse(airport.InspectorCode, logger, nameof(airport.InspectorCode), nameof(Airport), id, InspectorAgencyMap),
            LastInspection = airport.LastInspection,
            LastInfoResponse = airport.LastInfoResponse,

            // Services & Fuel
            FuelTypes = airport.FuelTypes,
            ContractFuelAvailable = ParseBool(airport.ContrFuelAvbl),
            AirframeRepairService = EnumParseHelper.Parse(airport.AirframeRepairSerCode, logger, nameof(airport.AirframeRepairSerCode), nameof(Airport), id, RepairServiceMap),
            PowerPlantRepairService = EnumParseHelper.Parse(airport.PwrPlantRepairSer, logger, nameof(airport.PwrPlantRepairSer), nameof(Airport), id, RepairServiceMap),
            BottledOxygenType = EnumParseHelper.Parse(airport.BottledOxyType, logger, nameof(airport.BottledOxyType), nameof(Airport), id, OxygenTypeMap),
            BulkOxygenType = EnumParseHelper.Parse(airport.BulkOxyType, logger, nameof(airport.BulkOxyType), nameof(Airport), id, OxygenTypeMap),
            OtherServices = airport.OtherServices,

            // Transient Storage
            TransientStorageBuoys = ParseBool(airport.TrnsStrgBuoyFlag),
            TransientStorageHangars = ParseBool(airport.TrnsStrgHgrFlag),
            TransientStorageTiedowns = ParseBool(airport.TrnsStrgTieFlag),

            // Lighting & Visual Aids
            LgtSked = airport.LgtSked,
            BcnLgtSked = airport.BcnLgtSked,
            BeaconLensColor = EnumParseHelper.Parse(airport.BcnLensColor, logger, nameof(airport.BcnLensColor), nameof(Airport), id, BeaconLensColorMap),
            SegmentedCircleMarker = EnumParseHelper.Parse(airport.SegCircleMkrFlag, logger, nameof(airport.SegCircleMkrFlag), nameof(Airport), id, SegmentedCircleMap),
            WindIndicator = EnumParseHelper.Parse(airport.WindIndcrFlag, logger, nameof(airport.WindIndcrFlag), nameof(Airport), id, WindIndicatorMap),

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
}
