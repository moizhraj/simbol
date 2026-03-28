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

    [Fact]
    public void CreateDevice_ObjectInstancesAreGloballyUnique()
    {
        var defaults = CreateDefaults();

        var config1 = new DeviceConfig
        {
            InstanceId = 1000, Name = "Device-A", Description = "A",
            Objects = new() { ["analog-input"] = new() { Count = 3 } }
        };
        var config2 = new DeviceConfig
        {
            InstanceId = 1001, Name = "Device-B", Description = "B",
            Objects = new() { ["analog-input"] = new() { Count = 3 } }
        };

        var device1 = BacnetDeviceFactory.CreateDevice(config1, defaults);
        var device2 = BacnetDeviceFactory.CreateDevice(config2, defaults);

        var instances1 = device1.Storage.Objects
            .Where(o => o.Type == BacnetObjectTypes.OBJECT_ANALOG_INPUT)
            .Select(o => o.Instance).ToHashSet();
        var instances2 = device2.Storage.Objects
            .Where(o => o.Type == BacnetObjectTypes.OBJECT_ANALOG_INPUT)
            .Select(o => o.Instance).ToHashSet();

        // No overlap between device instance numbers
        Assert.Empty(instances1.Intersect(instances2));
    }

    [Fact]
    public void CreateDevice_ObjectNamesUseDeviceName()
    {
        var device = BacnetDeviceFactory.CreateDevice(CreateDeviceConfig(), CreateDefaults());

        var aiObjects = device.Storage.Objects
            .Where(o => o.Type == BacnetObjectTypes.OBJECT_ANALOG_INPUT)
            .ToList();

        foreach (var obj in aiObjects)
        {
            var nameProp = obj.Properties.First(p => p.Id == BacnetPropertyIds.PROP_OBJECT_NAME);
            Assert.StartsWith("TestDevice-analog-input-", nameProp.Value[0]);
        }
    }

    [Fact]
    public void CreateDevice_ObjectsHaveJitteredUpdateIntervals()
    {
        var device = BacnetDeviceFactory.CreateDevice(CreateDeviceConfig(), CreateDefaults());

        var intervals = device.SimulatedObjects.Select(o => o.UpdateIntervalMs).ToList();

        // All intervals should be > 0
        Assert.All(intervals, i => Assert.True(i > 0));

        // With 5 objects and jitter, not all intervals should be identical
        Assert.True(intervals.Distinct().Count() > 1,
            "Expected jittered intervals to produce variation across objects");
    }

    [Fact]
    public void CreateDevice_ObjectsHavePhaseOffsetVariation()
    {
        var config = new DeviceConfig
        {
            InstanceId = 2000, Name = "PhaseTest", Description = "Test",
            Objects = new() { ["analog-input"] = new() { Count = 5 } }
        };
        var device = BacnetDeviceFactory.CreateDevice(config, CreateDefaults());

        // Simulate at the same elapsed time — values should differ due to phase offsets
        var values = device.SimulatedObjects
            .Select(o => o.Simulator.GetAnalogValue(10.0))
            .ToList();

        Assert.True(values.Distinct().Count() > 1,
            "Expected phase offsets to produce different values across objects");
    }

    [Fact]
    public void CreateDevice_ExplicitUpdateInterval_AppliedWithJitter()
    {
        var config = new DeviceConfig
        {
            InstanceId = 3000, Name = "IntervalTest", Description = "Test",
            Objects = new()
            {
                ["analog-input"] = new() { Count = 5, UpdateIntervalMs = 10000 }
            }
        };
        var device = BacnetDeviceFactory.CreateDevice(config, CreateDefaults());

        // All intervals should be in the jitter range: 10000 * 0.5 to 10000 * 1.5
        foreach (var obj in device.SimulatedObjects)
        {
            Assert.InRange(obj.UpdateIntervalMs, 5000, 15000);
        }
    }
}
