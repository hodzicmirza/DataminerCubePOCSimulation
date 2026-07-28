namespace LinuxServerDataminerPOC.Application.Dtos;

public record ServerMetricsDto
{
    public double CpuUsagePercentage { get; init; }
    public long TotalMemoryMb { get; init; }
    public long UsedMemoryMb { get; init; }
    public double MemoryUsagePercentage { get; init; }
    public double DiskUsagePercentage { get; init; }
    public string SystemStatus { get; init; } = "OK";
    public DateTime TimestampUtc { get; init; }

    public ServerMetricsDto() { }

    public ServerMetricsDto(
        double cpuUsagePercentage,
        long totalMemoryMb,
        long usedMemoryMb,
        double memoryUsagePercentage,
        double diskUsagePercentage,
        string systemStatus,
        DateTime timestampUtc)
    {
        CpuUsagePercentage = cpuUsagePercentage;
        TotalMemoryMb = totalMemoryMb;
        UsedMemoryMb = usedMemoryMb;
        MemoryUsagePercentage = memoryUsagePercentage;
        DiskUsagePercentage = diskUsagePercentage;
        SystemStatus = systemStatus;
        TimestampUtc = timestampUtc;
    }
}
