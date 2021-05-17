﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Atc.Data.Buffers.Impl
{
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

    public unsafe class TypedBuffer<T> : ITypedBuffer, IDisposable
        where T : struct
    {
        private readonly StructTypeHandler _typeHandler;
        private readonly bool _readOnly;
        private readonly int _initialCapacity;

        private byte[] _buffer;
        //private GCHandle? _pinnedHandle;
        private int _count;
        private int _freeByteIndex = 0;
        private readonly List<int> _recordPtrs = new List<int>();

        public TypedBuffer(Stream input)
        {
            _typeHandler = new StructTypeHandler(typeof(T));
            _readOnly = true;

            using var reader = new BinaryReader(input, Encoding.UTF8, leaveOpen: true);

            var storedSize = reader.ReadInt32();
            if (storedSize < 0)
            {
                throw new InvalidDataException($"Stored Size value cannot be negative");
            }

            var storedRecordSize = reader.ReadInt32();
            if (storedRecordSize != _typeHandler.Size)
            {
                throw new InvalidDataException(
                    $"Expected record size of {_typeHandler.Size}, but stored record size is {storedRecordSize}");
            }

            _count = reader.ReadInt32();
            if (_count < 0)
            {
                throw new InvalidDataException($"Stored Count value cannot be negative");
            }

            for (int i = 0; i < _count; i++)
            {
                _recordPtrs.Add(reader.ReadInt32());
            }

            _initialCapacity = _count;
            _freeByteIndex = storedSize;
            _buffer = new byte[storedSize];
            //_pinnedHandle = GCHandle.Alloc(_buffer, GCHandleType.Pinned);            
            
            input.Read(_buffer, 0, _buffer.Length);
        }

        public TypedBuffer(int initialCapacity)
        {
            _typeHandler = new StructTypeHandler(typeof(T));
            _readOnly = false;
            _initialCapacity = initialCapacity > 0
                ? initialCapacity
                : throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Must be positive");
            _buffer = new byte[_initialCapacity * _typeHandler.Size];
            //_pinnedHandle = null;
        }

        public void Dispose()
        {
            // if (_pinnedHandle.HasValue)
            // {
            //     _pinnedHandle.Value.Free();
            //     _pinnedHandle = null;
            // }
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
            return InternalAllocate(_typeHandler.Size);
        }

        public BufferPtr<T> Allocate(int sizeInBytes)
        {
            return InternalAllocate(sizeInBytes);
        }

        public BufferPtr<T> Allocate(T value)
        {
            var size = _typeHandler.IsVariableSize
                ? ((IVariableSizeRecord)value).SizeOf()
                : _typeHandler.Size;
            
            var ptr = InternalAllocate(size);
            ref T recordRef = ref this[ptr.ByteIndex];
            recordRef = value;
            return ptr;
        }

        public ref byte[] GetRawBytesRef(int firstByteIndex)
        {

            if (firstByteIndex < 0 || firstByteIndex >= _buffer.Length)
            {
                throw new IndexOutOfRangeException();
            }

            fixed (byte* p = &_buffer[firstByteIndex])
            {
                return ref Unsafe.AsRef<byte[]>(p);
            }
        }

        private BufferPtr<T> InternalAllocate(int sizeInBytes)
        {
            if (_readOnly)
            {
                throw new InvalidOperationException("Cannot allocate in this TypeStream - it is read-only");
            }

            if (sizeInBytes <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeInBytes), "Allocation size must be a positive number.");
            }
                
            if (_freeByteIndex + sizeInBytes > _buffer.Length)
            {
                var temp = new byte[_buffer.Length + _initialCapacity * _typeHandler.Size];
                Array.Copy(_buffer, temp, _buffer.Length);
                _buffer = temp;
            }

            var byteIndex = _freeByteIndex;
            _freeByteIndex += sizeInBytes;
            _count++;
            _recordPtrs.Add(byteIndex);
            
            var ptr = new BufferPtr<T>(byteIndex);
            return ptr;
        }

        public void WriteTo(Stream output)
        {
            using var writer = new BinaryWriter(output, Encoding.Unicode, leaveOpen: true);

            writer.Write(_freeByteIndex);
            writer.Write(_typeHandler.Size);
            writer.Write(_count);

            for (int i = 0; i < _count; i++)
            {
                writer.Write(_recordPtrs[i]);
            }
            
            output.Write(_buffer, 0, _freeByteIndex);
            output.Flush();
        }

        public void DumpToConsole()
        {
            Console.WriteLine($"###### BEGIN TYPED BUFFER <{typeof(T).FullName}> ######");
            Console.WriteLine($"RecordSize     = {_typeHandler.Size}");
            Console.WriteLine($"IsVarRecSize   = {_typeHandler.IsVariableSize}");
            Console.WriteLine($"RecordCount    = {RecordCount}");
            Console.WriteLine($"TotalBytes     = {TotalBytes}");
            Console.WriteLine($"AllocatedBytes = {AllocatedBytes}");

            for (int i = 0; i < _count; i++)
            {
                var ptr = new BufferPtr<T>(_recordPtrs[i]);
                ref T record = ref ptr.Get();
                Console.WriteLine($"=== begin RECORD {i}/{_count} ===");
                Console.WriteLine(record.ToString());
                Console.WriteLine($"=== end RECORD {i}/{_count} ===");
            }
            
            Console.WriteLine($"###### END TYPED BUFFER <{typeof(T).FullName}> ######");
            Console.WriteLine();
        }

       
        public void DumpToDisk(string filePath)
        {
            using var file = File.Create(filePath);
            WriteTo(file);
            file.Flush();
        }

        public Memory<byte> GetRecordMemory(int index, out int offset)
        {
            offset = _recordPtrs[index];
            var nextOffset = index < _count - 1 ? _recordPtrs[index + 1] : _freeByteIndex;
            var size = nextOffset - offset;
            return _buffer.AsMemory(offset, size);
        }

        public bool ReadOnly => _readOnly;
        
        public Type RecordType => typeof(T);

        public StructTypeHandler TypeHandler => _typeHandler;

        public int RecordSize => _typeHandler.Size;

        public bool IsVariableRecordSize => _typeHandler.IsVariableSize;

        public int RecordCount => _count;

        public int InitialCapacity => _initialCapacity;

        public int TotalBytes => _buffer.Length;

        public int AllocatedBytes => _freeByteIndex;

        public IReadOnlyList<int> RecordOffsets => _recordPtrs;
    }
}