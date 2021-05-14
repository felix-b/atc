using System;
using System.Linq;
using Atc.Data.Buffers.Impl;

namespace Atc.Data.Buffers
{
    public static class BufferContextExtensions
    {
        public static BufferPtr<T> AllocateRecord<T>(this IBufferContext context)
            where T : struct
        {
            return context.GetBuffer<T>().Allocate();
        }

        public static BufferPtr<T> AllocateRecord<T>(this IBufferContext context, int sizeInBytes)
            where T : struct
        {
            return context.GetBuffer<T>().Allocate(sizeInBytes);
        }

        public static BufferPtr<T> AllocateRecord<T>(this IBufferContext context, in T value)
            where T : struct
        {
            return context.GetBuffer<T>().Allocate(value);
        }

        public static StringRef AllocateString(this IBufferContext context, string value)
        {
            var innerPtr = StringRecord.Allocate(value, context);
            return new StringRef(innerPtr);
        }

        public static BufferPtr<StringRecord> AllocateStringRecord(this IBufferContext context, string value)
        {
            return StringRecord.Allocate(value, context);
        }

        public static Vector<T> AllocateVector<T>(this IBufferContext context, params BufferPtr<T>[] items)
            where T : struct
        {
            var minBlockEntryCount = items.Length > 0 ? 0 : 10;
            var innerPtr = context.AllocateVectorRecord<T>(items, minBlockEntryCount);
            return new Vector<T>(innerPtr);
        }

        public static BufferPtr<VectorRecord<T>> AllocateVectorRecord<T>(this IBufferContext context, int minBlockEntryCount)
            where T : struct
        {
            return AllocateVectorRecord(context, Array.Empty<BufferPtr<T>>(), minBlockEntryCount);
        }

        public static BufferPtr<VectorRecord<T>> AllocateVectorRecord<T>(this IBufferContext context, params BufferPtr<T>[] items)
            where T : struct
        {
            return AllocateVectorRecord(context, items, minBlockEntryCount: 10);
        }

        public static BufferPtr<VectorRecord<T>> AllocateVectorRecord<T>(this IBufferContext context, BufferPtr<T>[] items, int minBlockEntryCount)
            where T : struct
        {
            var itemsAsSpan = items.AsSpan(); 
            return VectorRecord<T>.Allocate(in itemsAsSpan, minBlockEntryCount, context);
        }
    }
}
