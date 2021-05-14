using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Atc.Data.Buffers.Impl
{
    public interface ITypedBuffer
    {
        void WriteTo(Stream output);
    }

    public interface IVariableSizeRecord
    {
        int SizeOf();
    }

    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public class RecordOptionsAttribute : Attribute
    {
        public Type? SizeOfType { get; set; }
    }

    public class TypedBuffer
    {
        public static ITypedBuffer CreateEmpty(Type recordType, int initialCapacity)
        {
            return Create(recordType, initialCapacity);
        }

        public static ITypedBuffer CreateFromStream(Type recordType, Stream input)
        {
            return Create(recordType, input);
        }

        public static TypedBuffer<T> CreateFromStreamOf<T>(Stream input) where T : struct
        {
            return (TypedBuffer<T>) CreateFromStream(typeof(T), input);
        }

        private static ITypedBuffer Create(Type recordType, object constructorArg)
        {
            var bufferType = typeof(TypedBuffer<>).MakeGenericType(recordType);
            var buffer = Activator.CreateInstance(bufferType, constructorArg);
            
            return buffer as ITypedBuffer 
                ?? throw new InvalidDataException($"Could not create typed buffer for record type '{recordType}'");
        }
    }
    
    public unsafe class TypedBuffer<T> : ITypedBuffer
        where T : struct
    {
        public readonly int RecordSize = GetSizeOfRecord();

        public readonly bool IsVariableRecordSize = typeof(T).IsAssignableTo(typeof(IVariableSizeRecord));
        public readonly bool ReadOnly;
        public readonly int InitialCapacity;

        private byte[] _buffer;
        private int _count;
        private int _freeByteIndex = 0;

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

            InitialCapacity = _count; 
            _freeByteIndex = storedSize;
            _buffer = new byte[storedSize];
            input.Read(_buffer, 0, _buffer.Length);
        }

        public TypedBuffer(int initialCapacity)
        {
            ReadOnly = false;
            InitialCapacity = initialCapacity > 0 
                ? initialCapacity 
                : throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Must be positive");
            _buffer = new byte[InitialCapacity * RecordSize];
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
            return InternalAllocate(RecordSize);
        }

        public BufferPtr<T> Allocate(int sizeInBytes)
        {
            return InternalAllocate(sizeInBytes);
        }

        public BufferPtr<T> Allocate(in T value)
        {
            var size = IsVariableRecordSize
                ? ((IVariableSizeRecord)value).SizeOf()
                : RecordSize;
            
            var ptr = InternalAllocate(size);
            ref T recordRef = ref this[ptr.ByteIndex];
            recordRef = value;
            return ptr;
        }

        private BufferPtr<T> InternalAllocate(int sizeInBytes)
        {
            if (ReadOnly)
            {
                throw new InvalidOperationException("Cannot allocate in this TypeStream - it is read-only");
            }

            if (sizeInBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeInBytes), "Allocation size must be a positive number.");
            }
                
            if (_freeByteIndex + sizeInBytes > _buffer.Length)
            {
                var temp = new byte[_buffer.Length + InitialCapacity * RecordSize];
                Array.Copy(_buffer, temp, _buffer.Length);
                _buffer = temp;
            }

            var byteIndex = _freeByteIndex;
            _freeByteIndex += sizeInBytes;
            _count++;
            
            var ptr = new BufferPtr<T>(byteIndex);
            return ptr;
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

        private static int GetSizeOfRecord()
        {
            var recordType = typeof(T);
            if (!recordType.IsGenericType)
            {
                return Marshal.SizeOf(recordType);
            }

            var optionsAttribute = recordType.GetCustomAttribute<RecordOptionsAttribute>();
            if (optionsAttribute != null && optionsAttribute.SizeOfType != null)
            {
                return Marshal.SizeOf(optionsAttribute.SizeOfType);
            }

            throw new InvalidDataException("Record of generic type must specify SizeOfType in RecordOptionsAttribute");
        }
    }
}
