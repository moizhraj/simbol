namespace Simbol.Engine.Factories;

using System.IO.BACnet;
using System.IO.BACnet.Storage;
using Simbol.Core.Configuration;
using Simbol.Core.Simulation;
using Simbol.Engine.Models;
using BacnetObject = System.IO.BACnet.Storage.Object;
using BacnetProperty = System.IO.BACnet.Storage.Property;

public static class BacnetDeviceFactory
{
    public static SimulatedDevice CreateDevice(DeviceConfig deviceConfig, DefaultsConfig defaults)
    {
        var vendorId = deviceConfig.VendorId ?? defaults.VendorId;
        var vendorName = deviceConfig.VendorName ?? defaults.VendorName;
        var rng = new Random();

        var storageObjects = new List<BacnetObject>();
        var simulatedObjects = new List<SimulatedObject>();

        // Create the Device object (required)
        var deviceObj = CreateDeviceObject(deviceConfig, (uint)vendorId, vendorName, defaults);
        storageObjects.Add(deviceObj);

        // Create objects from config
        // Use device instance ID as base offset to ensure globally unique object instance numbers
        // e.g., device 1000 → objects at 1000001+, device 1001 → objects at 1001001+
        var instanceOffset = deviceConfig.InstanceId * 1000u;
        uint objectCounter = 0;

        foreach (var (objectTypeName, groupConfig) in deviceConfig.Objects)
        {
            var (bacnetType, category, isWritable) = ObjectTypeMapper.Resolve(objectTypeName);
            var pattern = groupConfig.SimulationPattern ?? defaults.SimulationPattern;
            var range = groupConfig.ValueRange ?? defaults.ValueRange;
            var baseIntervalMs = groupConfig.UpdateIntervalMs ?? defaults.UpdateIntervalMs;

            for (int i = 0; i < groupConfig.Count; i++)
            {
                objectCounter++;
                var instanceNumber = instanceOffset + objectCounter;
                var objectName = $"{deviceConfig.Name}-{objectTypeName}-{i + 1}";

                // Random phase offset (0 to period) for value variation between objects
                var phaseOffset = rng.NextDouble() * 60.0;
                var simulator = ValueSimulatorFactory.Create(pattern, range, groupConfig.DefaultValue, phaseOffset);

                // Jittered update interval: ±50% of base interval
                var jitterFactor = 0.5 + rng.NextDouble(); // 0.5 to 1.5
                var jitteredIntervalMs = (int)(baseIntervalMs * jitterFactor);

                var (storageObj, simObj) = BacnetObjectFactory.CreateObject(
                    bacnetType, instanceNumber, objectName,
                    category, isWritable, simulator, groupConfig, defaults,
                    jitteredIntervalMs);

                storageObjects.Add(storageObj);
                simulatedObjects.Add(simObj);
            }
        }

        // Build OBJECT_LIST property on the device object
        UpdateDeviceObjectList(deviceObj, storageObjects);

        var storage = new DeviceStorage
        {
            DeviceId = deviceConfig.InstanceId,
            Objects = storageObjects.ToArray()
        };

        return new SimulatedDevice
        {
            DeviceId = deviceConfig.InstanceId,
            Name = deviceConfig.Name,
            Storage = storage,
            SimulatedObjects = simulatedObjects,
            Config = deviceConfig
        };
    }

    private static BacnetObject CreateDeviceObject(DeviceConfig config, uint vendorId, string vendorName, DefaultsConfig defaults)
    {
        var properties = new List<BacnetProperty>
        {
            new()
            {
                Id = BacnetPropertyIds.PROP_OBJECT_IDENTIFIER,
                Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID,
                Value = new[] { $"OBJECT_DEVICE:{config.InstanceId}" }
            },
            new()
            {
                Id = BacnetPropertyIds.PROP_OBJECT_NAME,
                Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING,
                Value = new[] { config.Name }
            },
            new()
            {
                Id = BacnetPropertyIds.PROP_OBJECT_TYPE,
                Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED,
                Value = new[] { ((uint)BacnetObjectTypes.OBJECT_DEVICE).ToString() }
            },
            new()
            {
                Id = BacnetPropertyIds.PROP_SYSTEM_STATUS,
                Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED,
                Value = new[] { "0" } // operational
            },
            new()
            {
                Id = BacnetPropertyIds.PROP_VENDOR_NAME,
                Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING,
                Value = new[] { vendorName }
            },
            new()
            {
                Id = BacnetPropertyIds.PROP_VENDOR_IDENTIFIER,
                Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT,
                Value = new[] { vendorId.ToString() }
            },
            new()
            {
                Id = BacnetPropertyIds.PROP_MODEL_NAME,
                Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING,
                Value = new[] { "Simbol Virtual Device" }
            },
            new()
            {
                Id = BacnetPropertyIds.PROP_FIRMWARE_REVISION,
                Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING,
                Value = new[] { "1.0.0" }
            },
            new()
            {
                Id = BacnetPropertyIds.PROP_APPLICATION_SOFTWARE_VERSION,
                Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING,
                Value = new[] { "1.0.0" }
            },
            new()
            {
                Id = BacnetPropertyIds.PROP_DESCRIPTION,
                Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING,
                Value = new[] { config.Description }
            },
            new()
            {
                Id = BacnetPropertyIds.PROP_PROTOCOL_VERSION,
                Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT,
                Value = new[] { "1" }
            },
            new()
            {
                Id = BacnetPropertyIds.PROP_PROTOCOL_REVISION,
                Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT,
                Value = new[] { "14" }
            },
            new()
            {
                Id = BacnetPropertyIds.PROP_PROTOCOL_SERVICES_SUPPORTED,
                Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING,
                Value = new[] { "110000101111001010100" }
            },
            new()
            {
                Id = BacnetPropertyIds.PROP_PROTOCOL_OBJECT_TYPES_SUPPORTED,
                Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING,
                Value = new[] { "1111110000000100000001" }
            },
            new()
            {
                Id = BacnetPropertyIds.PROP_OBJECT_LIST,
                Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID,
                Value = Array.Empty<string>()
            },
            new()
            {
                Id = BacnetPropertyIds.PROP_MAX_APDU_LENGTH_ACCEPTED,
                Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT,
                Value = new[] { "1476" }
            },
            new()
            {
                Id = BacnetPropertyIds.PROP_SEGMENTATION_SUPPORTED,
                Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED,
                Value = new[] { ((uint)BacnetSegmentations.SEGMENTATION_BOTH).ToString() }
            },
            new()
            {
                Id = BacnetPropertyIds.PROP_DATABASE_REVISION,
                Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT,
                Value = new[] { "1" }
            }
        };

        return new BacnetObject
        {
            Type = BacnetObjectTypes.OBJECT_DEVICE,
            Instance = config.InstanceId,
            Properties = properties.ToArray()
        };
    }

    private static void UpdateDeviceObjectList(BacnetObject deviceObj, List<BacnetObject> allObjects)
    {
        var objectListProp = deviceObj.Properties.First(p => p.Id == BacnetPropertyIds.PROP_OBJECT_LIST);
        objectListProp.Value = allObjects.Select(o => $"{o.Type}:{o.Instance}").ToArray();
    }
}
