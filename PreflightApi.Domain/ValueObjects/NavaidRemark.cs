namespace PreflightApi.Domain.ValueObjects
{
    public class NavaidRemark
    {
        /// <summary>NASR table associated with remark.</summary>
        public string TabName { get; set; } = string.Empty;

        /// <summary>NASR column name associated with remark.</summary>
        public string ReferenceColumnName { get; set; } = string.Empty;

        /// <summary>Sequence number assigned to reference column remark.</summary>
        public int SequenceNumber { get; set; }

        /// <summary>Remark text (free form text that further describes a specific information item).</summary>
        public string Remark { get; set; } = string.Empty;
    }
}
