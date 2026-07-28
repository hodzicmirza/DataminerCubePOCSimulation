namespace LinuxServerDataminerPOC.Domain.Interfaces;

using LinuxServerDataminerPOC.Domain.Entities;

public interface ILinuxMetricsCollector
{
    Task<ServerMetrics> CollectMetricsAsync(CancellationToken cancellationToken = default);
}
