using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Zero.Serialization.Buffers.Impl
{
    public unsafe struct VectorRecord<T> : IVariableSizeRecord, IEnumerable<ZCursor<T>>
        where T : unmanaged
    {
        private IntPtr _thisPtr; //TODO: remove when stabilized
        
        #pragma warning disable 0649
        [InjectSelfByteIndex]
        private int _selfByteIndex;
        #pragma warning restore 0649

        private int _entrySize;
        private int _vectorItemCount;
        private int _blockEntryCount;
        private int _blockAllocatedEntryCount;
        private int? _nextBlockPtr;
        private int? _tailBlockPtr;
        private fixed byte _entries[1];

        public IEnumerator<ZCursor<T>> GetEnumerator()
        {
            return new ZCursorEnumerator(BufferContext.Current.GetBuffer(this.GetType()), ref this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ZCursorEnumerator(BufferContext.Current.GetBuffer(this.GetType()), ref this);
        }

        public int SizeOf()
        {
            return SizeOf(_blockEntryCount);
        }

        public void Add(T item)
        {
            Add(ref item);
        }

        public void Add(ref T item)
        {
            //TODO: remove when stabilized
            fixed (void* p = &_vectorItemCount)
            {
                if (_thisPtr != new IntPtr(p))
                {
                    //Console.WriteLine("VectorRecord memory address changed!!!"); //TODO
                    //_thisPtr = new IntPtr(p);
                    //throw new InvalidOperationException("VectorRecord memory location changed!!!");
                }
            }

            if (!_tailBlockPtr.HasValue && _blockAllocatedEntryCount >= _blockEntryCount)
            {
                AllocateNextBlock();
            }
            
            if (_tailBlockPtr.HasValue)
            {
                ref var tailBlock = ref new ZRef<VectorRecord<T>>(_tailBlockPtr.Value).Get();
                tailBlock.Add(ref item);
                if (tailBlock._tailBlockPtr.HasValue)
                {
                    _tailBlockPtr = tailBlock._tailBlockPtr;
                }
            }
            else
            {
                T* entryPtr = GetEntryPointer(entryIndex: _blockAllocatedEntryCount);
                *entryPtr = item;
                _blockAllocatedEntryCount++;
            }

            _vectorItemCount++;
        }

        public ref T this[int index]
        {
            get
            {
                if (index < 0 || (index >= _blockAllocatedEntryCount && !_nextBlockPtr.HasValue))
                {
                    throw new IndexOutOfRangeException();
                }
                
                if (index < _blockAllocatedEntryCount)
                {
                    T* entryPtr = GetEntryPointer(index);
                    return ref Unsafe.AsRef<T>(entryPtr);
                }

                if (_nextBlockPtr.HasValue)
                {
                    return ref GetNextBlock()[index - _blockAllocatedEntryCount];
                }

                throw new InvalidDataException("Vector record is corrupt.");
            }
        }

        public int Count => _vectorItemCount;

        internal ZRef<VectorRecord<T>>? NextBlock =>
            _nextBlockPtr.HasValue
                ? new ZRef<VectorRecord<T>>(_nextBlockPtr.Value)
                : null;

        internal int BlockEntryCount => _blockEntryCount;
        
        internal int BlockAllocatedEntryCount => _blockAllocatedEntryCount;

        private void Initialize(int blockEntryCount)
        {
            fixed (void* p = &_vectorItemCount)
            {
                _thisPtr = new IntPtr(p);
            }

            _entrySize = Unsafe.SizeOf<T>();
            _vectorItemCount = 0;
            _blockEntryCount = blockEntryCount;
            _blockAllocatedEntryCount = 0;
            _nextBlockPtr = null;
            _tailBlockPtr = null;
            
            for (int i = 0; i < _blockEntryCount; i++)
            {
                _entries[i] = 0xFF;
            }
        }

        private T* GetEntryPointer(int entryIndex)
        {
            fixed (byte* entryFirstBytePtr = &_entries[entryIndex * _entrySize])
            {
                return (T*) entryFirstBytePtr;
            }
        }
        
        private ref VectorRecord<T> GetNextBlock()
        {
            if (_nextBlockPtr.HasValue)
            {
                var ptr = new ZRef<VectorRecord<T>>(_nextBlockPtr.Value);
                return ref ptr.Get();
            }

            throw new NullReferenceException("Cannot get next block, it is a null pointer");
        }

        private void AllocateNextBlock()
        {
            var maxRecordSize = BufferContext.Current.GetBuffer(this.GetType()).MaxRecordSizeBytes;
            var maxBlockEntryCount = (maxRecordSize - _baseSize) / _entrySize;
            var nextBlockEntryCount = Math.Min(_blockEntryCount * 2, maxBlockEntryCount);

            var nextBlockByteSize = SizeOf(nextBlockEntryCount);
            var nextBlockPtr = BufferContext.Current.GetBuffer<VectorRecord<T>>().Allocate(nextBlockByteSize);
            ref var nextBlockRecord = ref nextBlockPtr.Get();
            nextBlockRecord.Initialize(nextBlockEntryCount);

            _nextBlockPtr = nextBlockPtr.ByteIndex;
            _tailBlockPtr = _nextBlockPtr;
        }

        private static readonly int _baseSize = Unsafe.SizeOf<VectorRecord<byte>>();

        public static ZRef<VectorRecord<T>> Allocate(Span<T> items, int minBlockEntryCount, IBufferContext? context = null)
        {
            var blockEntryCount = Math.Max(minBlockEntryCount, items.Length);
            var effectiveContext = context ?? BufferContext.Current; 
            var byteSize = SizeOf(blockEntryCount);

            var ptr = effectiveContext.GetBuffer<VectorRecord<T>>().Allocate(byteSize);
            ref var vector = ref ptr.Get();
            vector.Initialize(blockEntryCount);

            for (int i = 0; i < items.Length; i++)
            {
                vector.Add(ref items[i]);
            }
            
            return ptr;
        }

        public static int SizeOf(int blockEntryCount)
        {
            return _baseSize - sizeof(byte) + blockEntryCount * Unsafe.SizeOf<T>();
        }

        private class ZCursorEnumerator : IEnumerator<ZCursor<T>>
        {
            private readonly ITypedBuffer _buffer;
            private readonly int _firstRecordByteIndex;
            private readonly int _entryArrayOffset;
            private int _recordByteIndex;
            private int _indexInRecord;
            private bool _hasCurrent;
            
            public ZCursorEnumerator(ITypedBuffer buffer, ref VectorRecord<T> record)
            {
                _buffer = buffer;
                _firstRecordByteIndex = record._selfByteIndex;
                _entryArrayOffset = _buffer.TypeHandler.VariableBufferField!.Offset;
                Reset();
            }
            
            public bool MoveNext()
            {
                ref var record = ref GetCurrentRecord();
                _indexInRecord++;

                if (_indexInRecord < record._blockAllocatedEntryCount)
                {
                    _hasCurrent = true;
                }
                else
                {
                    if (record._nextBlockPtr.HasValue)
                    {
                        _recordByteIndex = record._nextBlockPtr.Value;
                        _indexInRecord = 0;
                        _hasCurrent = true;
                    }
                    else
                    {
                        _hasCurrent = false;
                    }
                }

                return _hasCurrent;
            }

            public void Reset()
            {
                _recordByteIndex = _firstRecordByteIndex;
                _indexInRecord = -1;
                _hasCurrent = false;
            }

            public ZCursor<T> Current
            {
                get
                {
                    if (_hasCurrent)
                    {
                        var bufferByteIndex = 
                            _recordByteIndex + 
                            _entryArrayOffset + 
                            _indexInRecord * Unsafe.SizeOf<T>();
                        return new ZCursor<T>(_buffer, bufferByteIndex);
                    }
                    throw new InvalidOperationException("Current item is not available");
                }
            }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                _hasCurrent = false;
            }

            private ref VectorRecord<T> GetCurrentRecord()
            {
                byte* pRecord = _buffer.GetRawRecordPointer(_recordByteIndex);
                return ref Unsafe.AsRef<VectorRecord<T>>(pRecord);
            }
        }
    }
}
