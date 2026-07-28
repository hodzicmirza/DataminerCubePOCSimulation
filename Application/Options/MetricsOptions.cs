namespace LinuxServerDataminerPOC.Application.Options;

public class MetricsOptions
{
    public const string SectionName = "MetricsOptions";

    public int RefreshIntervalSeconds { get; set; } = 5;
    public bool EnableSimulationMode { get; set; } = false;
    public double CpuThresholdWarningPercentage { get; set; } = 80.0;
}
