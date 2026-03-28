namespace Simbol.Core.Simulation;

using Simbol.Core.Configuration;
using Simbol.Core.Enums;

public static class ValueSimulatorFactory
{
    public static IValueSimulator Create(SimulationPattern pattern, ValueRangeConfig? range = null, double? defaultValue = null)
    {
        var min = range?.Min ?? 0.0;
        var max = range?.Max ?? 100.0;

        return pattern switch
        {
            SimulationPattern.Static => new StaticValueSimulator(defaultValue ?? (min + max) / 2.0),
            SimulationPattern.Sine => new SineWaveSimulator(min, max),
            SimulationPattern.Ramp => new RampSimulator(min, max),
            SimulationPattern.Random => new RandomSimulator(min, max),
            SimulationPattern.Sawtooth => new SawtoothSimulator(min, max),
            _ => throw new ArgumentOutOfRangeException(nameof(pattern), pattern, "Unknown simulation pattern.")
        };
    }
}
