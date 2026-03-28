namespace Simbol.Engine.Services;

using System.IO.BACnet;
using System.IO.BACnet.Storage;
using Microsoft.Extensions.Logging;
using Simbol.Engine.Models;

public class BacnetServiceHandler
{
    private readonly Dictionary<uint, SimulatedDevice> _devices = new();
    private readonly CovManager _covManager;
    private readonly ILogger<BacnetServiceHandler> _logger;
    private readonly IConsoleDisplay _display;
    private BacnetClient? _client;

    public BacnetServiceHandler(CovManager covManager, ILogger<BacnetServiceHandler> logger, IConsoleDisplay display)
    {
        _covManager = covManager;
        _logger = logger;
        _display = display;
    }

    public void RegisterDevice(SimulatedDevice device)
    {
        _devices[device.DeviceId] = device;
    }

    public void AttachClient(BacnetClient client)
    {
        _client = client;
        client.OnWhoIs += HandleWhoIs;
        client.OnReadPropertyRequest += HandleReadProperty;
        client.OnReadPropertyMultipleRequest += HandleReadPropertyMultiple;
        client.OnWritePropertyRequest += HandleWriteProperty;
        client.OnSubscribeCOV += HandleSubscribeCov;
        client.OnIam += HandleIam;

        _logger.LogInformation("BACnet service handler attached to client with {Count} devices", _devices.Count);
    }

    public void DetachClient()
    {
        if (_client == null) return;

        _client.OnWhoIs -= HandleWhoIs;
        _client.OnReadPropertyRequest -= HandleReadProperty;
        _client.OnReadPropertyMultipleRequest -= HandleReadPropertyMultiple;
        _client.OnWritePropertyRequest -= HandleWriteProperty;
        _client.OnSubscribeCOV -= HandleSubscribeCov;
        _client.OnIam -= HandleIam;

        _client = null;
    }

    public BacnetClient? Client => _client;
    public IReadOnlyDictionary<uint, SimulatedDevice> Devices => _devices;

    private void HandleWhoIs(BacnetClient sender, BacnetAddress adr, int lowLimit, int highLimit)
    {
        var clientAddr = adr.ToString();
        var matchCount = 0;
        foreach (var device in _devices.Values)
        {
            if ((lowLimit == -1 && highLimit == -1) ||
                (device.DeviceId >= (uint)lowLimit && device.DeviceId <= (uint)highLimit))
            {
                device.Stats.RecordWhoIs(clientAddr);
                sender.Iam(device.DeviceId, BacnetSegmentations.SEGMENTATION_BOTH);
                _logger.LogDebug("I-Am sent for device {DeviceId} ({Name})", device.DeviceId, device.Name);
                matchCount++;
            }
        }

        _display.AddActivity($"Who-Is from {adr} → {matchCount} device(s) responded");
    }

    private void HandleReadProperty(BacnetClient sender, BacnetAddress adr, byte invokeId,
        BacnetObjectId objectId, BacnetPropertyReference property, BacnetMaxSegments maxSegments)
    {
        var clientAddr = adr.ToString();
        foreach (var device in _devices.Values)
        {
            var status = device.Storage.ReadProperty(objectId, (BacnetPropertyIds)property.propertyIdentifier,
                property.propertyArrayIndex, out var value);

            if (status == DeviceStorage.ErrorCodes.Good)
            {
                device.Stats.RecordReadProperty(clientAddr);
                sender.ReadPropertyResponse(adr, invokeId, sender.GetSegmentBuffer(maxSegments),
                    objectId, property, value);
                return;
            }

            if (status != DeviceStorage.ErrorCodes.UnknownObject)
            {
                device.Stats.RecordReadProperty(clientAddr);
                device.Stats.RecordError();
                sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY, invokeId,
                    BacnetErrorClasses.ERROR_CLASS_PROPERTY,
                    status == DeviceStorage.ErrorCodes.UnknownProperty
                        ? BacnetErrorCodes.ERROR_CODE_UNKNOWN_PROPERTY
                        : BacnetErrorCodes.ERROR_CODE_OTHER);
                return;
            }
        }

        sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROPERTY, invokeId,
            BacnetErrorClasses.ERROR_CLASS_OBJECT, BacnetErrorCodes.ERROR_CODE_UNKNOWN_OBJECT);
    }

    private void HandleReadPropertyMultiple(BacnetClient sender, BacnetAddress adr, byte invokeId,
        IList<BacnetReadAccessSpecification> properties, BacnetMaxSegments maxSegments)
    {
        SimulatedDevice? targetDevice = null;
        foreach (var device in _devices.Values)
        {
            if (properties.Count > 0 && device.Storage.FindObject(properties[0].objectIdentifier) != null)
            {
                targetDevice = device;
                break;
            }
        }

        if (targetDevice == null)
        {
            sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_READ_PROP_MULTIPLE, invokeId,
                BacnetErrorClasses.ERROR_CLASS_OBJECT, BacnetErrorCodes.ERROR_CODE_UNKNOWN_OBJECT);
            return;
        }

        targetDevice.Stats.RecordReadPropertyMultiple(adr.ToString());

        var values = new List<BacnetReadAccessResult>();
        foreach (var spec in properties)
        {
            var propValues = new List<BacnetPropertyValue>();

            if (spec.propertyReferences == null || spec.propertyReferences.Count == 0 ||
                (spec.propertyReferences.Count == 1 && spec.propertyReferences[0].propertyIdentifier == (uint)BacnetPropertyIds.PROP_ALL))
            {
                if (targetDevice.Storage.ReadPropertyAll(spec.objectIdentifier, out var allValues))
                {
                    values.Add(new BacnetReadAccessResult
                    {
                        objectIdentifier = spec.objectIdentifier,
                        values = allValues
                    });
                    continue;
                }
            }
            else
            {
                foreach (var propRef in spec.propertyReferences)
                {
                    var status = targetDevice.Storage.ReadProperty(spec.objectIdentifier,
                        (BacnetPropertyIds)propRef.propertyIdentifier, propRef.propertyArrayIndex, out var value);

                    if (status == DeviceStorage.ErrorCodes.Good)
                    {
                        propValues.Add(new BacnetPropertyValue
                        {
                            property = propRef,
                            value = value
                        });
                    }
                }
            }

            values.Add(new BacnetReadAccessResult
            {
                objectIdentifier = spec.objectIdentifier,
                values = propValues
            });
        }

        _display.AddActivity($"ReadPropertyMultiple on {targetDevice.Name} ({properties.Count} objects)");
        sender.ReadPropertyMultipleResponse(adr, invokeId, sender.GetSegmentBuffer(maxSegments), values);
    }

    private void HandleWriteProperty(BacnetClient sender, BacnetAddress adr, byte invokeId,
        BacnetObjectId objectId, BacnetPropertyValue value, BacnetMaxSegments maxSegments)
    {
        var clientAddr = adr.ToString();
        foreach (var device in _devices.Values)
        {
            var obj = device.Storage.FindObject(objectId);
            if (obj == null) continue;

            device.Stats.RecordWriteProperty(clientAddr);

            var simObj = device.SimulatedObjects.FirstOrDefault(o => o.ObjectId.Equals(objectId));
            if (simObj != null && !simObj.IsWritable)
            {
                device.Stats.RecordError();
                sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY, invokeId,
                    BacnetErrorClasses.ERROR_CLASS_PROPERTY, BacnetErrorCodes.ERROR_CODE_WRITE_ACCESS_DENIED);
                return;
            }

            var status = device.Storage.WriteProperty(objectId,
                (BacnetPropertyIds)value.property.propertyIdentifier,
                value.property.propertyArrayIndex, value.value);

            if (status == DeviceStorage.ErrorCodes.Good)
            {
                if (simObj != null && (BacnetPropertyIds)value.property.propertyIdentifier == BacnetPropertyIds.PROP_PRESENT_VALUE)
                {
                    simObj.IsOverridden = true;
                    _logger.LogInformation("Object {ObjectId} on device {DeviceId} overridden via WriteProperty",
                        objectId, device.DeviceId);
                    _display.AddActivity($"WriteProperty on {device.Name}/{objectId} → overridden");
                }

                sender.SimpleAckResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY, invokeId);
                return;
            }

            sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY, invokeId,
                BacnetErrorClasses.ERROR_CLASS_PROPERTY,
                status == DeviceStorage.ErrorCodes.WriteAccessDenied
                    ? BacnetErrorCodes.ERROR_CODE_WRITE_ACCESS_DENIED
                    : BacnetErrorCodes.ERROR_CODE_OTHER);
            return;
        }

        sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_WRITE_PROPERTY, invokeId,
            BacnetErrorClasses.ERROR_CLASS_OBJECT, BacnetErrorCodes.ERROR_CODE_UNKNOWN_OBJECT);
    }

    private void HandleSubscribeCov(BacnetClient sender, BacnetAddress adr, byte invokeId,
        uint subscriberProcessIdentifier, BacnetObjectId monitoredObjectIdentifier,
        bool cancellationRequest, bool issueConfirmedNotifications, uint lifetime,
        BacnetMaxSegments maxSegments)
    {
        foreach (var device in _devices.Values)
        {
            if (device.Storage.FindObject(monitoredObjectIdentifier) == null) continue;

            device.Stats.RecordSubscribeCov(adr.ToString());
            _display.AddActivity($"SubscribeCOV from {adr} on {device.Name}/{monitoredObjectIdentifier}");

            if (cancellationRequest)
            {
                _covManager.Unsubscribe(adr, subscriberProcessIdentifier, monitoredObjectIdentifier, device.DeviceId);
            }
            else
            {
                _covManager.Subscribe(adr, subscriberProcessIdentifier, monitoredObjectIdentifier,
                    issueConfirmedNotifications, lifetime, device.DeviceId);
            }

            sender.SimpleAckResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV, invokeId);
            return;
        }

        sender.ErrorResponse(adr, BacnetConfirmedServices.SERVICE_CONFIRMED_SUBSCRIBE_COV, invokeId,
            BacnetErrorClasses.ERROR_CLASS_OBJECT, BacnetErrorCodes.ERROR_CODE_UNKNOWN_OBJECT);
    }

    private void HandleIam(BacnetClient sender, BacnetAddress adr, uint deviceId,
        uint maxAPDU, BacnetSegmentations segmentation, ushort vendorId)
    {
        _logger.LogDebug("Received I-Am from device {DeviceId} at {Address}", deviceId, adr);
    }

    public void SendCovNotifications(uint deviceId, BacnetObjectId objectId, IList<BacnetPropertyValue> values)
    {
        if (_client == null) return;

        var subscriptions = _covManager.GetActiveSubscriptions(deviceId, objectId);
        foreach (var sub in subscriptions)
        {
            try
            {
                var timeRemaining = sub.ExpiresAt == DateTime.MaxValue
                    ? 0u
                    : (uint)Math.Max(0, (sub.ExpiresAt - DateTime.UtcNow).TotalSeconds);

                _client.Notify(sub.Subscriber, sub.ProcessIdentifier, deviceId,
                    objectId, timeRemaining, sub.IssueConfirmedNotifications, values);

                if (_devices.TryGetValue(deviceId, out var notifiedDevice))
                    notifiedDevice.Stats.RecordCovNotificationSent();

                _logger.LogDebug("COV notification sent to {Subscriber} for device {DeviceId}, object {ObjectId}",
                    sub.Subscriber, deviceId, objectId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send COV notification to {Subscriber}", sub.Subscriber);
            }
        }
    }
}
