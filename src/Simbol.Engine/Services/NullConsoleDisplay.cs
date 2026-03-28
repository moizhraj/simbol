namespace Simbol.Engine.Services;

using Simbol.Engine.Models;

public class NullConsoleDisplay : IConsoleDisplay
{
    public void Initialize(IReadOnlyDictionary<uint, SimulatedDevice> devices) { }
    public void AddActivity(string message) { }
    public void SetLogFilePath(string path) { }
    public void SetConfigPath(string path) { }
    public Task RunAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
