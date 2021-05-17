using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Atc.Data.Buffers.Impl
{
    public unsafe struct VectorRecord<T> : IVariableSizeRecord
        where T : struct
    {
        private IntPtr _thisPtr;
        private int _vectorItemCount;
        private int _blockEntryCount;
        private int _blockAllocatedEntryCount;
        private int? _nextBlockPtr;
        private fixed int _entryPtrs[1];

        private void AddDebugInfo(StringBuilder text)
        {
            var blockByteSize = SizeOf();

            fixed (void *pBlockStart = &_vectorItemCount)
            fixed (int* pEntryPtrStart = _entryPtrs)
            {
                byte* pBlockEnd = ((byte*) pBlockStart) + blockByteSize;
                int* pEntryPtrEnd = pEntryPtrStart + _blockEntryCount;
                text.AppendLine("--- begin block ---");
                text.AppendLine($"vectorItemCount          = {_vectorItemCount}");
                text.AppendLine($"blockEntryCount          = {_blockEntryCount}");
                text.AppendLine($"blockAllocatedEntryCount = {_blockAllocatedEntryCount}");
                text.AppendLine($"nextBlockPtr             = {_nextBlockPtr}");
                text.AppendLine("--- memory footprint ---");
                text.AppendLine($"SizeOf                   = {blockByteSize}");
                text.AppendLine($"block start address      = 0x{((IntPtr)pBlockStart):X}");
                text.AppendLine($"block end address        = 0x{((IntPtr)pBlockEnd):X}");
                text.AppendLine($"entryPtrs start address  = 0x{((IntPtr)pEntryPtrStart):X}");
                text.AppendLine($"entryPtrs end address    = 0x{((IntPtr)pEntryPtrEnd):X}");

                for (int i = 0; i < _blockEntryCount; i++)
                {
                    int* pEntry = pEntryPtrStart + i;
                    int entryValue = *pEntry;
                    text.AppendLine($"entryPtrs[{i}]             = 0x{entryValue:X} @ 0x{((IntPtr)pEntry):X}");
                }

                text.AppendLine("--- byte dump ---");

                for (int i = 0; i < blockByteSize; i++)
                {
                    byte *pByte = ((byte*)pBlockStart) + i;
                    byte b = *pByte;
                    text.Append($"0x{b:X} ");
                }
                
                text.AppendLine();
                text.AppendLine("--- end block ---");
            }
        }

        public override string ToString()
        {
            var text = new StringBuilder();
            AddDebugInfo(text);
            return text.ToString();
        }
        
        public int SizeOf()
        {
            return SizeOf(_blockEntryCount);
        }

        public void Add(BufferPtr<T> item)
        {
            fixed (void* p = &_vectorItemCount)
            {
                if (_thisPtr != new IntPtr(p))
                {
                    //Console.WriteLine("VectorRecord memory address changed!!!");
                    _thisPtr = new IntPtr(p);
                    //throw new InvalidOperationException("VectorRecord memory address changed!!!");
                }
            }

            if (_blockAllocatedEntryCount >= _blockEntryCount && !_nextBlockPtr.HasValue)
            {
                var nextBlockByteSize = SizeOf(_blockEntryCount * 2);
                var nextBlockPtr = BufferContext.Current.GetBuffer<VectorRecord<T>>().Allocate(nextBlockByteSize);
                ref var nextBlockRecord = ref nextBlockPtr.Get();
                nextBlockRecord.Initialize(_blockEntryCount * 2);

                var nextBlockPtrNewValue = new Nullable<int>(nextBlockPtr.ByteIndex);
                _nextBlockPtr = nextBlockPtrNewValue;
            }
            
            if (_nextBlockPtr.HasValue)
            {
               GetNextBlock().Add(item);
            }
            else
            {
                _entryPtrs[_blockAllocatedEntryCount] = item.ByteIndex;
                // fixed (int* pEntryPtrs = &entryPtrs[0])
                // {
                //     int* p = pEntryPtrs + blockAllocatedEntryCount; 
                //     *p = item.ByteIndex;
                //     //Console.WriteLine($">>> WROTE 0x{item.ByteIndex:X} @ 0x{(IntPtr)p:X}");
                //     //Console.WriteLine("////////////////////////////////////////");
                //     //Console.WriteLine(this.ToString());
                //     //Console.WriteLine("////////////////////////////////////////");
                // }
                _blockAllocatedEntryCount++;
            }

            _vectorItemCount++;
        }

        public ref BufferPtr<T> this[int index]
        {
            get
            {
                if (index < 0 || (index >= _blockAllocatedEntryCount && !_nextBlockPtr.HasValue))
                {
                    throw new IndexOutOfRangeException();
                }
                
                if (index < _blockAllocatedEntryCount)
                {
                    fixed (int* p = _entryPtrs)
                    {
                        return ref Unsafe.AsRef<BufferPtr<T>>(p + index);
                    }
                }

                if (_nextBlockPtr.HasValue)
                {
                    return ref GetNextBlock()[index - _blockAllocatedEntryCount];
                }

                throw new InvalidDataException("Vector record is corrupt.");
            }
        }

        public int Count => _vectorItemCount;

        internal BufferPtr<VectorRecord<T>>? NextBlock =>
            _nextBlockPtr.HasValue
                ? new BufferPtr<VectorRecord<T>>(_nextBlockPtr.Value)
                : null;

        internal int BlockEntryCount => _blockEntryCount;
        
        internal int BlockAllocatedEntryCount => _blockAllocatedEntryCount;

        private void Initialize(int blockEntryCount)
        {
            fixed (void* p = &_vectorItemCount)
            {
                _thisPtr = new IntPtr(p);
            }

            _vectorItemCount = 0;
            _blockEntryCount = blockEntryCount;
            _blockAllocatedEntryCount = 0;
            _nextBlockPtr = null;
            
            for (int i = 0; i < _blockEntryCount; i++)
            {
                _entryPtrs[i] = -1;
            }
        }
        
        private ref VectorRecord<T> GetNextBlock()
        {
            if (_nextBlockPtr.HasValue)
            {
                var ptr = new BufferPtr<VectorRecord<T>>(_nextBlockPtr.Value);
                return ref ptr.Get();
            }

            throw new NullReferenceException("Cannot get next block, it is a null pointer");
        }

        private static readonly int _baseSize = Unsafe.SizeOf<VectorRecord<T>>();

        public static BufferPtr<VectorRecord<T>> Allocate(Span<BufferPtr<T>> items, int minBlockEntryCount, IBufferContext? context = null)
        {
            var blockEntryCount = Math.Max(minBlockEntryCount, items.Length);
            var effectiveContext = context ?? BufferContext.Current; 
            var byteSize = SizeOf(blockEntryCount);

            var ptr = effectiveContext.GetBuffer<VectorRecord<T>>().Allocate(byteSize);
            ref var vector = ref ptr.Get();
            vector.Initialize(blockEntryCount);

            for (int i = 0; i < items.Length; i++)
            {
                vector.Add(items[i]);
            }
            
            return ptr;
        }
        
        public static int SizeOf(int blockEntryCount)
        {
            return _baseSize + (blockEntryCount - 1) * sizeof(int);
        }
    }
}
