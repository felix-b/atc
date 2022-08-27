using System.Collections.Immutable;
using Atc.Utilities;

namespace Atc.Server;

public struct SessionItems
{
    private readonly WriteLocked<ImmutableDictionary<Type, object?>> _entries;

    public SessionItems(int initialEntryCount)
    {
        _entries = new WriteLocked<ImmutableDictionary<Type, object?>>(ImmutableDictionary<Type, object?>.Empty);
    }

    public bool Has<T>() where T : class
    {
        return _entries.Read().ContainsKey(typeof(T));
    }

    public bool TryGet<T>(out T? item) where T : class
    {
        if (_entries.Read().TryGetValue(typeof(T), out var untypedItem))
        {
            item = (T?)untypedItem;
            return true;
        }

        item = default;
        return false;
    }

    public T? Get<T>() where T : class
    {
        return (T?)_entries.Read()[typeof(T)];
    }

    public T? GetOrAdd<T>(Func<T?> factory) where T : class
    {
        var newDictionary = _entries.Replace(dictionary => {
            return dictionary.ContainsKey(typeof(T))
                ? dictionary
                : dictionary.Add(typeof(T), factory());
        });
        
        return (T?)newDictionary[typeof(T)];
    }
        
    public void Set<T>(T? item) where T : class
    {
        _entries.Replace(dictionary => dictionary.Add(typeof(T), item));
    }

    public bool Remove<T>() where T : class
    {
        var keyFound = true;
        _entries.Replace(dictionary => {
            keyFound = dictionary.ContainsKey(typeof(T));
            return keyFound
                ? dictionary.Remove(typeof(T))
                : dictionary;
        });
        return keyFound;
    }

    public IReadOnlyDictionary<Type, object?> Entries => _entries.Read();
}
