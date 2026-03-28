namespace Simbol.Core.Configuration;

public class ValueRangeConfig
{
    public double Min { get; set; } = 0.0;
    public double Max { get; set; } = 100.0;

    public void Validate()
    {
        if (Min >= Max)
            throw new InvalidOperationException($"ValueRange.Min ({Min}) must be less than Max ({Max}).");
    }
}
