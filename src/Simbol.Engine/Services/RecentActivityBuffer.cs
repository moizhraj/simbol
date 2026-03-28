namespace Simbol.Engine.Services;

public class RecentActivityBuffer
{
    private readonly string[] _items;
    private int _head;
    private int _count;
    private readonly object _lock = new();

    public RecentActivityBuffer(int capacity = 8)
    {
        _items = new string[capacity];
    }

    public void Add(string message)
    {
        lock (_lock)
        {
            _items[_head] = message;
            _head = (_head + 1) % _items.Length;
            if (_count < _items.Length) _count++;
        }
    }

    public string[] GetItems()
    {
        lock (_lock)
        {
            var result = new string[_count];
            for (int i = 0; i < _count; i++)
            {
                var index = (_head - _count + i + _items.Length) % _items.Length;
                result[i] = _items[index];
            }
            return result;
        }
    }
}
