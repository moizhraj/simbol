namespace Simbol.Engine.Models;

using System.IO.BACnet;
using Simbol.Core.Simulation;

public class SimulatedObject
{
    public BacnetObjectId ObjectId { get; init; }
    public IValueSimulator Simulator { get; init; } = null!;
    public bool IsWritable { get; init; }
    public bool IsOverridden { get; set; }
    public uint NumberOfStates { get; init; } = 3;

    /// The category: analog, binary, or multistate.
    public ObjectValueCategory ValueCategory { get; init; }

    /// Per-object update interval in milliseconds (with jitter applied).
    public int UpdateIntervalMs { get; init; } = 5000;

    /// Tracks when this object was last updated.
    public DateTime LastUpdateTime { get; set; } = DateTime.MinValue;
}

public enum ObjectValueCategory
{
    Analog,
    Binary,
    MultiState
}
