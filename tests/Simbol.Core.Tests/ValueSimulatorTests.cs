namespace Simbol.Core.Tests;

using Simbol.Core.Enums;
using Simbol.Core.Simulation;

public class ValueSimulatorTests
{
    [Fact]
    public void StaticSimulator_ReturnsConstantValue()
    {
        var sim = new StaticValueSimulator(42.0);

        Assert.Equal(42.0, sim.GetAnalogValue(0));
        Assert.Equal(42.0, sim.GetAnalogValue(100));
        Assert.Equal(42.0, sim.GetAnalogValue(9999));
    }

    [Fact]
    public void SineWaveSimulator_ReturnsValueInRange()
    {
        var sim = new SineWaveSimulator(min: 10, max: 90);

        for (double t = 0; t < 120; t += 0.5)
        {
            var value = sim.GetAnalogValue(t);
            Assert.InRange(value, 10.0, 90.0);
        }
    }

    [Fact]
    public void SineWaveSimulator_AtZero_ReturnsMidpoint()
    {
        var sim = new SineWaveSimulator(min: 0, max: 100);

        var value = sim.GetAnalogValue(0);

        Assert.Equal(50.0, value, precision: 5);
    }

    [Fact]
    public void RampSimulator_ReturnsValueInRange()
    {
        var sim = new RampSimulator(min: 20, max: 80);

        for (double t = 0; t < 120; t += 0.5)
        {
            var value = sim.GetAnalogValue(t);
            Assert.InRange(value, 20.0, 80.0);
        }
    }

    [Fact]
    public void RandomSimulator_ReturnsValueInRange()
    {
        var sim = new RandomSimulator(min: 5, max: 95);

        for (int i = 0; i < 100; i++)
        {
            var value = sim.GetAnalogValue(i);
            Assert.InRange(value, 5.0, 95.0);
        }
    }

    [Fact]
    public void SawtoothSimulator_ReturnsValueInRange()
    {
        var sim = new SawtoothSimulator(min: 0, max: 50);

        for (double t = 0; t < 120; t += 0.5)
        {
            var value = sim.GetAnalogValue(t);
            Assert.InRange(value, 0.0, 50.0);
        }
    }

    [Fact]
    public void SawtoothSimulator_AtStart_ReturnsMin()
    {
        var sim = new SawtoothSimulator(min: 10, max: 90);

        var value = sim.GetAnalogValue(0);

        Assert.Equal(10.0, value, precision: 5);
    }

    [Theory]
    [InlineData(SimulationPattern.Static, typeof(StaticValueSimulator))]
    [InlineData(SimulationPattern.Sine, typeof(SineWaveSimulator))]
    [InlineData(SimulationPattern.Ramp, typeof(RampSimulator))]
    [InlineData(SimulationPattern.Random, typeof(RandomSimulator))]
    [InlineData(SimulationPattern.Sawtooth, typeof(SawtoothSimulator))]
    public void ValueSimulatorFactory_CreatesCorrectType(SimulationPattern pattern, Type expectedType)
    {
        var sim = ValueSimulatorFactory.Create(pattern);

        Assert.IsType(expectedType, sim);
    }

    [Theory]
    [InlineData(0.0, false)]
    [InlineData(1.0, true)]
    [InlineData(42.0, true)]
    public void StaticSimulator_BinaryValue_ReturnsCorrectBool(double value, bool expected)
    {
        var sim = new StaticValueSimulator(value);

        Assert.Equal(expected, sim.GetBinaryValue(0));
    }

    [Fact]
    public void SineWaveSimulator_MultiStateValue_ReturnsValidState()
    {
        var sim = new SineWaveSimulator(min: 0, max: 100);
        uint numberOfStates = 5;

        for (double t = 0; t < 120; t += 0.5)
        {
            var state = sim.GetMultiStateValue(t, numberOfStates);
            Assert.InRange(state, 1u, numberOfStates);
        }
    }

    [Fact]
    public void SineWaveSimulator_DifferentPhaseOffsets_ProduceDifferentValues()
    {
        var sim1 = new SineWaveSimulator(0, 100, phaseOffset: 0.0);
        var sim2 = new SineWaveSimulator(0, 100, phaseOffset: 15.0);

        // At the same elapsed time, different offsets produce different values
        var val1 = sim1.GetAnalogValue(10.0);
        var val2 = sim2.GetAnalogValue(10.0);

        Assert.NotEqual(val1, val2);
    }

    [Fact]
    public void RampSimulator_DifferentPhaseOffsets_ProduceDifferentValues()
    {
        var sim1 = new RampSimulator(0, 100, phaseOffset: 0.0);
        var sim2 = new RampSimulator(0, 100, phaseOffset: 20.0);

        var val1 = sim1.GetAnalogValue(5.0);
        var val2 = sim2.GetAnalogValue(5.0);

        Assert.NotEqual(val1, val2);
    }

    [Fact]
    public void SawtoothSimulator_DifferentPhaseOffsets_ProduceDifferentValues()
    {
        var sim1 = new SawtoothSimulator(0, 100, phaseOffset: 0.0);
        var sim2 = new SawtoothSimulator(0, 100, phaseOffset: 25.0);

        var val1 = sim1.GetAnalogValue(5.0);
        var val2 = sim2.GetAnalogValue(5.0);

        Assert.NotEqual(val1, val2);
    }

    [Fact]
    public void ValueSimulatorFactory_PassesPhaseOffset()
    {
        var sim1 = ValueSimulatorFactory.Create(SimulationPattern.Sine, phaseOffset: 0.0);
        var sim2 = ValueSimulatorFactory.Create(SimulationPattern.Sine, phaseOffset: 10.0);

        Assert.NotEqual(sim1.GetAnalogValue(5.0), sim2.GetAnalogValue(5.0));
    }
}
