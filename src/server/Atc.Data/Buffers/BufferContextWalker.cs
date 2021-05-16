using System;
using System.Collections.Generic;
using System.Linq;
using Atc.Data.Buffers.Impl;

namespace Atc.Data.Buffers
{
    public class BufferContextWalker
    {
        private readonly BufferContext _context;
        private readonly Type[] _recordTypes;
        
        public BufferContextWalker(BufferContext context)
        {
            _context = context;
            _recordTypes = _context.RecordTypes.ToArray();
        }

        public BufferWalker GetBuffer(Type recordType)
        {
            return new BufferWalker(_context, _context.GetBuffer(recordType));
        }
        
        public IReadOnlyList<Type> RecordTypes => _recordTypes;

        public class BufferWalker 
        {
            private readonly BufferContext _context;
            private readonly ITypedBuffer _buffer;
            private StructTypeHandler _typeHandler;

            public BufferWalker(BufferContext context, ITypedBuffer buffer)
            {
                _context = context;
                _buffer = buffer;
                _typeHandler = buffer.TypeHandler;
            }

            public RecordWalker GetRecord(int index)
            {
                return new RecordWalker(_context, _buffer, index, _typeHandler);
            }
            
            public BufferContext Context => _context;
            public ITypedBuffer Buffer => _buffer;
            public StructTypeHandler TypeHandler => _typeHandler;
        }

        public unsafe class RecordWalker
        {
            private readonly BufferContext _context;
            private readonly ITypedBuffer _buffer;
            private readonly int _index;
            private readonly int _offset;
            private readonly Memory<byte> _memory;
            private readonly StructTypeHandler _typeHandler;

            public RecordWalker(BufferContext context, ITypedBuffer buffer, int index, StructTypeHandler typeHandler)
            {
                _context = context;
                _buffer = buffer;
                _index = index;
                _memory = buffer.GetRecordMemory(index, out _offset);
                _typeHandler = typeHandler;
            }

            public BufferContext Context => _context;
            public ITypedBuffer Buffer => _buffer;
            public Memory<byte> Memory => _memory;
            public StructTypeHandler TypeHandler => _typeHandler;
        }
    }
}