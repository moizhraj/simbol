namespace Simbol.Engine.Factories;

using System.IO.BACnet;
using System.IO.BACnet.Storage;
using Simbol.Core.Configuration;
using Simbol.Core.Enums;
using Simbol.Core.Simulation;
using Simbol.Engine.Models;
using BacnetObject = System.IO.BACnet.Storage.Object;
using BacnetProperty = System.IO.BACnet.Storage.Property;

public static class BacnetObjectFactory
{
    public static (BacnetObject StorageObject, SimulatedObject SimObject) CreateObject(
        BacnetObjectTypes objectType,
        uint instanceNumber,
        string objectName,
        ObjectValueCategory category,
        bool isWritable,
        IValueSimulator simulator,
        ObjectGroupConfig groupConfig,
        DefaultsConfig defaults,
        int updateIntervalMs = 5000)
    {
        var properties = new List<BacnetProperty>();

        // Object_Identifier
        properties.Add(new BacnetProperty
        {
            Id = BacnetPropertyIds.PROP_OBJECT_IDENTIFIER,
            Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID,
            Value = new[] { $"{objectType}:{instanceNumber}" }
        });

        // Object_Name
        properties.Add(new BacnetProperty
        {
            Id = BacnetPropertyIds.PROP_OBJECT_NAME,
            Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING,
            Value = new[] { objectName }
        });

        // Object_Type
        properties.Add(new BacnetProperty
        {
            Id = BacnetPropertyIds.PROP_OBJECT_TYPE,
            Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED,
            Value = new[] { ((uint)objectType).ToString() }
        });

        // Present_Value — type depends on category
        switch (category)
        {
            case ObjectValueCategory.Analog:
                var analogValue = groupConfig.DefaultValue ?? (defaults.ValueRange.Min + defaults.ValueRange.Max) / 2.0;
                properties.Add(new BacnetProperty
                {
                    Id = BacnetPropertyIds.PROP_PRESENT_VALUE,
                    Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL,
                    Value = new[] { analogValue.ToString(System.Globalization.CultureInfo.InvariantCulture) }
                });
                // Units
                properties.Add(new BacnetProperty
                {
                    Id = BacnetPropertyIds.PROP_UNITS,
                    Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED,
                    Value = new[] { ((uint)BacnetUnitsId.UNITS_NO_UNITS).ToString() }
                });
                break;

            case ObjectValueCategory.Binary:
                properties.Add(new BacnetProperty
                {
                    Id = BacnetPropertyIds.PROP_PRESENT_VALUE,
                    Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED,
                    Value = new[] { "0" } // inactive
                });
                // Active/Inactive text
                properties.Add(new BacnetProperty
                {
                    Id = BacnetPropertyIds.PROP_ACTIVE_TEXT,
                    Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING,
                    Value = new[] { "Active" }
                });
                properties.Add(new BacnetProperty
                {
                    Id = BacnetPropertyIds.PROP_INACTIVE_TEXT,
                    Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING,
                    Value = new[] { "Inactive" }
                });
                break;

            case ObjectValueCategory.MultiState:
                properties.Add(new BacnetProperty
                {
                    Id = BacnetPropertyIds.PROP_PRESENT_VALUE,
                    Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT,
                    Value = new[] { "1" }
                });
                // Number_Of_States
                properties.Add(new BacnetProperty
                {
                    Id = BacnetPropertyIds.PROP_NUMBER_OF_STATES,
                    Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT,
                    Value = new[] { groupConfig.NumberOfStates.ToString() }
                });
                // State_Text
                var stateTexts = Enumerable.Range(1, groupConfig.NumberOfStates)
                    .Select(i => $"State-{i}")
                    .ToArray();
                properties.Add(new BacnetProperty
                {
                    Id = BacnetPropertyIds.PROP_STATE_TEXT,
                    Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING,
                    Value = stateTexts
                });
                break;
        }

        // Status_Flags (all clear = normal)
        properties.Add(new BacnetProperty
        {
            Id = BacnetPropertyIds.PROP_STATUS_FLAGS,
            Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING,
            Value = new[] { "0000" }
        });

        // Out_Of_Service
        properties.Add(new BacnetProperty
        {
            Id = BacnetPropertyIds.PROP_OUT_OF_SERVICE,
            Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN,
            Value = new[] { "False" }
        });

        // Description
        properties.Add(new BacnetProperty
        {
            Id = BacnetPropertyIds.PROP_DESCRIPTION,
            Tag = BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING,
            Value = new[] { $"Simulated {objectType} #{instanceNumber}" }
        });

        var storageObj = new BacnetObject
        {
            Type = objectType,
            Instance = instanceNumber,
            Properties = properties.ToArray()
        };

        var simObj = new SimulatedObject
        {
            ObjectId = new BacnetObjectId(objectType, instanceNumber),
            Simulator = simulator,
            IsWritable = isWritable,
            ValueCategory = category,
            NumberOfStates = (uint)groupConfig.NumberOfStates,
            UpdateIntervalMs = updateIntervalMs
        };

        return (storageObj, simObj);
    }
}
