namespace Simbol.Engine.Services;

using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Simbol.Core.Configuration;

public class StatsReporterService : BackgroundService
{
    private readonly BacnetServiceHandler _serviceHandler;
    private readonly SimbolConfig _config;
    private readonly ILogger<StatsReporterService> _logger;

    public StatsReporterService(
        BacnetServiceHandler serviceHandler,
        SimbolConfig config,
        ILogger<StatsReporterService> logger)
    {
        _serviceHandler = serviceHandler;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = _config.Defaults.StatsIntervalSeconds;
        _logger.LogInformation("Stats reporter started (interval: {Interval}s)", intervalSeconds);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                ReportStats(intervalSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reporting stats");
            }
        }
    }

    private void ReportStats(int windowSeconds)
    {
        var devices = _serviceHandler.Devices;
        if (devices.Count == 0) return;

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┐");
        sb.AppendLine("│                                        DEVICE LOAD STATISTICS                                              │");
        sb.AppendLine("├──────────────┬──────────┬──────┬───────┬───────┬───────┬────────┬────────┬─────────┬──────────┬─────────────┤");
        sb.AppendLine("│ Device       │ Total    │ RP   │ RPM   │ WP    │ COVSb │ WhoIs  │ COVNot │ Errors  │ Clients  │ Req/min     │");
        sb.AppendLine("├──────────────┼──────────┼──────┼───────┼───────┼───────┼────────┼────────┼─────────┼──────────┼─────────────┤");

        foreach (var device in devices.Values)
        {
            var stats = device.Stats;
            var rate = stats.ResetWindow(windowSeconds);
            var peak = stats.PeakRequestsPerMinute;

            var name = device.Name.Length > 12 ? device.Name[..12] : device.Name;

            sb.AppendLine(string.Format(
                "│ {0,-12} │ {1,8} │ {2,4} │ {3,5} │ {4,5} │ {5,5} │ {6,6} │ {7,6} │ {8,7} │ {9,8} │ {10,5:F1}/{11,5:F1} │",
                name,
                stats.TotalRequestCount,
                stats.ReadPropertyCount,
                stats.ReadPropertyMultipleCount,
                stats.WritePropertyCount,
                stats.SubscribeCovCount,
                stats.WhoIsCount,
                stats.CovNotificationsSentCount,
                stats.ErrorCount,
                stats.UniqueClientCount,
                rate,
                peak));
        }

        sb.AppendLine("└──────────────┴──────────┴──────┴───────┴───────┴───────┴────────┴────────┴─────────┴──────────┴─────────────┘");
        sb.AppendLine("  RP=ReadProperty  RPM=ReadPropertyMultiple  WP=WriteProperty  COVSb=SubscribeCOV  COVNot=COV Notifications");
        sb.AppendLine("  Req/min: current/peak");

        _logger.LogInformation("{Stats}", sb.ToString());
    }
}
