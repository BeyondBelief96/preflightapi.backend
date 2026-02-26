namespace PreflightApi.Infrastructure.HealthChecks;

public class HealthMonitorSettings
{
    public int IntervalSeconds { get; set; } = 30;
    public int FailureThreshold { get; set; } = 3;
    public int RecoveryThreshold { get; set; } = 2;
}
