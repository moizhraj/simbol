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

    // Rolling 1-minute peak: store instant rates from recent windows
    private readonly Queue<double> _recentRates = new();
    private double _rollingPeakPerMinute;

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
    public double RollingPeakPerMinute => _rollingPeakPerMinute;
    public double CurrentRequestsPerMinute { get; private set; }
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
    /// Computes requests/minute from the current window using an exponential moving average
    /// so the rate decays smoothly instead of snapping to 0 between bursts.
    /// Also tracks a 1-minute rolling peak from recent windows.
    /// Should only be called once per stats interval (not on every display refresh).
    /// </summary>
    public double ResetWindow(double windowSeconds)
    {
        var count = Interlocked.Exchange(ref _requestsInWindow, 0);
        var instantRate = windowSeconds > 0 ? count / windowSeconds * 60.0 : 0;

        // EMA smoothing: blend new rate with previous (alpha=0.4 gives ~3-window decay)
        const double alpha = 0.4;
        CurrentRequestsPerMinute = alpha * instantRate + (1.0 - alpha) * CurrentRequestsPerMinute;

        // Round very small values to 0 to avoid showing 0.1 forever
        if (CurrentRequestsPerMinute < 0.5)
            CurrentRequestsPerMinute = 0;

        // All-time peak
        if (instantRate > _peakRequestsPerMinute)
            _peakRequestsPerMinute = instantRate;

        // Rolling 1-minute peak: keep last N windows where N = 60/windowSeconds
        var maxWindows = Math.Max(1, (int)(60.0 / windowSeconds));
        _recentRates.Enqueue(instantRate);
        while (_recentRates.Count > maxWindows)
            _recentRates.Dequeue();
        _rollingPeakPerMinute = _recentRates.Count > 0 ? _recentRates.Max() : 0;

        return CurrentRequestsPerMinute;
    }

    private void TrackClient(string clientAddress)
    {
        LastRequestTime = DateTime.UtcNow;
        _uniqueClients.TryAdd(clientAddress, 0);
    }
}
