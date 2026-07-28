namespace LinuxServerDataminerPOC.Application.Services;

using LinuxServerDataminerPOC.Application.Dtos;
using LinuxServerDataminerPOC.Application.Interfaces;
using LinuxServerDataminerPOC.Application.Options;
using LinuxServerDataminerPOC.Domain.Interfaces;
using Microsoft.Extensions.Options;

public class MetricsService : IMetricsService
{
    private readonly ILinuxMetricsCollector _metricsCollector;
    private readonly MetricsOptions _options;

    public MetricsService(
        ILinuxMetricsCollector metricsCollector,
        IOptions<MetricsOptions> options)
    {
        _metricsCollector = metricsCollector;
        _options = options.Value;
    }

    public async Task<ServerMetricsDto> GetCurrentMetricsAsync(CancellationToken cancellationToken = default)
    {
        var rawMetrics = await _metricsCollector.CollectMetricsAsync(cancellationToken);

        double memPercentage = rawMetrics.TotalMemoryMb > 0 
            ? Math.Round((double)rawMetrics.UsedMemoryMb / rawMetrics.TotalMemoryMb * 100, 2)
            : 0;

        string status = rawMetrics.CpuUsagePercentage >= _options.CpuThresholdWarningPercentage 
            ? "WARNING" 
            : "OK";

        return new ServerMetricsDto(
            rawMetrics.CpuUsagePercentage,
            rawMetrics.TotalMemoryMb,
            rawMetrics.UsedMemoryMb,
            memPercentage,
            rawMetrics.DiskUsagePercentage,
            status,
            rawMetrics.TimestampUtc
        );
    }
}
