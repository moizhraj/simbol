namespace Simbol.Core.Configuration;

using System.Text.Json.Serialization;

public class DeviceConfig
{
    public uint InstanceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? VendorId { get; set; }
    public string? VendorName { get; set; }

    [JsonPropertyName("objects")]
    public Dictionary<string, ObjectGroupConfig> Objects { get; set; } = new();

    public void Validate()
    {
        if (InstanceId > 4194302)
            throw new InvalidOperationException($"Device instance ID {InstanceId} exceeds BACnet maximum (4194302).");

        if (string.IsNullOrWhiteSpace(Name))
            throw new InvalidOperationException($"Device with instance ID {InstanceId} must have a name.");

        if (Objects.Count == 0)
            throw new InvalidOperationException($"Device '{Name}' (ID: {InstanceId}) must have at least one object group.");

        var validObjectTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "analog-input", "analog-output", "analog-value",
            "binary-input", "binary-output", "binary-value",
            "multi-state-input", "multi-state-output", "multi-state-value"
        };

        foreach (var (objectType, config) in Objects)
        {
            if (!validObjectTypes.Contains(objectType))
                throw new InvalidOperationException($"Unknown object type '{objectType}' in device '{Name}'. Valid types: {string.Join(", ", validObjectTypes)}");

            config.Validate(objectType);
        }
    }
}
