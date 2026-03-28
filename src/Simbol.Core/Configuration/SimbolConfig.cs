namespace Simbol.Core.Configuration;

public class SimbolConfig
{
    public NetworkConfig Network { get; set; } = new();
    public DefaultsConfig Defaults { get; set; } = new();
    public List<DeviceConfig> Devices { get; set; } = new();

    public void Validate()
    {
        Network.Validate();
        Defaults.Validate();

        if (Devices.Count == 0)
            throw new InvalidOperationException("Configuration must define at least one device.");

        var instanceIds = new HashSet<uint>();
        foreach (var device in Devices)
        {
            device.Validate();
            if (!instanceIds.Add(device.InstanceId))
                throw new InvalidOperationException($"Duplicate device instance ID: {device.InstanceId}");
        }
    }
}
