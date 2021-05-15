using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Atc.Data.Buffers.Impl
{
    public unsafe struct VectorBlockState
    {
        public int vectorItemCount;
        public int blockEntryCount;
        public int blockAllocatedEntryCount;
        public int? nextBlockPtr;
        public fixed int entryPtrs[1];
    }
    
    [RecordOptions(SizeOfType = typeof(VectorBlockState))]
    public unsafe struct VectorRecord<T> : IVariableSizeRecord
        where T : struct
    {
        private VectorBlockState _block;

        private void AddDebugInfo(StringBuilder text)
        {
            var blockByteSize = SizeOf();

            fixed (void *pBlockStart = &_block)
            fixed (int* pEntryPtrStart = _block.entryPtrs)
            {
                byte* pBlockEnd = ((byte*) pBlockStart) + blockByteSize;
                int* pEntryPtrEnd = pEntryPtrStart + _block.blockEntryCount;
                text.AppendLine("--- begin block ---");
                text.AppendLine($"vectorItemCount          = {_block.vectorItemCount}");
                text.AppendLine($"blockEntryCount          = {_block.blockEntryCount}");
                text.AppendLine($"blockAllocatedEntryCount = {_block.blockAllocatedEntryCount}");
                text.AppendLine($"nextBlockPtr             = {_block.nextBlockPtr}");
                text.AppendLine("--- memory footprint ---");
                text.AppendLine($"SizeOf                   = {blockByteSize}");
                text.AppendLine($"block start address      = 0x{((IntPtr)pBlockStart):X}");
                text.AppendLine($"block end address        = 0x{((IntPtr)pBlockEnd):X}");
                text.AppendLine($"entryPtrs start address  = 0x{((IntPtr)pEntryPtrStart):X}");
                text.AppendLine($"entryPtrs end address    = 0x{((IntPtr)pEntryPtrEnd):X}");

                for (int i = 0; i < _block.blockEntryCount; i++)
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
                // fixed (int* pEntryPtrs = &_block.entryPtrs[0])
                // {
                //     int* p = pEntryPtrs + _block.blockAllocatedEntryCount; 
                //     *p = item.ByteIndex;
                //     //Console.WriteLine($">>> WROTE 0x{item.ByteIndex:X} @ 0x{(IntPtr)p:X}");
                //     //Console.WriteLine("////////////////////////////////////////");
                //     //Console.WriteLine(this.ToString());
                //     //Console.WriteLine("////////////////////////////////////////");
                // }
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
            
            for (int i = 0; i < _block.blockEntryCount; i++)
            {
                _block.entryPtrs[i] = i < items.Length ? items[i].ByteIndex : -1;
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

        private static readonly int _baseSize = Marshal.SizeOf(typeof(VectorBlockState));

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
