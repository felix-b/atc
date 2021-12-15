using System;

namespace Atc.Data.Primitives
{
    public readonly struct SingleOrRange<T>
        where T : struct
    {
        private readonly T _point0;
        private readonly T? _point1;

        public SingleOrRange(T point)
        {
            _point0 = point;
            _point1 = default;
        }

        public SingleOrRange(T rangeFrom, T rangeTo)
        {
            _point0 = rangeFrom;
            _point1 = rangeTo;
        }

        public bool IsRange => _point1.HasValue;

        public T Value
        {
            get
            {
                if (!IsRange)
                {
                    return _point0;
                }
                throw new InvalidOperationException("Range is not a value");
            }
        }

        public T RangeFrom
        {
            get
            {
                if (IsRange)
                {
                    return _point0;
                }
                throw new InvalidOperationException("Value is not a range");
            }
        }

        public T RangeTo
        {
            get
            {
                if (IsRange)
                {
                    return _point1!.Value;
                }
                throw new InvalidOperationException("Value is not a range");
            }
        }

        public T Min => _point0;
        
        public T Max => _point1.HasValue ? _point1.Value : _point0;

        public static implicit operator SingleOrRange<T>(T value)
        {
            return new SingleOrRange<T>(value);
        }

        public static implicit operator SingleOrRange<T>((T from, T to) range)
        {
            return new SingleOrRange<T>(range.from, range.to);
        }
    }
}
