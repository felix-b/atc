using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Atc.Data.Buffers.Impl
{
    public unsafe struct IntMapRecord<TValue> : IVariableSizeRecord
        where TValue : struct
    {
        private int _itemCount;
        private int _bucketCount;
        private fixed int _bucketPtrs[1];

        public int SizeOf()
        {
            return SizeOf(_bucketCount);
        }
        
        public bool Contains(int key)
        {
            return TryGetValue(key).HasValue;
        }

        public void Add(int key, BufferPtr<TValue> value)
        {
            if (!TryAddOrReplace(key, value, shouldReplace: false))
            {
                throw new ArgumentException(
                    $"Key {key} already exists in IntMapRecord<{typeof(TValue)}>");
            }
        }

        public void Set(int key, BufferPtr<TValue> value)
        {
            TryAddOrReplace(key, value, shouldReplace: true);
        }

        public bool TryAdd(int key, BufferPtr<TValue> value)
        {
            return TryAddOrReplace(key, value, shouldReplace: false);
        }

        public BufferPtr<TValue>? TryGetValue(int key)
        {
            var bucketIndex = GetBucketIndex(key);
            if (_bucketPtrs[bucketIndex] < 0)
            {
                return null;
            }
            
            // Console.WriteLine($"======= BEGIN TRY GET VALUE: {key} =========");
            // Console.WriteLine($">>> KEY [${key}] -> BUCKET PTR [{BucketPtrs[bucketIndex]}]");

            ref VectorRecord<MapRecordEntry> entryVector = ref GetOrAddEntryVector(bucketIndex);

            //Console.WriteLine(entryVector.ToString());
                
            var result = TryMatchEntryAndGetValue(key, ref entryVector);
            //Console.WriteLine($"======= END TRY GET VALUE: {key} =========");
            return result;
        }

        public BufferPtr<TValue> this[int key]
        {
            get
            {
                var ptr = TryGetValue(key);

                if (ptr.HasValue)
                {
                    return ptr.Value;
                }

                throw new KeyNotFoundException(
                    $"IntMapRecord<{typeof(TValue)}> has no key {key}");
            }
        }

        public int Count => _itemCount;

        public IEnumerable<int> Keys => throw new NotImplementedException();

        public IEnumerable<BufferPtr<TValue>> Values => throw new NotImplementedException();

        internal void Initialize(int bucketCount)
        {
            _itemCount = 0;
            _bucketCount = bucketCount;

            for (int i = 0; i < bucketCount; i++)
            {
                _bucketPtrs[i] = -1;
            }
        }

        private bool TryAddOrReplace(int key, BufferPtr<TValue> value, bool shouldReplace)
        {
            var bucketIndex = GetBucketIndex(key);
            ref VectorRecord<MapRecordEntry> entryVector = ref GetOrAddEntryVector(bucketIndex);
            
            for (int i = 0; i < entryVector.Count; i++)
            {
                ref MapRecordEntry entry = ref entryVector[i].Get();
                if (entry.Key == key)
                {
                    if (shouldReplace)
                    {
                        entry.ValuePtr = value.ByteIndex;
                        return true;
                    }
                    return false;
                }
            }

            var newEntryPtr = BufferContext.Current.AllocateRecord(new MapRecordEntry {
                Key = key,
                ValuePtr = value.ByteIndex
            });

            entryVector.Add(newEntryPtr);

            _itemCount++;

            return true;
        }

        private BufferPtr<TValue>? TryMatchEntryAndGetValue(int key, ref VectorRecord<MapRecordEntry> entryVector)
        {
            //Console.WriteLine(entryVector.ToString());
            
            for (int i = 0; i < entryVector.Count; i++)
            {
                var entryPtr = entryVector[i];
                ref MapRecordEntry entry = ref entryPtr.Get();
                if (entry.Key == key)
                {
                    return new BufferPtr<TValue>(entry.ValuePtr);
                }
            }

            return null;
        }

        private ref VectorRecord<MapRecordEntry> GetOrAddEntryVector(int bucketIndex)
        {
            var ptrValue = _bucketPtrs[bucketIndex];
            var entryVectorPtr = ptrValue >= 0
                ? new BufferPtr<VectorRecord<MapRecordEntry>>(ptrValue)
                : VectorRecord<MapRecordEntry>.Allocate(
                    items: Span<BufferPtr<MapRecordEntry>>.Empty,
                    minBlockEntryCount: 4);

            if (ptrValue < 0)
            {
                _bucketPtrs[bucketIndex] = entryVectorPtr.ByteIndex;
            }

            return ref entryVectorPtr.Get();
        }

        private int GetBucketIndex(int key)
        {
            var bucketIndex = key % _bucketCount;
            return bucketIndex;
        }

        private static readonly int _baseSize = Unsafe.SizeOf<IntMapRecord<TValue>>();
        
        public static BufferPtr<IntMapRecord<TValue>> Allocate(int bucketCount, IBufferContext? context = null)
        {
            var byteSize = SizeOf(bucketCount);
            var effectiveContext = context ?? BufferContext.Current; 
            var ptr = effectiveContext.GetBuffer<IntMapRecord<TValue>>().Allocate(byteSize);
            ptr.Get().Initialize(bucketCount);
            return ptr;
        }
        
        public static int SizeOf(int bucketCount)
        {
            return _baseSize + (bucketCount - 1) * sizeof(int);
        }
    }

    public struct MapRecordEntry
    {
        public int Key { get; init; }
        public int ValuePtr { get; set; }
    }
}
