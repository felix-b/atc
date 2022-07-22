namespace Atc.Telemetry.CodePath;

public class CodePathStringMap 
{
    private readonly object _syncRoot = new();
    private Dictionary<string, Int32> _keyByString = new();
    private int _lastStringKey = 0;

    public Int32 GetStringKey(string s, out bool createdNew)
    {
        if (_keyByString.TryGetValue(s, out var existingKey))
        {
            createdNew = false;
            return existingKey;
        }

        if (!Monitor.TryEnter(_syncRoot, 500))
        {
            Console.WriteLine("CodePathStringMap: add string key failed: sync root timeout");
            createdNew = false;
            return -1;
        }

        try 
        {
            if (_keyByString.TryGetValue(s, out existingKey))
            {
                createdNew = false;
                return existingKey;
            }

            var newKey = ++_lastStringKey;
            _keyByString.Add(s, newKey);

            createdNew = true;
            return newKey;
        }
        catch (Exception e)
        {
            Console.WriteLine($"CodePathStringMap: add string key failed: {e}");
            createdNew = false;
            return -2;
        }
        finally
        {
            Monitor.Exit(_syncRoot);
        }
    }

    public IReadOnlyDictionary<string, int> TakeSnapshot()
    {
        if (!Monitor.TryEnter(_syncRoot, 500))
        {
            Console.WriteLine("CodePathStringMap: take snapshot failed: sync root timeout");
            return new Dictionary<string, int>();
        }

        try
        {
            var snapshot = new Dictionary<string, int>(_keyByString);
            return snapshot;
        }
        catch (Exception e)
        {
            Console.WriteLine($"CodePathStringMap: take snapshot failed: {e}");
            return new Dictionary<string, int>();
        }
        finally
        {
            Monitor.Exit(_syncRoot);
        }
    }

    public void WriteAllEntries(BinaryWriter writer)
    {
        var snapshot = TakeSnapshot();
        foreach (var entry in snapshot)
        {
            WriteEntry(writer, key: entry.Value, value: entry.Key);
        }
    }

    public void WriteEntry(BinaryWriter writer, int key, string value)
    {
        writer.Write((byte)LogStreamOpCode.StringKey);
        writer.Write(key);
        writer.Write(value);
    }
}
