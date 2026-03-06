using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PreflightApi.Domain.Entities
{
    /// <summary>
    /// Communication frequency data from the FAA NASR database. Sourced from FRQ CSV file in the FAA NASR 28-day subscription.
    /// Contains radio frequencies for ATC facilities, towers, approach/departure control, and other aviation communication services.
    /// </summary>
    [Table("communication_frequencies")]
    public class CommunicationFrequency : INasrEntity<CommunicationFrequency>
    {
        /// <summary>System-generated unique identifier.</summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        /// <summary>FAA NASR field: FACILITY. Contains FACILITY ID except for FACILITY TYPE AFIS, CTAF, GCO, UNICOM and RCAG. The FACILITY NAME is used for RCAG sites. AFIS, CTAF, GCO and UNICOM are NULL.</summary>
        [Column("facility_code", TypeName = "varchar(30)")]
        public string? FacilityCode { get; set; }

        /// <summary>FAA NASR field: EFF_DATE. The 28 Day NASR Subscription Effective Date. ISO 8601 UTC format.</summary>
        [Column("effective_date")]
        public DateTime EffectiveDate { get; set; }

        /// <summary>FAA NASR field: FAC_NAME. Official Facility Name. NULL for AFIS, CTAF, GCO, UNICOM (no FACILITY ID or NAME in NASR) and ASOS/AWOS (no FACILITY NAME in NASR).</summary>
        [Column("facility_name", TypeName = "varchar(50)")]
        public string? FacilityName { get; set; }

        /// <summary>FAA NASR field: FACILITY_TYPE. All records contain a FACILITY TYPE. Note: RCO and RCO1 serve the same function (remote communication outlet). An RCO1 may exist if two separate sites share the same identifier.</summary>
        [Column("facility_type", TypeName = "varchar(12)")]
        [Required]
        public string FacilityType { get; set; } = string.Empty;

        /// <summary>FAA NASR field: ARTCC_OR_FSS_ID. RCAG facilities contain an ARTCC ID; RCO/RCO1 facilities contain an FSS ID. Included for convenience to identify the parent ARTCC or FSS resource in NASR.</summary>
        [Column("artcc_or_fss_id", TypeName = "varchar(4)")]
        public string? ArtccOrFssId { get; set; }

        /// <summary>FAA NASR field: CPDLC. A Controller Pilot Data Link Communications (CPDLC) remark associated with a FACILITY.</summary>
        [Column("cpdlc", TypeName = "varchar(100)")]
        public string? Cpdlc { get; set; }

        /// <summary>FAA NASR field: TOWER_HRS. Tower operating hours. Only listed for ATCT FACILITY TYPEs where the FACILITY equals the SERVICED FACILITY.</summary>
        [Column("tower_hours", TypeName = "varchar(200)")]
        public string? TowerHours { get; set; }

        /// <summary>FAA NASR field: SERVICED_FACILITY. The FACILITY ID (or FACILITY NAME if FACILITY TYPE is RCAG) that is serviced by the frequencies listed. This is a NON-NULL field.</summary>
        [Column("serviced_facility", TypeName = "varchar(30)")]
        [Required]
        public string ServicedFacility { get; set; } = string.Empty;

        /// <summary>FAA NASR field: SERVICED_FAC_NAME. The FACILITY NAME that is serviced by the frequencies listed.</summary>
        [Column("serviced_facility_name", TypeName = "varchar(50)")]
        public string? ServicedFacilityName { get; set; }

        /// <summary>FAA NASR field: SERVICED_SITE_TYPE. Facility Type of SERVICED FACILITY.</summary>
        [Column("serviced_site_type", TypeName = "varchar(25)")]
        public string? ServicedSiteType { get; set; }

        /// <summary>FAA NASR field: LAT_DECIMAL. Facility Reference Point Latitude in decimal degrees (WGS 84).</summary>
        [Column("latitude", TypeName = "decimal(10,8)")]
        public decimal? Latitude { get; set; }

        /// <summary>FAA NASR field: LONG_DECIMAL. Facility Reference Point Longitude in decimal degrees (WGS 84).</summary>
        [Column("longitude", TypeName = "decimal(11,8)")]
        public decimal? Longitude { get; set; }

        /// <summary>FAA NASR field: SERVICED_CITY. Serviced Facility Associated City Name.</summary>
        [Column("serviced_city", TypeName = "varchar(40)")]
        public string? ServicedCity { get; set; }

        /// <summary>FAA NASR field: SERVICED_STATE. Two-letter state ID of the SERVICED FACILITY.</summary>
        [Column("serviced_state", TypeName = "varchar(2)")]
        public string? ServicedState { get; set; }

        /// <summary>FAA NASR field: SERVICED_COUNTRY. Country Post Office Code of Serviced Facility.</summary>
        [Column("serviced_country", TypeName = "varchar(2)")]
        public string? ServicedCountry { get; set; }

        /// <summary>FAA NASR field: TOWER_OR_COMM_CALL. Radio call used by pilot to contact ATC or FSS facility.</summary>
        [Column("tower_or_comm_call", TypeName = "varchar(30)")]
        public string? TowerOrCommCall { get; set; }

        /// <summary>FAA NASR field: PRIMARY_APPROACH_RADIO_CALL. Radio call of facility that furnishes primary approach control.</summary>
        [Column("primary_approach_radio_call", TypeName = "varchar(26)")]
        public string? PrimaryApproachRadioCall { get; set; }

        /// <summary>FAA NASR field: FREQ. Frequency for SERVICED FACILITY use in MHz. In the case of a NAVAID with DME/TACAN Channel, the Frequency is displayed with the Channel (FREQ/CHAN).</summary>
        [Column("frequency", TypeName = "varchar(40)")]
        public string? Frequency { get; set; }

        /// <summary>FAA NASR field: SECTORIZATION. Sectorization based on SERVICED FACILITY or airway boundaries, or limitations based on runway usage. For ARTCC and RCAG, identifies the Frequency Altitude as Low, High, Low/High or Ultra-High.</summary>
        [Column("sectorization", TypeName = "varchar(50)")]
        public string? Sectorization { get; set; }

        /// <summary>FAA NASR field: FREQ_USE. SERVICED FACILITY frequency use description.</summary>
        [Column("frequency_use", TypeName = "varchar(600)")]
        public string? FrequencyUse { get; set; }

        /// <summary>FAA NASR field: REMARK. Remark Text (Free Form Text that further describes a specific Information Item).</summary>
        [Column("remark", TypeName = "varchar(1500)")]
        public string? Remark { get; set; }

        // INasrEntity<CommunicationFrequency> implementation
        public string CreateUniqueKey()
        {
            return string.Join("|", new[]
            {
                FacilityCode ?? string.Empty,
                ServicedFacility ?? string.Empty,
                ServicedSiteType ?? string.Empty,
                ServicedState ?? string.Empty,
                Frequency ?? string.Empty,
                FrequencyUse ?? string.Empty,
                Sectorization ?? string.Empty
            });
        }

        public void UpdateFrom(CommunicationFrequency source, HashSet<string>? limitToProperties = null)
        {
            // For standalone entities like CommunicationFrequency, we typically update all properties
            // since there's no supplementary data concept
            if (limitToProperties == null || !limitToProperties.Any())
            {
                UpdateAllProperties(source);
            }
            else
            {
                UpdateSelectiveProperties(source, limitToProperties);
            }
        }

        public CommunicationFrequency CreateSelectiveEntity(HashSet<string> properties)
        {
            var selective = new CommunicationFrequency();

            // Key properties
            if (properties.Contains(nameof(FacilityCode)))
                selective.FacilityCode = FacilityCode;
            if (properties.Contains(nameof(ServicedFacility)))
                selective.ServicedFacility = ServicedFacility;
            if (properties.Contains(nameof(ServicedSiteType)))
                selective.ServicedSiteType = ServicedSiteType;
            if (properties.Contains(nameof(ServicedState)))
                selective.ServicedState = ServicedState;
            if (properties.Contains(nameof(Frequency)))
                selective.Frequency = Frequency;
            if (properties.Contains(nameof(FrequencyUse)))
                selective.FrequencyUse = FrequencyUse;
            if (properties.Contains(nameof(Sectorization)))
                selective.Sectorization = Sectorization;

            // Other properties
            if (properties.Contains(nameof(EffectiveDate)))
                selective.EffectiveDate = EffectiveDate;
            if (properties.Contains(nameof(FacilityName)))
                selective.FacilityName = FacilityName;
            if (properties.Contains(nameof(FacilityType)))
                selective.FacilityType = FacilityType;
            if (properties.Contains(nameof(ArtccOrFssId)))
                selective.ArtccOrFssId = ArtccOrFssId;
            if (properties.Contains(nameof(Cpdlc)))
                selective.Cpdlc = Cpdlc;
            if (properties.Contains(nameof(TowerHours)))
                selective.TowerHours = TowerHours;
            if (properties.Contains(nameof(ServicedFacilityName)))
                selective.ServicedFacilityName = ServicedFacilityName;
            if (properties.Contains(nameof(Latitude)))
                selective.Latitude = Latitude;
            if (properties.Contains(nameof(Longitude)))
                selective.Longitude = Longitude;
            if (properties.Contains(nameof(ServicedCity)))
                selective.ServicedCity = ServicedCity;
            if (properties.Contains(nameof(ServicedCountry)))
                selective.ServicedCountry = ServicedCountry;
            if (properties.Contains(nameof(TowerOrCommCall)))
                selective.TowerOrCommCall = TowerOrCommCall;
            if (properties.Contains(nameof(PrimaryApproachRadioCall)))
                selective.PrimaryApproachRadioCall = PrimaryApproachRadioCall;
            if (properties.Contains(nameof(Remark)))
                selective.Remark = Remark;

            return selective;
        }

        private void UpdateAllProperties(CommunicationFrequency source)
        {
            EffectiveDate = source.EffectiveDate;
            FacilityName = source.FacilityName;
            FacilityType = source.FacilityType;
            ArtccOrFssId = source.ArtccOrFssId;
            Cpdlc = source.Cpdlc;
            TowerHours = source.TowerHours;
            ServicedFacilityName = source.ServicedFacilityName;
            Latitude = source.Latitude;
            Longitude = source.Longitude;
            ServicedCity = source.ServicedCity;
            ServicedCountry = source.ServicedCountry;
            TowerOrCommCall = source.TowerOrCommCall;
            PrimaryApproachRadioCall = source.PrimaryApproachRadioCall;
            Remark = source.Remark;
        }

        private void UpdateSelectiveProperties(CommunicationFrequency source, HashSet<string> limitToProperties)
        {
            // Update only specified properties with non-null values
            if (limitToProperties.Contains(nameof(EffectiveDate)))
                EffectiveDate = source.EffectiveDate;
            if (limitToProperties.Contains(nameof(FacilityName)) && source.FacilityName != null)
                FacilityName = source.FacilityName;
            if (limitToProperties.Contains(nameof(FacilityType)) && source.FacilityType != null)
                FacilityType = source.FacilityType;
            if (limitToProperties.Contains(nameof(ArtccOrFssId)) && source.ArtccOrFssId != null)
                ArtccOrFssId = source.ArtccOrFssId;
            if (limitToProperties.Contains(nameof(Cpdlc)) && source.Cpdlc != null)
                Cpdlc = source.Cpdlc;
            if (limitToProperties.Contains(nameof(TowerHours)) && source.TowerHours != null)
                TowerHours = source.TowerHours;
            if (limitToProperties.Contains(nameof(ServicedFacilityName)) && source.ServicedFacilityName != null)
                ServicedFacilityName = source.ServicedFacilityName;
            if (limitToProperties.Contains(nameof(Latitude)) && source.Latitude != null)
                Latitude = source.Latitude;
            if (limitToProperties.Contains(nameof(Longitude)) && source.Longitude != null)
                Longitude = source.Longitude;
            if (limitToProperties.Contains(nameof(ServicedCity)) && source.ServicedCity != null)
                ServicedCity = source.ServicedCity;
            if (limitToProperties.Contains(nameof(ServicedCountry)) && source.ServicedCountry != null)
                ServicedCountry = source.ServicedCountry;
            if (limitToProperties.Contains(nameof(TowerOrCommCall)) && source.TowerOrCommCall != null)
                TowerOrCommCall = source.TowerOrCommCall;
            if (limitToProperties.Contains(nameof(PrimaryApproachRadioCall)) && source.PrimaryApproachRadioCall != null)
                PrimaryApproachRadioCall = source.PrimaryApproachRadioCall;
            if (limitToProperties.Contains(nameof(Remark)) && source.Remark != null)
                Remark = source.Remark;
        }
    }
}
