namespace Simbol.Core.Enums;

using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SimulationPattern
{
    Static,
    Sine,
    Ramp,
    Random,
    Sawtooth
}
