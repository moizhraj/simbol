namespace Simbol.Core.Configuration;

using Simbol.Core.Enums;

public class ObjectGroupConfig
{
    public int Count { get; set; } = 0;
    public SimulationPattern? SimulationPattern { get; set; }
    public ValueRangeConfig? ValueRange { get; set; }
    public double? DefaultValue { get; set; }
    public int NumberOfStates { get; set; } = 3;
    public int? UpdateIntervalMs { get; set; }

    public void Validate(string objectType)
    {
        if (Count < 0)
            throw new InvalidOperationException($"Object group '{objectType}' count cannot be negative.");

        if (NumberOfStates < 2)
            throw new InvalidOperationException($"Object group '{objectType}' numberOfStates must be >= 2.");

        if (UpdateIntervalMs.HasValue && UpdateIntervalMs.Value < 100)
            throw new InvalidOperationException($"Object group '{objectType}' updateIntervalMs must be >= 100.");

        ValueRange?.Validate();
    }
}
