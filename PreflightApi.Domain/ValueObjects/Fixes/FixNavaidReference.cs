namespace PreflightApi.Domain.ValueObjects.Fixes;

public class FixNavaidReference
{
    public string? NavId { get; set; }
    public string? NavType { get; set; }
    public string? Bearing { get; set; }
    public string? Distance { get; set; }
}
