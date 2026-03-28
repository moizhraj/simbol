namespace Simbol.Engine.Tests;

using Simbol.Engine.Models;

public class RateHistoryBufferTests
{
    [Fact]
    public void Add_StoresDataPoint()
    {
        var buffer = new RateHistoryBuffer();
        buffer.Add(42.0);

        Assert.Equal(1, buffer.Count);
        var points = buffer.GetAllPoints();
        Assert.Single(points);
        Assert.Equal(42.0, points[0].Rate);
    }

    [Fact]
    public void GetPoints_FiltersByWindow()
    {
        var buffer = new RateHistoryBuffer();
        buffer.Add(10.0);
        buffer.Add(20.0);
        buffer.Add(30.0);

        // All 3 should be within the last 5 minutes
        var points = buffer.GetPoints(TimeSpan.FromMinutes(5));
        Assert.Equal(3, points.Length);
    }

    [Fact]
    public void GetAllPoints_ReturnsEverything()
    {
        var buffer = new RateHistoryBuffer();
        for (int i = 0; i < 100; i++)
            buffer.Add(i);

        Assert.Equal(100, buffer.GetAllPoints().Length);
    }

    [Fact]
    public void Add_TrimsOldData()
    {
        // Use a very short retention for testing
        var buffer = new RateHistoryBuffer(TimeSpan.FromMilliseconds(100));
        buffer.Add(1.0);
        Thread.Sleep(150);
        buffer.Add(2.0);

        // Old point should be trimmed
        Assert.Equal(1, buffer.Count);
        Assert.Equal(2.0, buffer.GetAllPoints()[0].Rate);
    }

    [Fact]
    public void DeviceStats_RecordsHistory()
    {
        var stats = new DeviceStats();
        stats.RecordReadProperty("client1");
        stats.RecordReadProperty("client1");
        stats.ResetWindow(5);

        Assert.Equal(1, stats.RateHistory.Count);
        var points = stats.RateHistory.GetAllPoints();
        // instant rate = 2 requests / 5 seconds * 60 = 24 req/min
        Assert.Equal(24.0, points[0].Rate, 0.1);
    }
}
