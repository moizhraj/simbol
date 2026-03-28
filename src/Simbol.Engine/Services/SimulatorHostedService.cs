namespace Simbol.Engine.Services;

using System.IO.BACnet;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Simbol.Core.Configuration;
using Simbol.Engine.Factories;

public class SimulatorHostedService : IHostedService
{
    private readonly SimbolConfig _config;
    private readonly BacnetServiceHandler _serviceHandler;
    private readonly ILogger<SimulatorHostedService> _logger;
    private BacnetClient? _client;

    public SimulatorHostedService(
        SimbolConfig config,
        BacnetServiceHandler serviceHandler,
        ILogger<SimulatorHostedService> logger)
    {
        _config = config;
        _serviceHandler = serviceHandler;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("=== Simbol BACnet Simulator ===");

        // Create devices from config, chaining instance bases to avoid collisions
        uint instanceBase = 0;
        foreach (var deviceConfig in _config.Devices)
        {
            var device = BacnetDeviceFactory.CreateDevice(deviceConfig, _config.Defaults, instanceBase);
            _serviceHandler.RegisterDevice(device);
            instanceBase = device.NextInstanceBase;

            var objectCount = device.SimulatedObjects.Count;
            _logger.LogInformation("Created device '{Name}' (ID: {DeviceId}) with {ObjectCount} simulated objects",
                device.Name, device.DeviceId, objectCount);
        }

        // Create and start BACnet transport
        var transport = new BacnetIpUdpProtocolTransport(_config.Network.Port, false, false, 1472, _config.Network.Interface);
        _client = new BacnetClient(transport);

        _serviceHandler.AttachClient(_client);
        _client.Start();

        _logger.LogInformation("BACnet/IP transport started on {Interface}:{Port}",
            _config.Network.Interface, _config.Network.Port);
        _logger.LogInformation("Simulator is running. Press Ctrl+C to stop.");

        // Send initial I-Am for all devices
        foreach (var device in _serviceHandler.Devices.Values)
        {
            _client.Iam(device.DeviceId, BacnetSegmentations.SEGMENTATION_BOTH);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping BACnet simulator...");

        _serviceHandler.DetachClient();

        if (_client != null)
        {
            _client.Dispose();
            _client = null;
        }

        _logger.LogInformation("BACnet simulator stopped.");
        return Task.CompletedTask;
    }
}
