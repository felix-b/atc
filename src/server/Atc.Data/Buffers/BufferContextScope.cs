using System;
using System.Threading;
using Atc.Data.Buffers.Impl;

namespace Atc.Data.Buffers
{
    public class BufferContextScope : IDisposable
    {
        private readonly IBufferContext _context;
        private readonly BufferContextScope? _previousValue;
        
        public BufferContextScope(IBufferContext context)
        {
            _context = context;
            _previousValue = _current.Value;
            _current.Value = this;
        }

        void IDisposable.Dispose()
        {
            _current.Value = _previousValue;
        }

        public TypedBuffer<T> GetBuffer<T>() where T : struct
        {
            return _context.GetBuffer<T>();
        }

        public IBufferContext Context => _context;
        
        private static readonly AsyncLocal<BufferContextScope?> _current = new();

        public static bool HasCurrent => _current.Value != null; 
        
        public static IBufferContext CurrentContext => 
            _current.Value?.Context 
            ?? throw new InvalidOperationException("No BufferContext exists in the current scope");
    }
}
