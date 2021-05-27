using System;
using System.Threading;

namespace Zero.Latency.Servers
{
    // Sequences replacement of immutable collections in multi-threaded scenarios
    public class WriteLocked<T> where T : class
    {
        private readonly object _syncRoot = new(); 
        private T _immutableValue;

        public WriteLocked(T immutableValue)
        {
            _immutableValue = immutableValue;
        }

        public T Read() => _immutableValue;

        public T Replace(Func<T, T> transform)
        {
            lock (_syncRoot)
            {
                var newValue = transform(_immutableValue);
                Interlocked.Exchange(ref _immutableValue, newValue);
                return newValue;
            }
        }

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
