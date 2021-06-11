using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Zero.Serialization.Buffers.Impl
{
    [BufferInfoProvider(typeof(IntMapRecordBufferInfoProvider))]
    public unsafe struct IntMapRecord<TValue> : IVariableSizeRecord, IEnumerable<ZCursor<MapRecordEntry<TValue>>>
        where TValue : unmanaged
    {
        #pragma warning disable 0649
        [InjectSelfByteIndex]
        private int _selfByteIndex;
        #pragma warning restore 0649
        
        private int _itemCount;
        private int _bucketCount;
        private fixed int _bucketPtrs[1];

        public int SizeOf()
        {
            return SizeOf(_bucketCount);
        }
        
        public bool Contains(int key)
        {
            return !Unsafe.IsNullRef(ref TryGetValue(key));
        }

        public void Add(int key, TValue value)
        {
            Add(key, ref value);
        }

        public void Add(int key, ref TValue value)
        {
            if (!TryAddOrReplace(key, ref value, shouldReplace: false))
            {
                throw new ArgumentException(
                    $"Key {key} already exists in IntMapRecord<{typeof(TValue)}>");
            }
        }

        public void Set(int key, TValue value)
        {
            Set(key, ref value);
        }

        public void Set(int key, ref TValue value)
        {
            TryAddOrReplace(key, ref value, shouldReplace: true);
        }

        public bool TryAdd(int key, TValue value)
        {
            return TryAdd(key, ref value);
        }

        public bool TryAdd(int key, ref TValue value)
        {
            return TryAddOrReplace(key, ref value, shouldReplace: false);
        }

        public ref TValue TryGetValue(int key)
        {
            var bucketIndex = GetBucketIndex(key);
            if (_bucketPtrs[bucketIndex] < 0)
            {
                return ref Unsafe.NullRef<TValue>();
            }
            
            // Console.WriteLine($"======= BEGIN TRY GET VALUE: {key} =========");
            // Console.WriteLine($">>> KEY [${key}] -> BUCKET PTR [{BucketPtrs[bucketIndex]}]");

            ref VectorRecord<MapRecordEntry<TValue>> entryVector = ref GetOrAddEntryVector(bucketIndex);

            //Console.WriteLine(entryVector.ToString());
                
            ref var result = ref TryMatchEntryAndGetValue(key, ref entryVector);
            //Console.WriteLine($"======= END TRY GET VALUE: {key} =========");
            return ref result;
        }

        public IEnumerator<ZCursor<MapRecordEntry<TValue>>> GetEnumerator()
        {
            return new EntriesEnumerator(BufferContext.Current.GetBuffer(this.GetType()), _selfByteIndex);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public ref TValue this[int key]
        {
            get
            {
                ref var result = ref TryGetValue(key);

                if (!Unsafe.IsNullRef(ref result))
                {
                    return ref result;
                }

                throw new KeyNotFoundException($"IntMapRecord<{typeof(TValue)}> has no key {key}");
            }
        }

        public int Count => _itemCount;
        
        public IEnumerable<int> Keys
        {
            get
            {
                return this.Select(
                    entry => entry.Get().Key
                );
            }
        }

        public IEnumerable<ZCursor<TValue>> Values
        {
            get
            {
                return this.Select(
                    entry => entry.AccessField<TValue>(offsetBytes: sizeof(int))
                );
            }
        }

        internal void Initialize(int bucketCount)
        {
            _itemCount = 0;
            _bucketCount = bucketCount;

            for (int i = 0; i < bucketCount; i++)
            {
                _bucketPtrs[i] = -1;
            }
        }

        internal void CountBuckets(out int total, out int used, out int unused)
        {
            total = _bucketCount;
            used = 0;
            unused = 0;
            
            for (int i = 0; i < _bucketCount; i++)
            {
                if (_bucketPtrs[i] >= 0)
                {
                    used++;
                }
                else
                {
                    unused++;
                }
            }
        }

        private bool TryAddOrReplace(int key, ref TValue value, bool shouldReplace)
        {
            var bucketIndex = GetBucketIndex(key);
            ref VectorRecord<MapRecordEntry<TValue>> entryVector = ref GetOrAddEntryVector(bucketIndex);
            
            for (int i = 0; i < entryVector.Count; i++)
            {
                ref var entry = ref entryVector[i];
                if (entry.Key == key)
                {
                    if (shouldReplace)
                    {
                        entry.Value = value;
                        return true;
                    }
                    return false;
                }
            }

            var newEntry = new MapRecordEntry<TValue> {
                Key = key,
                Value = value
            };

            entryVector.Add(ref newEntry);

            _itemCount++;

            return true;
        }

        private ref TValue TryMatchEntryAndGetValue(int key, ref VectorRecord<MapRecordEntry<TValue>> entryVector)
        {
            //Console.WriteLine(entryVector.ToString());
            
            for (int i = 0; i < entryVector.Count; i++)
            {
                ref var entry = ref entryVector[i];
                if (entry.Key == key)
                {
                    return ref entry.Value;
                }
            }

            return ref Unsafe.NullRef<TValue>();
        }

        private ref VectorRecord<MapRecordEntry<TValue>> GetOrAddEntryVector(int bucketIndex)
        {
            var ptrValue = _bucketPtrs[bucketIndex];
            var entryVectorPtr = ptrValue >= 0
                ? new ZRef<VectorRecord<MapRecordEntry<TValue>>>(ptrValue)
                : VectorRecord<MapRecordEntry<TValue>>.Allocate(
                    items: Span<MapRecordEntry<TValue>>.Empty,
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
        
        public static ZRef<IntMapRecord<TValue>> Allocate(int bucketCount, IBufferContext? context = null)
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

        private class EntriesEnumerator : IEnumerator<ZCursor<MapRecordEntry<TValue>>>
        {
            private readonly ITypedBuffer _buffer;
            private readonly int _recordByteIndex;
            private int _currentBucketIndex;
            private IEnumerator<ZCursor<MapRecordEntry<TValue>>>? _currentEntryVector;
            private bool _hasCurrent;

            public EntriesEnumerator(ITypedBuffer buffer, int recordByteIndex)
            {
                _buffer = buffer;
                _recordByteIndex = recordByteIndex;
                Reset();
            }

            public bool MoveNext()
            {
                ref var record = ref GetIntMapRecord();

                if (_currentEntryVector != null && _currentEntryVector.MoveNext())
                {
                    _hasCurrent = true;
                }
                else
                {
                    while (++_currentBucketIndex < record._bucketCount)
                    {
                        if (record._bucketPtrs[_currentBucketIndex] >= 0)
                        {
                            break;
                        }
                    }

                    if (_currentBucketIndex < record._bucketCount)
                    {
                        _currentEntryVector?.Dispose();
                        _currentEntryVector = record.GetOrAddEntryVector(_currentBucketIndex).GetEnumerator();
                        _hasCurrent = _currentEntryVector.MoveNext();
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
                _currentBucketIndex = -1;
                _currentEntryVector = null;
                _hasCurrent = false;
            }

            public ZCursor<MapRecordEntry<TValue>> Current
            {
                get
                {
                    if (_hasCurrent && _currentEntryVector != null)
                    {
                        return _currentEntryVector.Current;
                    }
                    throw new InvalidOperationException("Current item is not available");
                }
            }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                _currentEntryVector?.Dispose();
                _currentEntryVector = null;
            }

            private ref IntMapRecord<TValue> GetIntMapRecord()
            {
                var ptr = _buffer.GetRawRecordPointer(_recordByteIndex);
                ref var record = ref Unsafe.AsRef<IntMapRecord<TValue>>(ptr);
                return ref record;
            }
        }
    }

    public struct MapRecordEntry<T> 
        where T : unmanaged
    {
        public int Key;
        public T Value;
    }

    public class IntMapRecordBufferInfoProvider : IBufferInfoProvider
    {
        public IEnumerable<(string Label, string Value)> GetInfo(ITypedBuffer buffer)
        {
            if (buffer.RecordType.GetGenericTypeDefinition() != typeof(IntMapRecord<>))
            {
                throw new ArgumentException("Invalid record type", nameof(buffer));
            }

            var constructedProviderType = typeof(Provider<>)
                .MakeGenericType(buffer.RecordType.GenericTypeArguments[0]);

            var providerInstance = (IBufferInfoProvider) (Activator.CreateInstance(constructedProviderType)!);
            return providerInstance.GetInfo(buffer);
        }

        private class Provider<TValue> : IBufferInfoProvider
            where TValue : unmanaged
        {
            public IEnumerable<(string Label, string Value)> GetInfo(ITypedBuffer buffer)
            {
                int totalKeys = 0;
                int totalBuckets = 0;
                int totalUsedBuckets = 0;
                int totalUnusedBuckets = 0;
                Dictionary<int, int> keyCountGroups = new();
                Dictionary<int, int> usedBucketCountGroups = new();

                var typedBuffer = (TypedBuffer<IntMapRecord<TValue>>) buffer;
                var recordCount = typedBuffer.RecordOffsets.Count;
                for (int i = 0; i < recordCount; i++)
                {
                    var offset = typedBuffer.RecordOffsets[i];
                    ref var record = ref typedBuffer[offset];
                    AnalyzeRecord(ref record);
                }

                return new[] {
                    (Label: "total-keys..........", Value: totalKeys.ToString()),
                    (Label: "total-buckets.......", Value: totalBuckets.ToString()),
                    (Label: "total-buckets-used..", Value: totalUsedBuckets.ToString()),
                    (Label: "total-buckets-unused", Value: totalUnusedBuckets.ToString()),
                    (Label: "avg-keys............", Value: Average(totalKeys, recordCount)),
                    (Label: "avg-buckets.........", Value: Average(totalBuckets, recordCount)),
                    (Label: "avg-buckets-used....", Value: Average(totalUsedBuckets, recordCount)),
                    (Label: "avg-buckets-unused..", Value: Average(totalUnusedBuckets, recordCount)),
                    (Label: "key-count-groups....", Value: GetCountGroupText(keyCountGroups)),
                    (Label: "used-bucket-groups..", Value: GetCountGroupText(usedBucketCountGroups)),
                };

                void AnalyzeRecord(ref IntMapRecord<TValue> @record)
                {
                    totalKeys += @record.Count;
                    @record.CountBuckets(out var total, out var used, out var unused);
                    totalBuckets += total;
                    totalUsedBuckets += used;
                    totalUnusedBuckets += unused;

                    AddToCountGroup(keyCountGroups, @record.Count, 1);
                    AddToCountGroup(usedBucketCountGroups, used, 1);
                }

                void AddToCountGroup(Dictionary<int, int> destination, int groupKey, int countDelta)
                {
                    if (destination.TryGetValue(groupKey, out var currentCount))
                    {
                        destination[groupKey] = currentCount + countDelta;
                    }
                    else
                    {
                        destination[groupKey] = countDelta;
                    }
                }

                string Average(int count, int @base)
                {
                    return ((float) count / @base).ToString(CultureInfo.InvariantCulture);
                }

                string GetCountGroupText(Dictionary<int, int> countGroups)
                {
                    var result = new StringBuilder();

                    var sortedPairs = countGroups.OrderBy(pair => pair.Key);
                    foreach (var pair in sortedPairs)
                    {
                        result.Append(' ');
                        result.Append('[');
                        result.Append(pair.Key);
                        result.Append('|');
                        result.Append(pair.Value);
                        result.Append(']');
                    }

                    return result.ToString();
                }
            }
        }
    }
}
