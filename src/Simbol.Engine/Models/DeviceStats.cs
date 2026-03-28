namespace Simbol.Engine.Models;

using System.Collections.Concurrent;

/// <summary>
/// Thread-safe per-device statistics tracking request load from BACnet clients.
/// </summary>
public class DeviceStats
{
    private long _readPropertyCount;
    private long _readPropertyMultipleCount;
    private long _writePropertyCount;
    private long _subscribeCovCount;
    private long _whoIsCount;
    private long _covNotificationsSentCount;
    private long _errorCount;
    private long _requestsInWindow;
    private double _peakRequestsPerMinute;

    private readonly ConcurrentDictionary<string, byte> _uniqueClients = new();

    public long ReadPropertyCount => Interlocked.Read(ref _readPropertyCount);
    public long ReadPropertyMultipleCount => Interlocked.Read(ref _readPropertyMultipleCount);
    public long WritePropertyCount => Interlocked.Read(ref _writePropertyCount);
    public long SubscribeCovCount => Interlocked.Read(ref _subscribeCovCount);
    public long WhoIsCount => Interlocked.Read(ref _whoIsCount);
    public long CovNotificationsSentCount => Interlocked.Read(ref _covNotificationsSentCount);
    public long ErrorCount => Interlocked.Read(ref _errorCount);
    public long RequestsInWindow => Interlocked.Read(ref _requestsInWindow);
    public double PeakRequestsPerMinute => _peakRequestsPerMinute;
    public DateTime LastRequestTime { get; private set; }
    public int UniqueClientCount => _uniqueClients.Count;

    public long TotalRequestCount =>
        ReadPropertyCount + ReadPropertyMultipleCount + WritePropertyCount +
        SubscribeCovCount + WhoIsCount;

    public void RecordReadProperty(string clientAddress)
    {
        Interlocked.Increment(ref _readPropertyCount);
        Interlocked.Increment(ref _requestsInWindow);
        TrackClient(clientAddress);
    }

    public void RecordReadPropertyMultiple(string clientAddress)
    {
        Interlocked.Increment(ref _readPropertyMultipleCount);
        Interlocked.Increment(ref _requestsInWindow);
        TrackClient(clientAddress);
    }

    public void RecordWriteProperty(string clientAddress)
    {
        Interlocked.Increment(ref _writePropertyCount);
        Interlocked.Increment(ref _requestsInWindow);
        TrackClient(clientAddress);
    }

    public void RecordSubscribeCov(string clientAddress)
    {
        Interlocked.Increment(ref _subscribeCovCount);
        Interlocked.Increment(ref _requestsInWindow);
        TrackClient(clientAddress);
    }

    public void RecordWhoIs(string clientAddress)
    {
        Interlocked.Increment(ref _whoIsCount);
        Interlocked.Increment(ref _requestsInWindow);
        TrackClient(clientAddress);
    }

    public void RecordCovNotificationSent()
    {
        Interlocked.Increment(ref _covNotificationsSentCount);
    }

    public void RecordError()
    {
        Interlocked.Increment(ref _errorCount);
    }

    /// <summary>
    /// Computes requests/minute from the current window, updates peak, and resets the window counter.
    /// Returns the computed rate.
    /// </summary>
    public double ResetWindow(double windowSeconds)
    {
        var count = Interlocked.Exchange(ref _requestsInWindow, 0);
        var rate = windowSeconds > 0 ? count / windowSeconds * 60.0 : 0;

        if (rate > _peakRequestsPerMinute)
            _peakRequestsPerMinute = rate;

        return rate;
    }

    private void TrackClient(string clientAddress)
    {
        LastRequestTime = DateTime.UtcNow;
        _uniqueClients.TryAdd(clientAddress, 0);
    }
}
