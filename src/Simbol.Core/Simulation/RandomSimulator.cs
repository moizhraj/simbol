namespace Simbol.Core.Simulation;

public class RandomSimulator : IValueSimulator
{
    private readonly double _min;
    private readonly double _max;
    private readonly Random _random;

    public RandomSimulator(double min = 0.0, double max = 100.0, int? seed = null)
    {
        _min = min;
        _max = max;
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public double GetAnalogValue(double elapsedSeconds) =>
        _min + _random.NextDouble() * (_max - _min);

    public bool GetBinaryValue(double elapsedSeconds) =>
        _random.NextDouble() >= 0.5;

    public uint GetMultiStateValue(double elapsedSeconds, uint numberOfStates) =>
        (uint)(_random.Next((int)numberOfStates) + 1);
}
