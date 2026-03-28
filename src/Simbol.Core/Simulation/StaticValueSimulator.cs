namespace Simbol.Core.Simulation;

public class StaticValueSimulator : IValueSimulator
{
    private readonly double _value;

    public StaticValueSimulator(double value = 0.0)
    {
        _value = value;
    }

    public double GetAnalogValue(double elapsedSeconds) => _value;

    public bool GetBinaryValue(double elapsedSeconds) => _value != 0.0;

    public uint GetMultiStateValue(double elapsedSeconds, uint numberOfStates) =>
        (uint)Math.Clamp(_value, 1, numberOfStates);
}
