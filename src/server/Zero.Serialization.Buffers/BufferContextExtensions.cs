using System;
using Zero.Serialization.Buffers.Impl;

namespace Zero.Serialization.Buffers
{
    public static class BufferContextExtensions
    {
        public static ZRef<T> AllocateRecord<T>(this IBufferContext context)
            where T : unmanaged
        {
            return context.GetBuffer<T>().Allocate();
        }

        public static ZRef<T> AllocateRecord<T>(this IBufferContext context, T value)
            where T : unmanaged
        {
            return context.GetBuffer<T>().Allocate(value);
        }

        public static ZStringRef AllocateString(this IBufferContext context, string value)
        {
            var innerPtr = StringRecord.Allocate(value, context);
            return new ZStringRef(innerPtr);
        }

        public static ZVectorRef<T> AllocateVector<T>(this IBufferContext context, params T[] items)
            where T : unmanaged
        {
            var minBlockEntryCount = items.Length > 0 ? 0 : 10;
            var innerPtr = context.AllocateVectorRecord<T>(items, minBlockEntryCount);
            return new ZVectorRef<T>(innerPtr);
        }

        public static ZIntMapRef<TValue> AllocateIntMap<TValue>(this IBufferContext context, int bucketCount = 1024)
            where TValue : unmanaged
        {
            var innerPtr = IntMapRecord<TValue>.Allocate(bucketCount, context);
            return new ZIntMapRef<TValue>(innerPtr);
        }

        public static ZStringMapRef<TValue> AllocateStringMap<TValue>(this IBufferContext context, int bucketCount = 1024)
            where TValue : unmanaged
        {
            var innerPtr = IntMapRecord<TValue>.Allocate(bucketCount, context);
            return new ZStringMapRef<TValue>(innerPtr);
        }

        internal static ZRef<VectorRecord<T>> AllocateVectorRecord<T>(this IBufferContext context, int minBlockEntryCount)
            where T : unmanaged
        {
            return AllocateVectorRecord(context, Array.Empty<T>(), minBlockEntryCount);
        }

        internal static ZRef<VectorRecord<T>> AllocateVectorRecord<T>(this IBufferContext context, T[] items, int minBlockEntryCount)
            where T : unmanaged
        {
            var itemsAsSpan = items.AsSpan(); 
            return VectorRecord<T>.Allocate(itemsAsSpan, minBlockEntryCount, context);
        }
    }
}
