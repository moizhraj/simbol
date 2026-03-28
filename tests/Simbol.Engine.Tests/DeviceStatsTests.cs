namespace Simbol.Engine.Tests;

using Simbol.Engine.Models;

public class DeviceStatsTests
{
    [Fact]
    public void RecordReadProperty_IncrementsCount()
    {
        var stats = new DeviceStats();
        stats.RecordReadProperty("192.168.1.1:47808");
        stats.RecordReadProperty("192.168.1.1:47808");

        Assert.Equal(2, stats.ReadPropertyCount);
        Assert.Equal(2, stats.TotalRequestCount);
    }

    [Fact]
    public void RecordReadPropertyMultiple_IncrementsCount()
    {
        var stats = new DeviceStats();
        stats.RecordReadPropertyMultiple("192.168.1.1:47808");

        Assert.Equal(1, stats.ReadPropertyMultipleCount);
        Assert.Equal(1, stats.TotalRequestCount);
    }

    [Fact]
    public void RecordWriteProperty_IncrementsCount()
    {
        var stats = new DeviceStats();
        stats.RecordWriteProperty("192.168.1.1:47808");

        Assert.Equal(1, stats.WritePropertyCount);
    }

    [Fact]
    public void RecordSubscribeCov_IncrementsCount()
    {
        var stats = new DeviceStats();
        stats.RecordSubscribeCov("192.168.1.1:47808");

        Assert.Equal(1, stats.SubscribeCovCount);
    }

    [Fact]
    public void RecordWhoIs_IncrementsCount()
    {
        var stats = new DeviceStats();
        stats.RecordWhoIs("192.168.1.1:47808");

        Assert.Equal(1, stats.WhoIsCount);
    }

    [Fact]
    public void RecordCovNotificationSent_IncrementsCount()
    {
        var stats = new DeviceStats();
        stats.RecordCovNotificationSent();
        stats.RecordCovNotificationSent();

        Assert.Equal(2, stats.CovNotificationsSentCount);
    }

    [Fact]
    public void RecordError_IncrementsCount()
    {
        var stats = new DeviceStats();
        stats.RecordError();

        Assert.Equal(1, stats.ErrorCount);
    }

    [Fact]
    public void TotalRequestCount_SumsAllRequestTypes()
    {
        var stats = new DeviceStats();
        stats.RecordReadProperty("a");
        stats.RecordReadPropertyMultiple("b");
        stats.RecordWriteProperty("c");
        stats.RecordSubscribeCov("d");
        stats.RecordWhoIs("e");

        Assert.Equal(5, stats.TotalRequestCount);
    }

    [Fact]
    public void UniqueClients_TracksDifferentAddresses()
    {
        var stats = new DeviceStats();
        stats.RecordReadProperty("192.168.1.1:47808");
        stats.RecordReadProperty("192.168.1.1:47808"); // duplicate
        stats.RecordReadProperty("192.168.1.2:47808");
        stats.RecordWriteProperty("10.0.0.1:47808");

        Assert.Equal(3, stats.UniqueClientCount);
    }

    [Fact]
    public void LastRequestTime_UpdatesOnRequest()
    {
        var stats = new DeviceStats();
        var before = DateTime.UtcNow;

        stats.RecordReadProperty("192.168.1.1:47808");

        Assert.True(stats.LastRequestTime >= before);
    }

    [Fact]
    public void ResetWindow_ReturnsRateAndResets()
    {
        var stats = new DeviceStats();
        stats.RecordReadProperty("a");
        stats.RecordReadProperty("a");
        stats.RecordReadProperty("a");

        // 3 requests in a 30 second window = 6 req/min
        var rate = stats.ResetWindow(30);
        Assert.Equal(6.0, rate, 0.01);

        // Window counter is now reset
        Assert.Equal(0, stats.RequestsInWindow);

        // Total is still 3
        Assert.Equal(3, stats.TotalRequestCount);
    }

    [Fact]
    public void PeakRequestsPerMinute_TracksHighWaterMark()
    {
        var stats = new DeviceStats();

        // Window 1: 6 req/min
        stats.RecordReadProperty("a");
        stats.RecordReadProperty("a");
        stats.RecordReadProperty("a");
        stats.ResetWindow(30);

        // Window 2: 12 req/min (new peak)
        for (int i = 0; i < 6; i++) stats.RecordReadProperty("a");
        stats.ResetWindow(30);

        // Window 3: 2 req/min (lower, peak unchanged)
        stats.RecordReadProperty("a");
        stats.ResetWindow(30);

        Assert.Equal(12.0, stats.PeakRequestsPerMinute, 0.01);
    }

    [Fact]
    public void CovNotifications_NotCountedInTotalRequests()
    {
        var stats = new DeviceStats();
        stats.RecordCovNotificationSent();
        stats.RecordCovNotificationSent();

        // COV notifications are outbound, not inbound requests
        Assert.Equal(0, stats.TotalRequestCount);
        Assert.Equal(2, stats.CovNotificationsSentCount);
    }
}
