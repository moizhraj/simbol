namespace Simbol.Engine.Services;

using Simbol.Engine.Models;

public interface IConsoleDisplay
{
    void UpdateDashboard(IReadOnlyDictionary<uint, SimulatedDevice> devices, TimeSpan uptime, int statsIntervalSeconds);
    void AddActivity(string message);
    void SetLogFilePath(string path);
    void SetConfigPath(string path);
    Task RunAsync(CancellationToken cancellationToken);
}
