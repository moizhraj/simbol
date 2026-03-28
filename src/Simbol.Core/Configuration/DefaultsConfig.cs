namespace Simbol.Core.Configuration;

using Simbol.Core.Enums;

public class DefaultsConfig
{
    public int VendorId { get; set; } = 999;
    public string VendorName { get; set; } = "Simbol Simulator";
    public int SimulationIntervalMs { get; set; } = 1000;
    public int UpdateIntervalMs { get; set; } = 5000;
    public ValueRangeConfig ValueRange { get; set; } = new();
    public SimulationPattern SimulationPattern { get; set; } = SimulationPattern.Sine;

    public void Validate()
    {
        if (SimulationIntervalMs < 100)
            throw new InvalidOperationException($"SimulationIntervalMs must be >= 100, got {SimulationIntervalMs}.");

        if (UpdateIntervalMs < 100)
            throw new InvalidOperationException($"UpdateIntervalMs must be >= 100, got {UpdateIntervalMs}.");

        ValueRange.Validate();
    }
}
