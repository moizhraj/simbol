namespace Simbol.Engine.Services;

using Simbol.Engine.Models;

public class NullConsoleDisplay : IConsoleDisplay
{
    public void UpdateDashboard(IReadOnlyDictionary<uint, SimulatedDevice> devices, TimeSpan uptime, int statsIntervalSeconds) { }
    public void AddActivity(string message) { }
    public void SetLogFilePath(string path) { }
    public void SetConfigPath(string path) { }
    public Task RunAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
