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

    /// <summary>The category: analog, binary, or multistate.</summary>
    public ObjectValueCategory ValueCategory { get; init; }
}

public enum ObjectValueCategory
{
    Analog,
    Binary,
    MultiState
}
