namespace Simbol.Core.Enums;

using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BacnetObjectCategory
{
    AnalogInput,
    AnalogOutput,
    AnalogValue,
    BinaryInput,
    BinaryOutput,
    BinaryValue,
    MultiStateInput,
    MultiStateOutput,
    MultiStateValue
}
