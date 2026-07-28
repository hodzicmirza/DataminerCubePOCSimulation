namespace LinuxServerDataminerPOC.Application.Interfaces;

using LinuxServerDataminerPOC.Application.Dtos;

public interface IMetricsService
{
    Task<ServerMetricsDto> GetCurrentMetricsAsync(CancellationToken cancellationToken = default);
}
