using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Atc.Data.Buffers
{
    public unsafe struct VectorBlockRecord
    {
        public int vectorItemCount;
        public int blockEntryCount;
        public int blockAllocatedEntryCount;
        public int? nextBlockPtr;
        public fixed int entryPtrs[1];
    }
    
    [RecordOptions(SizeOfType = typeof(VectorBlockRecord))]
    public unsafe struct VectorRecord<T> : IVariableSizeRecord
        where T : struct
    {
        private VectorBlockRecord _block;
        
        public int SizeOf()
        {
            return SizeOf(_block.blockEntryCount);
        }

        public void Add(in BufferPtr<T> item)
        {
            if (_block.blockAllocatedEntryCount >= _block.blockEntryCount && !_block.nextBlockPtr.HasValue)
            {
                _block.nextBlockPtr = Allocate(Span<BufferPtr<T>>.Empty, _block.blockEntryCount * 2).ByteIndex;
            }
            
            if (_block.nextBlockPtr.HasValue)
            {
               GetNextBlock().Add(item);
            }
            else
            {
                _block.entryPtrs[_block.blockAllocatedEntryCount] = item.ByteIndex;
                _block.blockAllocatedEntryCount++;
            }

            _block.vectorItemCount++;
        }

        public ref BufferPtr<T> this[int index]
        {
            get
            {
                if (index < 0 || (index >= _block.blockAllocatedEntryCount && !_block.nextBlockPtr.HasValue))
                {
                    throw new IndexOutOfRangeException();
                }
                
                if (index < _block.blockAllocatedEntryCount)
                {
                    fixed (int* p = _block.entryPtrs)
                    {
                        return ref Unsafe.AsRef<BufferPtr<T>>(p + index);
                    }
                }

                if (_block.nextBlockPtr.HasValue)
                {
                    return ref GetNextBlock()[index - _block.blockAllocatedEntryCount];
                }

                throw new InvalidDataException("Vector record is corrupt.");
            }
        }

        public int Count => _block.vectorItemCount;

        internal BufferPtr<VectorRecord<T>>? NextBlock =>
            _block.nextBlockPtr.HasValue
                ? new BufferPtr<VectorRecord<T>>(_block.nextBlockPtr.Value)
                : null;

        internal int BlockEntryCount => _block.blockEntryCount;
        
        internal int BlockAllocatedEntryCount => _block.blockAllocatedEntryCount;

        private void Initialize(int blockEntryCount, in Span<BufferPtr<T>> items)
        {
            _block.vectorItemCount = items.Length;
            _block.blockEntryCount = blockEntryCount;
            _block.blockAllocatedEntryCount = Math.Min(blockEntryCount, items.Length);
            _block.nextBlockPtr = null;
            
            for (int i = 0; i < _block.blockAllocatedEntryCount; i++)
            {
                _block.entryPtrs[i] = items[i].ByteIndex;
            }
        }
        
        private ref VectorRecord<T> GetNextBlock()
        {
            if (_block.nextBlockPtr.HasValue)
            {
                var ptr = new BufferPtr<VectorRecord<T>>(_block.nextBlockPtr.Value);
                return ref ptr.Get();
            }

            throw new NullReferenceException("Cannot get next block, it is a null pointer");
        }

        private static readonly int _baseSize = Marshal.SizeOf(typeof(VectorBlockRecord));

        public static BufferPtr<VectorRecord<T>> Allocate(in Span<BufferPtr<T>> items, int minBlockEntryCount, IBufferContext? context = null)
        {
            var blockEntryCount = Math.Max(minBlockEntryCount, items.Length);
            var byteSize = SizeOf(blockEntryCount);
            var effectiveContext = context ?? BufferContext.Current; 
            var ptr = effectiveContext.GetBuffer<VectorRecord<T>>().Allocate(byteSize);
            ptr.Get().Initialize(blockEntryCount, in items);
            return ptr;
        }
        
        public static int SizeOf(int blockEntryCount)
        {
            return _baseSize + (blockEntryCount - 1) * sizeof(int);
        }
    }
}
