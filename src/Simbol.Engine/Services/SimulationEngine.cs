namespace Simbol.Engine.Services;

using System.IO.BACnet;
using System.IO.BACnet.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Simbol.Core.Configuration;
using Simbol.Engine.Models;

public class SimulationEngine : BackgroundService
{
    private readonly BacnetServiceHandler _serviceHandler;
    private readonly SimbolConfig _config;
    private readonly ILogger<SimulationEngine> _logger;
    private readonly DateTime _startTime = DateTime.UtcNow;

    public SimulationEngine(
        BacnetServiceHandler serviceHandler,
        SimbolConfig config,
        ILogger<SimulationEngine> logger)
    {
        _serviceHandler = serviceHandler;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Simulation engine starting with {Interval}ms tick interval",
            _config.Defaults.SimulationIntervalMs);

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_config.Defaults.SimulationIntervalMs));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var elapsed = (DateTime.UtcNow - _startTime).TotalSeconds;
                TickAllDevices(elapsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in simulation tick");
            }
        }
    }

    private void TickAllDevices(double elapsedSeconds)
    {
        foreach (var device in _serviceHandler.Devices.Values)
        {
            foreach (var simObj in device.SimulatedObjects)
            {
                if (simObj.IsOverridden) continue;

                try
                {
                    UpdateObjectValue(device, simObj, elapsedSeconds);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to update object {ObjectId} on device {DeviceId}",
                        simObj.ObjectId, device.DeviceId);
                }
            }
        }
    }

    private void UpdateObjectValue(SimulatedDevice device, SimulatedObject simObj, double elapsedSeconds)
    {
        IList<BacnetValue> newValue;
        bool valueChanged = false;

        switch (simObj.ValueCategory)
        {
            case ObjectValueCategory.Analog:
                var analogVal = (float)simObj.Simulator.GetAnalogValue(elapsedSeconds);
                newValue = new[] { new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL, analogVal) };
                valueChanged = true;
                break;

            case ObjectValueCategory.Binary:
                var binaryVal = simObj.Simulator.GetBinaryValue(elapsedSeconds) ? 1u : 0u;
                newValue = new[] { new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED, binaryVal) };
                valueChanged = true;
                break;

            case ObjectValueCategory.MultiState:
                var stateVal = simObj.Simulator.GetMultiStateValue(elapsedSeconds, simObj.NumberOfStates);
                newValue = new[] { new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT, stateVal) };
                valueChanged = true;
                break;

            default:
                return;
        }

        device.Storage.WriteProperty(simObj.ObjectId, BacnetPropertyIds.PROP_PRESENT_VALUE,
            uint.MaxValue, newValue);

        if (valueChanged)
        {
            var statusBitString = new BacnetBitString { bits_used = 4, value = new byte[] { 0x00 } };
            var statusFlagValue = new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_BIT_STRING,
                statusBitString);
            var covValues = new List<BacnetPropertyValue>
            {
                new()
                {
                    property = new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_PRESENT_VALUE, uint.MaxValue),
                    value = newValue
                },
                new()
                {
                    property = new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_STATUS_FLAGS, uint.MaxValue),
                    value = new[] { statusFlagValue }
                }
            };

            _serviceHandler.SendCovNotifications(device.DeviceId, simObj.ObjectId, covValues);
        }
    }
}
