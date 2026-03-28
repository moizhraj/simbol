namespace Simbol.Engine.Services;

using Simbol.Engine.Models;

public interface IConsoleDisplay
{
    void Initialize(IReadOnlyDictionary<uint, SimulatedDevice> devices);
    void AddActivity(string message);
    void SetLogFilePath(string path);
    void SetConfigPath(string path);
    Task RunAsync(CancellationToken cancellationToken);
}
