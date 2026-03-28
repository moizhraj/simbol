namespace Simbol.Engine.Tests;

using System.IO.BACnet;
using System.IO.BACnet.Storage;
using Simbol.Core.Configuration;
using Simbol.Core.Simulation;
using Simbol.Engine.Factories;
using Simbol.Engine.Models;

public class BacnetObjectFactoryTests
{
    private static DefaultsConfig CreateDefaults() => new()
    {
        VendorId = 999,
        VendorName = "Test",
        SimulationIntervalMs = 1000,
        ValueRange = new ValueRangeConfig { Min = 0, Max = 100 }
    };

    private static ObjectGroupConfig CreateGroupConfig(int count = 1, int numberOfStates = 3) => new()
    {
        Count = count,
        NumberOfStates = numberOfStates
    };

    [Fact]
    public void CreateObject_AnalogInput_HasRequiredProperties()
    {
        var (storageObj, _) = BacnetObjectFactory.CreateObject(
            BacnetObjectTypes.OBJECT_ANALOG_INPUT, 1, "AI-1",
            ObjectValueCategory.Analog, false,
            new StaticValueSimulator(50), CreateGroupConfig(), CreateDefaults());

        var propIds = storageObj.Properties.Select(p => p.Id).ToHashSet();

        Assert.Contains(BacnetPropertyIds.PROP_PRESENT_VALUE, propIds);
        Assert.Contains(BacnetPropertyIds.PROP_OBJECT_NAME, propIds);
        Assert.Contains(BacnetPropertyIds.PROP_UNITS, propIds);
        Assert.Contains(BacnetPropertyIds.PROP_STATUS_FLAGS, propIds);
    }

    [Fact]
    public void CreateObject_BinaryOutput_HasActiveInactiveText()
    {
        var (storageObj, _) = BacnetObjectFactory.CreateObject(
            BacnetObjectTypes.OBJECT_BINARY_OUTPUT, 1, "BO-1",
            ObjectValueCategory.Binary, true,
            new StaticValueSimulator(0), CreateGroupConfig(), CreateDefaults());

        var propIds = storageObj.Properties.Select(p => p.Id).ToHashSet();

        Assert.Contains(BacnetPropertyIds.PROP_ACTIVE_TEXT, propIds);
        Assert.Contains(BacnetPropertyIds.PROP_INACTIVE_TEXT, propIds);
    }

    [Fact]
    public void CreateObject_MultiStateInput_HasNumberOfStates()
    {
        var (storageObj, _) = BacnetObjectFactory.CreateObject(
            BacnetObjectTypes.OBJECT_MULTI_STATE_INPUT, 1, "MSI-1",
            ObjectValueCategory.MultiState, false,
            new StaticValueSimulator(1), CreateGroupConfig(numberOfStates: 4), CreateDefaults());

        var propIds = storageObj.Properties.Select(p => p.Id).ToHashSet();

        Assert.Contains(BacnetPropertyIds.PROP_NUMBER_OF_STATES, propIds);

        var numStates = storageObj.Properties
            .First(p => p.Id == BacnetPropertyIds.PROP_NUMBER_OF_STATES);
        Assert.Equal("4", numStates.Value[0]);
    }

    [Fact]
    public void CreateObject_ReturnsMatchingSimulatedObject()
    {
        var simulator = new StaticValueSimulator(50);
        var (_, simObj) = BacnetObjectFactory.CreateObject(
            BacnetObjectTypes.OBJECT_ANALOG_OUTPUT, 5, "AO-5",
            ObjectValueCategory.Analog, true,
            simulator, CreateGroupConfig(), CreateDefaults());

        Assert.Equal(BacnetObjectTypes.OBJECT_ANALOG_OUTPUT, simObj.ObjectId.Type);
        Assert.Equal(5u, simObj.ObjectId.Instance);
        Assert.Equal(ObjectValueCategory.Analog, simObj.ValueCategory);
        Assert.True(simObj.IsWritable);
    }
}
