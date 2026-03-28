namespace Simbol.Core.Tests;

using Simbol.Core.Configuration;

public class ConfigValidationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(99999)]
    public void NetworkConfig_InvalidPort_Throws(int port)
    {
        var config = new NetworkConfig { Port = port };

        Assert.Throws<InvalidOperationException>(() => config.Validate());
    }

    [Fact]
    public void DefaultsConfig_LowInterval_Throws()
    {
        var config = new DefaultsConfig { SimulationIntervalMs = 50 };

        Assert.Throws<InvalidOperationException>(() => config.Validate());
    }

    [Fact]
    public void DeviceConfig_NoName_Throws()
    {
        var config = new DeviceConfig
        {
            InstanceId = 1,
            Name = "",
            Objects = new Dictionary<string, ObjectGroupConfig>
            {
                ["analog-input"] = new() { Count = 1 }
            }
        };

        Assert.Throws<InvalidOperationException>(() => config.Validate());
    }

    [Fact]
    public void DeviceConfig_InstanceIdTooHigh_Throws()
    {
        var config = new DeviceConfig
        {
            InstanceId = 4194303,
            Name = "OverflowDevice",
            Objects = new Dictionary<string, ObjectGroupConfig>
            {
                ["analog-input"] = new() { Count = 1 }
            }
        };

        Assert.Throws<InvalidOperationException>(() => config.Validate());
    }

    [Fact]
    public void DeviceConfig_NoObjects_Throws()
    {
        var config = new DeviceConfig
        {
            InstanceId = 1,
            Name = "EmptyDevice",
            Objects = new Dictionary<string, ObjectGroupConfig>()
        };

        Assert.Throws<InvalidOperationException>(() => config.Validate());
    }

    [Fact]
    public void ValueRange_MinGreaterThanMax_Throws()
    {
        var config = new ValueRangeConfig { Min = 100, Max = 0 };

        Assert.Throws<InvalidOperationException>(() => config.Validate());
    }

    [Fact]
    public void ValueRange_MinEqualsMax_Throws()
    {
        var config = new ValueRangeConfig { Min = 50, Max = 50 };

        Assert.Throws<InvalidOperationException>(() => config.Validate());
    }
}
