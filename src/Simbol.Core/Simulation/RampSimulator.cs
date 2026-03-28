namespace Simbol.Core.Simulation;

public class RampSimulator : IValueSimulator
{
    private readonly double _min;
    private readonly double _max;
    private readonly double _periodSeconds;

    public RampSimulator(double min = 0.0, double max = 100.0, double periodSeconds = 60.0)
    {
        _min = min;
        _max = max;
        _periodSeconds = periodSeconds;
    }

    public double GetAnalogValue(double elapsedSeconds)
    {
        var fraction = (elapsedSeconds % _periodSeconds) / _periodSeconds;
        // Ramp up then ramp down (triangle wave)
        var triangleValue = fraction <= 0.5 ? fraction * 2.0 : (1.0 - fraction) * 2.0;
        return _min + (_max - _min) * triangleValue;
    }

    public bool GetBinaryValue(double elapsedSeconds)
    {
        var fraction = (elapsedSeconds % _periodSeconds) / _periodSeconds;
        return fraction < 0.5;
    }

    public uint GetMultiStateValue(double elapsedSeconds, uint numberOfStates)
    {
        var fraction = (elapsedSeconds % _periodSeconds) / _periodSeconds;
        var triangleValue = fraction <= 0.5 ? fraction * 2.0 : (1.0 - fraction) * 2.0;
        return (uint)Math.Clamp(Math.Floor(triangleValue * numberOfStates) + 1, 1, numberOfStates);
    }
}
