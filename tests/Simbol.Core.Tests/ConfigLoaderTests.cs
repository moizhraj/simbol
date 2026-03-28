namespace Simbol.Core.Tests;

using Simbol.Core.Configuration;

public class ConfigLoaderTests
{
    private const string ValidConfigJson = """
    {
      "network": { "interface": "0.0.0.0", "port": 47808 },
      "defaults": { "vendorId": 999, "vendorName": "Test", "simulationIntervalMs": 1000, "valueRange": { "min": 0, "max": 100 }, "simulationPattern": "sine" },
      "devices": [
        {
          "instanceId": 1000,
          "name": "TestDevice",
          "description": "Test",
          "objects": {
            "analog-input": { "count": 5 },
            "binary-output": { "count": 3 }
          }
        }
      ]
    }
    """;

    [Fact]
    public void LoadFromJson_ValidConfig_ReturnsSimbolConfig()
    {
        var config = ConfigLoader.LoadFromJson(ValidConfigJson);

        Assert.NotNull(config);
        Assert.Single(config.Devices);
        Assert.Equal("TestDevice", config.Devices[0].Name);
        Assert.Equal(2, config.Devices[0].Objects.Count);
        Assert.Equal(5, config.Devices[0].Objects["analog-input"].Count);
        Assert.Equal(3, config.Devices[0].Objects["binary-output"].Count);
    }

    [Fact]
    public void LoadFromJson_MissingDevices_ThrowsInvalidOperation()
    {
        var json = """
        {
          "network": { "interface": "0.0.0.0", "port": 47808 },
          "defaults": { "vendorId": 999, "vendorName": "Test", "simulationIntervalMs": 1000 },
          "devices": []
        }
        """;

        Assert.Throws<InvalidOperationException>(() => ConfigLoader.LoadFromJson(json));
    }

    [Fact]
    public void LoadFromJson_DuplicateInstanceIds_ThrowsInvalidOperation()
    {
        var json = """
        {
          "network": { "interface": "0.0.0.0", "port": 47808 },
          "defaults": { "vendorId": 999, "vendorName": "Test", "simulationIntervalMs": 1000 },
          "devices": [
            { "instanceId": 1000, "name": "Device1", "objects": { "analog-input": { "count": 1 } } },
            { "instanceId": 1000, "name": "Device2", "objects": { "binary-input": { "count": 1 } } }
          ]
        }
        """;

        Assert.Throws<InvalidOperationException>(() => ConfigLoader.LoadFromJson(json));
    }

    [Fact]
    public void LoadFromJson_InvalidObjectType_ThrowsInvalidOperation()
    {
        var json = """
        {
          "network": { "interface": "0.0.0.0", "port": 47808 },
          "defaults": { "vendorId": 999, "vendorName": "Test", "simulationIntervalMs": 1000 },
          "devices": [
            { "instanceId": 1000, "name": "Device1", "objects": { "foo-bar": { "count": 1 } } }
          ]
        }
        """;

        Assert.Throws<InvalidOperationException>(() => ConfigLoader.LoadFromJson(json));
    }

    [Fact]
    public void LoadFromFile_NonExistentFile_ThrowsFileNotFound()
    {
        Assert.Throws<FileNotFoundException>(() => ConfigLoader.LoadFromFile(@"C:\nonexistent\path\config.json"));
    }

    [Fact]
    public void GenerateSampleConfig_ProducesValidJson()
    {
        var json = ConfigLoader.GenerateSampleConfig();

        Assert.False(string.IsNullOrWhiteSpace(json));

        var config = ConfigLoader.LoadFromJson(json);

        Assert.NotNull(config);
        Assert.True(config.Devices.Count > 0);
        Assert.Equal(47808, config.Network.Port);
    }
}
