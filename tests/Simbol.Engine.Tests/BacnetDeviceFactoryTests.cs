namespace Simbol.Engine.Tests;

using System.IO.BACnet;
using System.IO.BACnet.Storage;
using Simbol.Core.Configuration;
using Simbol.Engine.Factories;

public class BacnetDeviceFactoryTests
{
    private static DefaultsConfig CreateDefaults() => new()
    {
        VendorId = 999,
        VendorName = "DefaultVendor",
        SimulationIntervalMs = 1000,
        ValueRange = new ValueRangeConfig { Min = 0, Max = 100 }
    };

    private static DeviceConfig CreateDeviceConfig() => new()
    {
        InstanceId = 1000,
        Name = "TestDevice",
        Description = "Unit test device",
        Objects = new Dictionary<string, ObjectGroupConfig>
        {
            ["analog-input"] = new() { Count = 3 },
            ["binary-output"] = new() { Count = 2 }
        }
    };

    [Fact]
    public void CreateDevice_HasDeviceObject()
    {
        var device = BacnetDeviceFactory.CreateDevice(CreateDeviceConfig(), CreateDefaults());

        var deviceObj = device.Storage.Objects
            .FirstOrDefault(o => o.Type == BacnetObjectTypes.OBJECT_DEVICE);

        Assert.NotNull(deviceObj);
        Assert.Equal(1000u, deviceObj.Instance);
    }

    [Fact]
    public void CreateDevice_HasCorrectObjectCount()
    {
        var device = BacnetDeviceFactory.CreateDevice(CreateDeviceConfig(), CreateDefaults());

        // 1 device object + 3 analog-input + 2 binary-output = 6
        Assert.Equal(6, device.Storage.Objects.Length);
        Assert.Equal(5, device.SimulatedObjects.Count);
    }

    [Fact]
    public void CreateDevice_DeviceObjectHasRequiredProperties()
    {
        var device = BacnetDeviceFactory.CreateDevice(CreateDeviceConfig(), CreateDefaults());

        var deviceObj = device.Storage.Objects
            .First(o => o.Type == BacnetObjectTypes.OBJECT_DEVICE);
        var propIds = deviceObj.Properties.Select(p => p.Id).ToHashSet();

        Assert.Contains(BacnetPropertyIds.PROP_VENDOR_NAME, propIds);
        Assert.Contains(BacnetPropertyIds.PROP_MODEL_NAME, propIds);
        Assert.Contains(BacnetPropertyIds.PROP_OBJECT_LIST, propIds);
        Assert.Contains(BacnetPropertyIds.PROP_VENDOR_IDENTIFIER, propIds);
    }

    [Fact]
    public void CreateDevice_UsesDefaultsWhenDeviceOverridesNull()
    {
        var config = CreateDeviceConfig();
        config.VendorId = null;
        config.VendorName = null;

        var defaults = CreateDefaults();
        var device = BacnetDeviceFactory.CreateDevice(config, defaults);

        var deviceObj = device.Storage.Objects
            .First(o => o.Type == BacnetObjectTypes.OBJECT_DEVICE);

        var vendorName = deviceObj.Properties
            .First(p => p.Id == BacnetPropertyIds.PROP_VENDOR_NAME);
        Assert.Equal("DefaultVendor", vendorName.Value[0]);

        var vendorId = deviceObj.Properties
            .First(p => p.Id == BacnetPropertyIds.PROP_VENDOR_IDENTIFIER);
        Assert.Equal("999", vendorId.Value[0]);
    }
}
