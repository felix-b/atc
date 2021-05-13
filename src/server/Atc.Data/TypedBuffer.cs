using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Atc.Data
{
    public interface ITypedBuffer
    {
        void WriteTo(Stream output);
    }

    public interface IVariableSizeRecord
    {
        int GetRecordSize();
    }

    public class TypedBuffer
    {
        public static ITypedBuffer CreateFromStream(Type recordType, Stream input)
        {
            var bufferType = typeof(TypedBuffer<>).MakeGenericType(recordType);
            var buffer = Activator.CreateInstance(bufferType, new object[] {input});
            
            return buffer as ITypedBuffer 
                ?? throw new InvalidDataException($"Could not create typed buffer for record type '{recordType}'");
        }

        public static TypedBuffer<T> ReadFrom<T>(Stream input) where T : struct
        {
            return (TypedBuffer<T>) CreateFromStream(typeof(T), input);
        }
    }
    
    public unsafe class TypedBuffer<T> : ITypedBuffer
        where T : struct
    {
        public readonly int RecordSize = Marshal.SizeOf(typeof(T));
        public readonly bool IsVariableRecordSize = typeof(T).IsAssignableTo(typeof(IVariableSizeRecord));
        public readonly bool ReadOnly;
        public readonly int Capacity;

        private byte[] _buffer;
        private int _count;
        private int _freeByteIndex = 0;
        private BufferPtr<T>? _lastAllocatedPtr = null;

        public TypedBuffer(Stream input)
        {
            ReadOnly = true;

            using var reader = new BinaryReader(input, Encoding.UTF8, leaveOpen: true);

            var storedSize = reader.ReadInt32();
            if (storedSize < 0)
            {
                throw new InvalidDataException($"Stored Size value cannot be negative");
            }
            
            var storedRecordSize = reader.ReadInt32();
            if (storedRecordSize != RecordSize)
            {
                throw new InvalidDataException(
                    $"Expected record size of {RecordSize}, but stored record size is {storedRecordSize}");
            }

            _count = reader.ReadInt32();
            if (_count < 0)
            {
                throw new InvalidDataException($"Stored Count value cannot be negative");
            }

            Capacity = _count; 
            _freeByteIndex = storedSize;
            _buffer = new byte[storedSize];
            input.Read(_buffer, 0, _buffer.Length);
        }

        public TypedBuffer(int capacity)
        {
            ReadOnly = false;
            Capacity = capacity > 0 
                ? capacity 
                : throw new ArgumentOutOfRangeException(nameof(capacity), "Must be positive");
            _buffer = new byte[Capacity * RecordSize];
        }

        public ref T this[int firstByteIndex]
        {
            get
            {
                if (firstByteIndex < 0 || firstByteIndex >= _buffer.Length)
                {
                    throw new IndexOutOfRangeException();
                }

                fixed (byte* p = &_buffer[firstByteIndex])
                {
                    return ref Unsafe.AsRef<T>(p);
                }
            }
        }

        public BufferPtr<T> Allocate()
        {
            if (ReadOnly)
            {
                throw new InvalidOperationException("This TypeStream is read-only");
            }

            if (_lastAllocatedPtr.HasValue && IsVariableRecordSize)
            {
                var record = (IVariableSizeRecord)_lastAllocatedPtr.Value.Get();
                _freeByteIndex = _lastAllocatedPtr.Value.ByteIndex + record.GetRecordSize();
            }

            if (_freeByteIndex + RecordSize > _buffer.Length)
            {
                var temp = new byte[_buffer.Length + Capacity * RecordSize];
                Array.Copy(_buffer, temp, _buffer.Length);
                _buffer = temp;
            }

            var byteIndex = _freeByteIndex;
            _freeByteIndex += RecordSize;
            _lastAllocatedPtr = new BufferPtr<T>(byteIndex);
            _count++;

            return _lastAllocatedPtr.Value;
        }

        public void WriteTo(Stream output)
        {
            using var writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen: true);

            writer.Write(_freeByteIndex);
            writer.Write(RecordSize);
            writer.Write(_count);
            output.Write(_buffer, 0, _freeByteIndex);
            output.Flush();
        }
        
        public int RecordCount => _count;

        public int TotalBytes => _buffer.Length;

        public int AllocatedBytes => _freeByteIndex;
    }
}
