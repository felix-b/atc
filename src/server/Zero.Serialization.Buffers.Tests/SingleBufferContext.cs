using System;
using System.IO;
using Zero.Serialization.Buffers.Impl;

namespace Zero.Serialization.Buffers.Tests
{
    public class SingleBufferContext<R> : IBufferContext, IDisposable 
        where R : struct
    {
        private readonly IDisposable _scope;
        public readonly TypedBuffer<R> Buffer;

        public SingleBufferContext(int capacity = 10)
        {
            _scope = new BufferContextScope(this);
            Buffer = new TypedBuffer<R>(capacity);
        }

        public SingleBufferContext(Stream input)
        {
            _scope = new BufferContextScope(this);
            Buffer = TypedBuffer.CreateFromStreamOf<R>(input);
        }
        
        public TypedBuffer<T> GetBuffer<T>() where T : struct
        {
            if (typeof(T) != typeof(R))
            {
                throw new InvalidOperationException($"Record type mismatch! Expected '{typeof(R)}', got '{typeof(T)}'.");
            }

            return (TypedBuffer<T>)(object)Buffer;
        }

        public BufferContextWalker GetWalker()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _scope.Dispose();
        }
    }
}