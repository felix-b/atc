 using System;
using System.Threading;

namespace Atc.Data
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

        private static readonly AsyncLocal<BufferContextScope?> _current = new();

        public static bool HasCurrent => _current.Value != null; 
        
        public static BufferContextScope CurrentContext => 
            _current.Value 
            ?? throw new InvalidOperationException("No BufferContext exists in the current scope");
    }
}
