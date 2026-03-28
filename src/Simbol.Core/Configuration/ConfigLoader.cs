namespace Simbol.Core.Configuration;

using System.Text.Json;
using System.Text.Json.Serialization;

public static class ConfigLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static SimbolConfig LoadFromFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Configuration file not found: {path}", path);

        var json = File.ReadAllText(path);
        return LoadFromJson(json);
    }

    public static SimbolConfig LoadFromJson(string json)
    {
        var config = JsonSerializer.Deserialize<SimbolConfig>(json, Options)
            ?? throw new InvalidOperationException("Failed to deserialize configuration.");

        config.Validate();
        return config;
    }

    public static string GenerateSampleConfig()
    {
        var sample = new SimbolConfig
        {
            Network = new NetworkConfig
            {
                Interface = "0.0.0.0",
                Port = 47808,
                BroadcastAddress = "255.255.255.255"
            },
            Defaults = new DefaultsConfig
            {
                VendorId = 999,
                VendorName = "Simbol Simulator",
                SimulationIntervalMs = 1000,
                UpdateIntervalMs = 5000,
                JitterPercent = 50,
                ValueRange = new ValueRangeConfig { Min = 0.0, Max = 100.0 },
                SimulationPattern = Enums.SimulationPattern.Sine
            },
            Devices = new List<DeviceConfig>
            {
                new()
                {
                    InstanceId = 1000,
                    Name = "SimDevice-1",
                    Description = "Simulated HVAC Controller",
                    Objects = new Dictionary<string, ObjectGroupConfig>
                    {
                        ["analog-input"] = new() { Count = 10, SimulationPattern = Enums.SimulationPattern.Sine, ValueRange = new() { Min = 60.0, Max = 80.0 }, UpdateIntervalMs = 10000 },
                        ["analog-output"] = new() { Count = 5, SimulationPattern = Enums.SimulationPattern.Static, DefaultValue = 72.0 },
                        ["analog-value"] = new() { Count = 5, UpdateIntervalMs = 15000 },
                        ["binary-input"] = new() { Count = 8, SimulationPattern = Enums.SimulationPattern.Random, UpdateIntervalMs = 8000 },
                        ["binary-output"] = new() { Count = 4 },
                        ["binary-value"] = new() { Count = 4 },
                        ["multi-state-input"] = new() { Count = 3, NumberOfStates = 4, UpdateIntervalMs = 12000 },
                        ["multi-state-output"] = new() { Count = 2, NumberOfStates = 3 },
                        ["multi-state-value"] = new() { Count = 2, NumberOfStates = 5 }
                    }
                },
                new()
                {
                    InstanceId = 1001,
                    Name = "SimDevice-2",
                    Description = "Simulated Lighting Controller",
                    Objects = new Dictionary<string, ObjectGroupConfig>
                    {
                        ["binary-output"] = new() { Count = 20, UpdateIntervalMs = 6000 },
                        ["analog-value"] = new() { Count = 10, UpdateIntervalMs = 20000 }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(sample, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
    }
}
