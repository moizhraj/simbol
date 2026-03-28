namespace Simbol.Engine.Models;

public class RateHistoryBuffer
{
    private readonly List<(DateTime Timestamp, double Rate)> _points = new();
    private readonly object _lock = new();
    private readonly TimeSpan _maxRetention;

    public RateHistoryBuffer(TimeSpan? maxRetention = null)
    {
        _maxRetention = maxRetention ?? TimeSpan.FromHours(24);
    }

    public void Add(double rate)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            _points.Add((now, rate));
            // Trim old data beyond retention
            var cutoff = now - _maxRetention;
            _points.RemoveAll(p => p.Timestamp < cutoff);
        }
    }

    public (DateTime Timestamp, double Rate)[] GetPoints(TimeSpan window)
    {
        lock (_lock)
        {
            var cutoff = DateTime.UtcNow - window;
            return _points.Where(p => p.Timestamp >= cutoff).ToArray();
        }
    }

    public (DateTime Timestamp, double Rate)[] GetAllPoints()
    {
        lock (_lock)
        {
            return _points.ToArray();
        }
    }

    public int Count
    {
        get { lock (_lock) { return _points.Count; } }
    }
}
