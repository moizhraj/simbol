namespace Simbol.Engine.Factories;

using System.IO.BACnet;
using Simbol.Engine.Models;

public static class ObjectTypeMapper
{
    private static readonly Dictionary<string, (BacnetObjectTypes Type, ObjectValueCategory Category, bool IsWritable)> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["analog-input"] = (BacnetObjectTypes.OBJECT_ANALOG_INPUT, ObjectValueCategory.Analog, false),
        ["analog-output"] = (BacnetObjectTypes.OBJECT_ANALOG_OUTPUT, ObjectValueCategory.Analog, true),
        ["analog-value"] = (BacnetObjectTypes.OBJECT_ANALOG_VALUE, ObjectValueCategory.Analog, true),
        ["binary-input"] = (BacnetObjectTypes.OBJECT_BINARY_INPUT, ObjectValueCategory.Binary, false),
        ["binary-output"] = (BacnetObjectTypes.OBJECT_BINARY_OUTPUT, ObjectValueCategory.Binary, true),
        ["binary-value"] = (BacnetObjectTypes.OBJECT_BINARY_VALUE, ObjectValueCategory.Binary, true),
        ["multi-state-input"] = (BacnetObjectTypes.OBJECT_MULTI_STATE_INPUT, ObjectValueCategory.MultiState, false),
        ["multi-state-output"] = (BacnetObjectTypes.OBJECT_MULTI_STATE_OUTPUT, ObjectValueCategory.MultiState, true),
        ["multi-state-value"] = (BacnetObjectTypes.OBJECT_MULTI_STATE_VALUE, ObjectValueCategory.MultiState, true),
    };

    public static (BacnetObjectTypes Type, ObjectValueCategory Category, bool IsWritable) Resolve(string objectTypeName)
    {
        if (Map.TryGetValue(objectTypeName, out var result))
            return result;
        throw new ArgumentException($"Unknown BACnet object type: {objectTypeName}");
    }
}
