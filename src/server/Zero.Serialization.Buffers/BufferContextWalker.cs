using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Zero.Serialization.Buffers.Impl;

namespace Zero.Serialization.Buffers
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

        public BufferWalker GetBuffer<T>() where T : struct
        {
            return GetBuffer(typeof(T));
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
            public Type RecordType => _buffer.RecordType;
            public StructTypeHandler TypeHandler => _typeHandler;
            public int RecordCount => _buffer.RecordCount;
        }

        public unsafe class RecordWalker
        {
            private readonly BufferContext _context;
            private readonly ITypedBuffer _buffer;
            private readonly int _index;
            private readonly int _offset;
            private readonly StructTypeHandler _typeHandler;

            public RecordWalker(BufferContext context, ITypedBuffer buffer, int index, StructTypeHandler typeHandler)
            {
                _context = context;
                _buffer = buffer;
                _index = index;
                _offset = buffer.RecordOffsets[index];
                _typeHandler = typeHandler;
            }

            public StructTypeHandler.FieldValuePair[] GetFieldValues()
            {
                ref var bytesRef = ref _buffer.GetRawBytesRef(_offset);
                void* pRecord = Unsafe.AsPointer(ref bytesRef);
                return _typeHandler.GetFieldValues(pRecord);
            }

            public int GetByteSize()
            {
                ref var bytesRef = ref _buffer.GetRawBytesRef(_offset);
                void* pRecord = Unsafe.AsPointer(ref bytesRef);
                return _typeHandler.GetInstanceSize(pRecord);
            }

            public IntPtr GetAbsoluteAddress()
            {
                ref var bytesRef = ref _buffer.GetRawBytesRef(_offset);
                void* pRecord = Unsafe.AsPointer(ref bytesRef);
                return new IntPtr(pRecord);
            }
            
            public BufferContext Context => _context;
            public ITypedBuffer Buffer => _buffer;
            public int Index => _index;
            public int Offset => _offset;
            public StructTypeHandler TypeHandler => _typeHandler;
        }
    }
}