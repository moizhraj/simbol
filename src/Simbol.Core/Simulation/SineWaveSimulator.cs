namespace Simbol.Core.Simulation;

public class SineWaveSimulator : IValueSimulator
{
    private readonly double _min;
    private readonly double _max;
    private readonly double _periodSeconds;
    private readonly double _phaseOffset;

    public SineWaveSimulator(double min = 0.0, double max = 100.0, double periodSeconds = 60.0, double phaseOffset = 0.0)
    {
        _min = min;
        _max = max;
        _periodSeconds = periodSeconds;
        _phaseOffset = phaseOffset;
    }

    public double GetAnalogValue(double elapsedSeconds)
    {
        var amplitude = (_max - _min) / 2.0;
        var center = _min + amplitude;
        return center + amplitude * Math.Sin(2.0 * Math.PI * (elapsedSeconds + _phaseOffset) / _periodSeconds);
    }

    public bool GetBinaryValue(double elapsedSeconds) =>
        Math.Sin(2.0 * Math.PI * (elapsedSeconds + _phaseOffset) / _periodSeconds) >= 0;

    public uint GetMultiStateValue(double elapsedSeconds, uint numberOfStates)
    {
        var normalized = (Math.Sin(2.0 * Math.PI * (elapsedSeconds + _phaseOffset) / _periodSeconds) + 1.0) / 2.0;
        return (uint)Math.Clamp(Math.Floor(normalized * numberOfStates) + 1, 1, numberOfStates);
    }
}
