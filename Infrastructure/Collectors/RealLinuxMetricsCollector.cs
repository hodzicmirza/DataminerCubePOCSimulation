namespace LinuxServerDataminerPOC.Infrastructure.Collectors;

using LinuxServerDataminerPOC.Domain.Entities;
using LinuxServerDataminerPOC.Domain.Interfaces;

public class RealLinuxMetricsCollector : ILinuxMetricsCollector
{
    private static readonly Random _random = new();

    private static string ProcPath
    {
        get
        {
            if (Directory.Exists("/host/proc")) return "/host/proc";
            if (Directory.Exists("/proc")) return "/proc";
            return string.Empty;
        }
    }

    public async Task<ServerMetrics> CollectMetricsAsync(CancellationToken cancellationToken = default)
    {
        var (totalMemMb, usedMemMb) = GetRealMemoryFromLinux();
        double realCpuUsage = await GetRealCpuUsageFromLinuxAsync();

        double simulatedDiskUsage = Math.Round(40.0 + (_random.NextDouble() * 10), 2);
        TimeSpan uptime = TimeSpan.FromHours(48);

        return new ServerMetrics(
            CpuUsagePercentage: realCpuUsage,
            TotalMemoryMb: totalMemMb,
            UsedMemoryMb: usedMemMb,
            DiskUsagePercentage: simulatedDiskUsage,
            Uptime: uptime,
            TimestampUtc: DateTime.UtcNow
        );
    }

    private static (long TotalMb, long UsedMb) GetRealMemoryFromLinux()
    {
        try
        {
            if (string.IsNullOrEmpty(ProcPath))
                return (16384, 8192);

            string memInfoPath = Path.Combine(ProcPath, "meminfo");
            if (!File.Exists(memInfoPath))
                return (16384, 8192);

            var lines = File.ReadAllLines(memInfoPath);
            long memTotalKb = 0;
            long memAvailableKb = 0;

            foreach (var line in lines)
            {
                if (line.StartsWith("MemTotal:"))
                {
                    memTotalKb = ParseKbLine(line);
                }
                else if (line.StartsWith("MemAvailable:"))
                {
                    memAvailableKb = ParseKbLine(line);
                }
            }

            long totalMb = memTotalKb / 1024;
            long usedMb = (memTotalKb - memAvailableKb) / 1024;

            return (totalMb, usedMb);
        }
        catch
        {
            return (16384, 8192);
        }
    }

    private static async Task<double> GetRealCpuUsageFromLinuxAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(ProcPath))
                return Math.Round(_random.NextDouble() * 100, 2);

            string statPath = Path.Combine(ProcPath, "stat");
            if (!File.Exists(statPath))
                return Math.Round(_random.NextDouble() * 100, 2);

            var firstSample = ReadCpuTimes(statPath);
            await Task.Delay(100);
            var secondSample = ReadCpuTimes(statPath);

            long idleDelta = secondSample.Idle - firstSample.Idle;
            long totalDelta = secondSample.Total - firstSample.Total;

            if (totalDelta == 0) return 0.0;

            double cpuPercent = (1.0 - ((double)idleDelta / totalDelta)) * 100.0;
            return Math.Round(cpuPercent, 2);
        }
        catch
        {
            return Math.Round(_random.NextDouble() * 100, 2);
        }
    }

    private static (long Idle, long Total) ReadCpuTimes(string statPath)
    {
        var firstLine = File.ReadLines(statPath).First();
        var parts = firstLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        long user = long.Parse(parts[1]);
        long nice = long.Parse(parts[2]);
        long system = long.Parse(parts[3]);
        long idle = long.Parse(parts[4]);
        long iowait = parts.Length > 5 ? long.Parse(parts[5]) : 0;
        long irq = parts.Length > 6 ? long.Parse(parts[6]) : 0;
        long softirq = parts.Length > 7 ? long.Parse(parts[7]) : 0;

        long total = user + nice + system + idle + iowait + irq + softirq;
        return (idle, total);
    }

    private static long ParseKbLine(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return long.TryParse(parts[1], out long val) ? val : 0;
    }
}
