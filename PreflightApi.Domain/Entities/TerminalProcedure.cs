using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PreflightApi.Domain.Entities
{
    public class TerminalProcedure
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string AirportName { get; set; } = string.Empty;

        [MaxLength(4)]
        public string? IcaoIdent { get; set; }

        [MaxLength(4)]
        public string? AirportIdent { get; set; }

        [Required]
        [MaxLength(10)]
        public string ChartCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string ChartName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string PdfFileName { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? AmendmentNumber { get; set; }

        [MaxLength(20)]
        public string? AmendmentDate { get; set; }
    }
}
