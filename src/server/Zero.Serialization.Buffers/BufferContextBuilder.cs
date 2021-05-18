using System;
using System.Collections.Generic;
using System.Linq;
using Zero.Serialization.Buffers.Impl;

namespace Zero.Serialization.Buffers
{
    public class BufferContextBuilder
    {
        private readonly HashSet<Type> _recordTypes = new HashSet<Type>();
        
        public BufferContextBuilder WithString()
        {
            _recordTypes.Add(typeof(StringRecord));
            return this;
        }
        
        public BufferContextBuilder WithType<T>(
            bool alsoAsVectorItem = false, 
            bool alsoAsMapItemValue = false)
            where T : unmanaged
        {
            _recordTypes.Add(typeof(T));

            if (alsoAsVectorItem)
            {
                _recordTypes.Add(typeof(VectorRecord<T>));
                _recordTypes.Add(typeof(VectorRecord<ZRef<T>>));
            }

            if (alsoAsMapItemValue)
            {
                WithMapTo<T>();
                WithMapTo<ZRef<T>>();
            }
            
            return this;
        }

        public BufferContextBuilder WithTypes<T1, T2>(
            bool alsoAsVectorItem = false, 
            bool alsoAsMapItemValue = false)
            where T1 : unmanaged
            where T2 : unmanaged
        {
            WithType<T1>(alsoAsVectorItem, alsoAsMapItemValue);
            WithType<T2>(alsoAsVectorItem, alsoAsMapItemValue);
            return this;
        }

        public BufferContextBuilder WithTypes<T1, T2, T3>(
            bool alsoAsVectorItem = false, 
            bool alsoAsMapItemValue = false)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
        {
            WithType<T1>(alsoAsVectorItem, alsoAsMapItemValue);
            WithType<T2>(alsoAsVectorItem, alsoAsMapItemValue);
            WithType<T3>(alsoAsVectorItem, alsoAsMapItemValue);
            return this;
        }

        public BufferContextBuilder WithTypes<T1, T2, T3, T4>(
            bool alsoAsVectorItem = false, 
            bool alsoAsMapItemValue = false)
            where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
        {
            WithType<T1>(alsoAsVectorItem, alsoAsMapItemValue);
            WithType<T2>(alsoAsVectorItem, alsoAsMapItemValue);
            WithType<T3>(alsoAsVectorItem, alsoAsMapItemValue);
            WithType<T4>(alsoAsVectorItem, alsoAsMapItemValue);
            return this;
        }

        public BufferContextBuilder WithMapTo<TValue>(bool alsoAsVectorItem = false)
            where TValue : unmanaged
        {
            _recordTypes.Add(typeof(IntMapRecord<TValue>));
            _recordTypes.Add(typeof(VectorRecord<MapRecordEntry<TValue>>));

            return this;
        }

        public BufferContextScope End(out IBufferContext context)
        {
            context = new BufferContext(_recordTypes.ToArray());
            return new BufferContextScope(context);
        }

        public BufferContextScope End()
        {
            return End(out _);
        }

        public static BufferContextBuilder Begin()
        {
            return new BufferContextBuilder();
        }
    }
}
