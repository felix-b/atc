using System;
using System.Threading;

namespace Zero.Latency.Servers
{
    // Sequences replacement of an immutable value in multi-threaded scenarios (e.g. collections like ImmutableList<T>) 
    // When replacing an immutable object, the new object is the result of a transformation applied to the current object: V1 = T(V0)
    // Example of transformation for ImmutableList<T>: newList = oldList.Add(newItem)
    // This class enforces transformations to run sequentially and always use result of the latest transformation. 
    public class WriteLocked<T> where T : class
    {
        private readonly object _syncRoot = new(); 
        private T _immutableValue;

        public WriteLocked(T immutableValue)
        {
            _immutableValue = immutableValue;
        }

        // reads are not controlled
        public T Read() => _immutableValue;

        // transform and replace, then return the new value
        public T Replace(Func<T, T> transform)
        {
            lock (_syncRoot)
            {
                var newValue = transform(_immutableValue);
                Interlocked.Exchange(ref _immutableValue, newValue);
                return newValue;
            }
        }

        // transform and replace, but return the previous value
        public T Exchange(Func<T, T> transform)
        {
            lock (_syncRoot)
            {
                var oldValue = _immutableValue;
                var newValue = transform(oldValue);
                Interlocked.Exchange(ref _immutableValue, newValue);
                return oldValue;
            }
        }

        public static implicit operator WriteLocked<T>(T value)
        {
            return new WriteLocked<T>(value);
        }
    }
}
