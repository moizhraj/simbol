namespace Simbol.Core.Simulation;

public interface IValueSimulator
{
    /// Returns an analog value (float) based on elapsed seconds since simulation start.
    double GetAnalogValue(double elapsedSeconds);

    /// Returns a binary value based on elapsed seconds.
    bool GetBinaryValue(double elapsedSeconds);

    /// Returns a multi-state value (1-based) based on elapsed seconds.
    uint GetMultiStateValue(double elapsedSeconds, uint numberOfStates);
}
