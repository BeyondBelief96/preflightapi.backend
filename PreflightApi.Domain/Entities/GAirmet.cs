using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;
using PreflightApi.Domain.ValueObjects.GAirmets;

namespace PreflightApi.Domain.Entities
{
    /// <summary>
    /// G-AIRMET (Graphical AIRMET) advisory data from the NOAA Aviation Weather Center.
    /// Sourced from the AvWx cache XML feed (gairmet1_0.xsd schema).
    /// </summary>
    [Table("gairmet")]
    public class GAirmet
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Time the G-AIRMET was received. UTC.
        /// </summary>
        [Column("receipt_time")]
        public DateTime ReceiptTime { get; set; }

        /// <summary>
        /// Time the G-AIRMET was issued. UTC.
        /// </summary>
        [Column("issue_time")]
        public DateTime IssueTime { get; set; }

        /// <summary>
        /// Time the G-AIRMET expires, typically 6 hours after issuance. UTC.
        /// </summary>
        [Column("expire_time")]
        public DateTime ExpireTime { get; set; }

        /// <summary>
        /// The valid time of the G-AIRMET snapshot. UTC.
        /// </summary>
        [Column("valid_time")]
        public DateTime ValidTime { get; set; }

        /// <summary>
        /// Product type: SIERRA, TANGO, or ZULU
        /// </summary>
        [Column("product")]
        public string Product { get; set; } = string.Empty;

        /// <summary>
        /// Forecast component identifier tag. ex: 1C
        /// </summary>
        [Column("tag")]
        public string? Tag { get; set; }

        /// <summary>
        /// The forecast hour taken from initial product issuance. ex: 0, 3, 6, 9, 12
        /// </summary>
        [Column("forecast_hour")]
        public int ForecastHour { get; set; }

        /// <summary>
        /// Type of hazard: IFR, MT_OBSC (mountain obscuration), TURB-HI (high-level turbulence), TURB-LO (low-level turbulence), ICE (icing), FZLVL (freezing level), M_FZLVL (multiple freezing levels), SFC_WND (strong surface winds), or LLWS (low-level wind shear).
        /// </summary>
        [Column("hazard_type")]
        public string? HazardType { get; set; }

        /// <summary>
        /// Severity of the hazard: MOD (moderate) or null.
        /// </summary>
        [Column("hazard_severity")]
        public string? HazardSeverity { get; set; }

        /// <summary>
        /// The geometry type: AREA or LINE.
        /// </summary>
        [Column("geometry_type")]
        public string? GeometryType { get; set; }

        /// <summary>
        /// Additional information, reason for the AIRMET. ex: CIG BLW 010/VIS BLW 3SM PCPN/BR/FG
        /// </summary>
        [Column("due_to")]
        public string? DueTo { get; set; }

        /// <summary>
        /// Altitude ranges for the advisory in feet MSL. 0 indicates surface, -1 indicates freezing level.
        /// </summary>
        [Column("altitudes", TypeName = "jsonb")]
        public List<GAirmetAltitude>? Altitudes { get; set; }

        /// <summary>
        /// The geographic area defined by lat/lon points
        /// </summary>
        [Column("area", TypeName = "jsonb")]
        public GAirmetArea? Area { get; set; }

        /// <summary>
        /// PostGIS geometry computed from Area JSONB via database trigger.
        /// Polygon representing the geographic boundary of the affected area.
        /// </summary>
        [Column("boundary", TypeName = "geometry(Geometry, 4326)")]
        public Geometry? Boundary { get; set; }
    }
}
