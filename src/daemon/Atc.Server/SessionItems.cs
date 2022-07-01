namespace Atc.Server;

public struct SessionItems
{
    private readonly Dictionary<Type, object?> _entries;

    public SessionItems(int initialEntryCount)
    {
        _entries = new Dictionary<Type, object?>(initialEntryCount);
    }

    public bool Has<T>() where T : class
    {
        return _entries.ContainsKey(typeof(T));
    }

    public bool Add<T>() where T : class
    {
        if (_entries.ContainsKey(typeof(T)))
        {
            return false;
        }
        _entries[typeof(T)] = null;
        return true;
    }

    public bool TryGet<T>(out T? item) where T : class
    {
        if (_entries.TryGetValue(typeof(T), out var untypedItem))
        {
            item = (T?)untypedItem;
            return true;
        }

        item = default;
        return false;
    }

    public T? Get<T>() where T : class
    {
        return (T?)_entries[typeof(T)];
    }

    public T? GetOrAdd<T>(Func<T?> factory) where T : class
    {
        if (_entries.TryGetValue(typeof(T), out var untypedItem))
        {
            return (T?)untypedItem;
        }

        var newItem = factory();
        _entries.Add(typeof(T), newItem);
        return newItem;
    }
        
    public void Set<T>(T? item) where T : class
    {
        _entries[typeof(T)] = item;
    }

    public IDictionary<Type, object?> Entries => _entries;
}