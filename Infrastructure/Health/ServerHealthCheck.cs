namespace LinuxServerDataminerPOC.Infrastructure.Health;

using LinuxServerDataminerPOC.Domain.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public class ServerHealthCheck : IHealthCheck
{
    private readonly ILinuxMetricsCollector _metricsCollector;

    public ServerHealthCheck(ILinuxMetricsCollector metricsCollector)
    {
        _metricsCollector = metricsCollector;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        var metrics = await _metricsCollector.CollectMetricsAsync(cancellationToken);

        if (metrics.CpuUsagePercentage > 95.0)
        {
            return HealthCheckResult.Unhealthy($"Kritično: CPU usage {metrics.CpuUsagePercentage}%");
        }

        if (metrics.CpuUsagePercentage > 80.0)
        {
            return HealthCheckResult.Degraded($"Upozorenje: CPU usage {metrics.CpuUsagePercentage}%");
        }

        return HealthCheckResult.Healthy("System is healthy.");
    }
}
