using System;
using System.Collections.Generic;
using System.Linq;
using Atc.Data.Buffers.Impl;

namespace Atc.Data.Buffers
{
    public class BufferContextBuilder
    {
        private readonly HashSet<Type> _recordTypes = new HashSet<Type>();
        
        public BufferContextBuilder WithString()
        {
            _recordTypes.Add(typeof(StringRecord));
            return this;
        }
        
        public BufferContextBuilder WithType<T>(bool alsoAsVectorItem = false)
            where T : struct
        {
            _recordTypes.Add(typeof(T));

            if (alsoAsVectorItem)
            {
                _recordTypes.Add(typeof(VectorRecord<T>));
            }
            
            return this;
        }

        public BufferContextBuilder WithTypes<T1, T2>(bool alsoAsVectorItem = false)
            where T1 : struct
            where T2 : struct
        {
            WithType<T1>(alsoAsVectorItem);
            WithType<T2>(alsoAsVectorItem);
            return this;
        }

        public BufferContextBuilder WithTypes<T1, T2, T3>(bool alsoAsVectorItem = false)
            where T1 : struct
            where T2 : struct
            where T3 : struct
        {
            WithType<T1>(alsoAsVectorItem);
            WithType<T2>(alsoAsVectorItem);
            WithType<T3>(alsoAsVectorItem);
            return this;
        }

        public BufferContextBuilder WithTypes<T1, T2, T3, T4>(bool alsoAsVectorItem = false)
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
        {
            WithType<T1>(alsoAsVectorItem);
            WithType<T2>(alsoAsVectorItem);
            WithType<T3>(alsoAsVectorItem);
            WithType<T4>(alsoAsVectorItem);
            return this;
        }

        public BufferContextBuilder WithIntMap<TValue>(bool alsoAsVectorItem = false)
            where TValue : struct
        {
            _recordTypes.Add(typeof(IntMapRecord<TValue>));
            _recordTypes.Add(typeof(MapRecordEntry));
            _recordTypes.Add(typeof(VectorRecord<MapRecordEntry>));

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
