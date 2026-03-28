namespace Simbol.Engine.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Simbol.Core.Configuration;

/// <summary>
/// Retained for backward compatibility. Rate computation is now driven by the
/// 1-second display refresh cycle in SpectreConsoleDisplay.
/// </summary>
public class StatsReporterService : BackgroundService
{
    private readonly ILogger<StatsReporterService> _logger;

    public StatsReporterService(ILogger<StatsReporterService> logger)
    {
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}
