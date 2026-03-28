namespace Simbol.Engine.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Simbol.Core.Configuration;

public class StatsReporterService : BackgroundService
{
    private readonly BacnetServiceHandler _serviceHandler;
    private readonly SimbolConfig _config;
    private readonly IConsoleDisplay _display;
    private readonly ILogger<StatsReporterService> _logger;
    private readonly DateTime _startTime = DateTime.UtcNow;

    public StatsReporterService(
        BacnetServiceHandler serviceHandler,
        SimbolConfig config,
        IConsoleDisplay display,
        ILogger<StatsReporterService> logger)
    {
        _serviceHandler = serviceHandler;
        _config = config;
        _display = display;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Stats reporter started (interval: {Interval}s)", _config.Defaults.StatsIntervalSeconds);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_config.Defaults.StatsIntervalSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var uptime = DateTime.UtcNow - _startTime;

                // Compute rates for all devices (resets window counters)
                foreach (var device in _serviceHandler.Devices.Values)
                {
                    device.Stats.ResetWindow(_config.Defaults.StatsIntervalSeconds);
                }

                _display.UpdateDashboard(
                    _serviceHandler.Devices,
                    uptime,
                    _config.Defaults.StatsIntervalSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating dashboard");
            }
        }
    }
}
