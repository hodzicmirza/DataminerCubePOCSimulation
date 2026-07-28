namespace LinuxServerDataminerPOC.Domain.Entities;

public record ServerMetrics(
    double CpuUsagePercentage,
    long TotalMemoryMb,
    long UsedMemoryMb,
    double DiskUsagePercentage,
    TimeSpan Uptime,
    DateTime TimestampUtc
);
