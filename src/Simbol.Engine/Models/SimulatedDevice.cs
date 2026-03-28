namespace Simbol.Engine.Models;

using System.IO.BACnet.Storage;
using Simbol.Core.Configuration;

public class SimulatedDevice
{
    public uint DeviceId { get; init; }
    public string Name { get; init; } = string.Empty;
    public DeviceStorage Storage { get; init; } = null!;
    public List<SimulatedObject> SimulatedObjects { get; init; } = new();
    public DeviceConfig Config { get; init; } = null!;
    public DeviceStats Stats { get; } = new();
}
