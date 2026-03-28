namespace Simbol.Core.Simulation;

public class SawtoothSimulator : IValueSimulator
{
    private readonly double _min;
    private readonly double _max;
    private readonly double _periodSeconds;

    public SawtoothSimulator(double min = 0.0, double max = 100.0, double periodSeconds = 60.0)
    {
        _min = min;
        _max = max;
        _periodSeconds = periodSeconds;
    }

    public double GetAnalogValue(double elapsedSeconds)
    {
        var fraction = (elapsedSeconds % _periodSeconds) / _periodSeconds;
        return _min + (_max - _min) * fraction;
    }

    public bool GetBinaryValue(double elapsedSeconds)
    {
        var fraction = (elapsedSeconds % _periodSeconds) / _periodSeconds;
        return fraction >= 0.5;
    }

    public uint GetMultiStateValue(double elapsedSeconds, uint numberOfStates)
    {
        var fraction = (elapsedSeconds % _periodSeconds) / _periodSeconds;
        return (uint)Math.Clamp(Math.Floor(fraction * numberOfStates) + 1, 1, numberOfStates);
    }
}
