using System;
using System.Threading;
using Zero.Serialization.Buffers.Impl;

namespace Zero.Serialization.Buffers
{
    public class BufferContextScope : IDisposable
    {
        private readonly IBufferContext _context;
        private readonly BufferContextScope? _previousValue;
        
        public BufferContextScope(IBufferContext context)
        {
            _context = context;
            _previousValue = GetScopeProvider().Current;
            GetScopeProvider().Current = this;
        }

        void IDisposable.Dispose()
        {
            GetScopeProvider().Current = _previousValue;
        }

        public TypedBuffer<T> GetBuffer<T>() where T : struct
        {
            return _context.GetBuffer<T>();
        }

        public IBufferContext Context => _context;

        private static IScopeProvider? _scopeProvider = null;

        public static void UseAsyncLocalScope()
        {
            if (_scopeProvider == null)
            {
                _scopeProvider = new AsyncLocalScopeProvider();
            }
            else if (!(_scopeProvider is AsyncLocalScopeProvider))
            {
                throw new InvalidOperationException("BufferContextScope was already initialized with a different provider type");
            }
        }

        public static void UseStaticScope()
        {
            if (_scopeProvider == null)
            {
                _scopeProvider = new StaticScopeProvider();
            }
            else if (!(_scopeProvider is StaticScopeProvider))
            {
                throw new InvalidOperationException("BufferContextScope was already initialized with a different provider type");
            }
        }

        public static bool HasCurrent => GetScopeProvider().Current != null; 
        
        public static IBufferContext CurrentContext => 
            GetScopeProvider().Current?.Context 
            ?? throw new InvalidOperationException("No BufferContext exists in the current scope");

        private static IScopeProvider GetScopeProvider()
        {
            return 
                _scopeProvider 
                ?? throw new InvalidOperationException("BufferContextScope was not initialized with UseStaticScope()/UseAsyncLocalScope()"
            );
        }
        
        private interface IScopeProvider
        {
            BufferContextScope? Current { get; set; }
        }

        private class AsyncLocalScopeProvider : IScopeProvider
        {
            private static readonly AsyncLocal<BufferContextScope?> _current = new();

            public BufferContextScope? Current
            {
                get
                {
                    return _current.Value;
                }
                set
                {
                    _current.Value = value;
                }
            }
        }

        private class StaticScopeProvider : IScopeProvider
        {
            private static BufferContextScope? _current = null;

            public BufferContextScope? Current
            {
                get
                {
                    return _current;
                }
                set
                {
                    _current = value;
                }
            }
        }
    }
}
