namespace Simbol.Engine.Services;

using System.IO.BACnet;
using Microsoft.Extensions.Logging;

public class CovSubscription
{
    public required BacnetAddress Subscriber { get; init; }
    public uint ProcessIdentifier { get; init; }
    public BacnetObjectId MonitoredObject { get; init; }
    public bool IssueConfirmedNotifications { get; init; }
    public DateTime ExpiresAt { get; init; }
    public uint DeviceId { get; init; }
    public double LastNotifiedAnalogValue { get; set; }
    public bool LastNotifiedBinaryValue { get; set; }
    public uint LastNotifiedMultiStateValue { get; set; }
}

public class CovManager
{
    private readonly List<CovSubscription> _subscriptions = new();
    private readonly object _lock = new();
    private readonly ILogger<CovManager> _logger;

    public CovManager(ILogger<CovManager> logger)
    {
        _logger = logger;
    }

    public void Subscribe(BacnetAddress subscriber, uint processId, BacnetObjectId objectId,
        bool issueConfirmed, uint lifetime, uint deviceId)
    {
        lock (_lock)
        {
            // Remove existing subscription for same subscriber + object
            _subscriptions.RemoveAll(s =>
                s.Subscriber.Equals(subscriber) &&
                s.ProcessIdentifier == processId &&
                s.MonitoredObject.Equals(objectId) &&
                s.DeviceId == deviceId);

            var sub = new CovSubscription
            {
                Subscriber = subscriber,
                ProcessIdentifier = processId,
                MonitoredObject = objectId,
                IssueConfirmedNotifications = issueConfirmed,
                ExpiresAt = lifetime == 0 ? DateTime.MaxValue : DateTime.UtcNow.AddSeconds(lifetime),
                DeviceId = deviceId
            };

            _subscriptions.Add(sub);
            _logger.LogInformation("COV subscription added: Device {DeviceId}, Object {ObjectId}, Subscriber {Subscriber}, Lifetime {Lifetime}s",
                deviceId, objectId, subscriber, lifetime);
        }
    }

    public void Unsubscribe(BacnetAddress subscriber, uint processId, BacnetObjectId objectId, uint deviceId)
    {
        lock (_lock)
        {
            var removed = _subscriptions.RemoveAll(s =>
                s.Subscriber.Equals(subscriber) &&
                s.ProcessIdentifier == processId &&
                s.MonitoredObject.Equals(objectId) &&
                s.DeviceId == deviceId);

            if (removed > 0)
                _logger.LogInformation("COV subscription removed: Device {DeviceId}, Object {ObjectId}", deviceId, objectId);
        }
    }

    public List<CovSubscription> GetActiveSubscriptions(uint deviceId, BacnetObjectId objectId)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            _subscriptions.RemoveAll(s => s.ExpiresAt < now);

            return _subscriptions
                .Where(s => s.DeviceId == deviceId && s.MonitoredObject.Equals(objectId))
                .ToList();
        }
    }

    public List<CovSubscription> GetAllActiveSubscriptions()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            _subscriptions.RemoveAll(s => s.ExpiresAt < now);
            return _subscriptions.ToList();
        }
    }
}
