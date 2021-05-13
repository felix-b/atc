using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Atc.Data.Buffers
{
    public interface IBufferContext
    {
        public TypedBuffer<T> GetBuffer<T>() where T : struct;
    }
    
    public class BufferContext : IBufferContext
    {
        private readonly Dictionary<Type, ITypedBuffer> _bufferByType = new();

        public BufferContext(params Type[] recordTypes)
        {
            foreach (var type in recordTypes)
            {
                _bufferByType.Add(type, TypedBuffer.CreateEmpty(type, initialCapacity: DefaultBufferCapacity));
            }
        }

        private BufferContext(Stream input)
        {
            using var reader = new BinaryReader(input, Encoding.UTF8, leaveOpen: true);

            var storedBufferCount = reader.ReadInt32();
            
            while (input.Position < input.Length && _bufferByType.Count < storedBufferCount)
            {
                var recordTypeName = reader.ReadString();
                var recordType = Type.GetType(recordTypeName) 
                    ?? throw new InvalidDataException($"Record type not found: '{recordTypeName}'");
                
                var buffer = TypedBuffer.CreateFromStream(recordType, input);
                _bufferByType.Add(recordType, buffer);
            }

            if (_bufferByType.Count < storedBufferCount)
            {
                throw new InvalidDataException(
                    $"Unexpected end of stream. Stored buffer count is {storedBufferCount}, but was only able to read {_bufferByType.Count}.");
            }
        }

        public TypedBuffer<T> GetBuffer<T>() where T : struct
        {
            return (TypedBuffer<T>)_bufferByType[typeof(T)];
        }

        public void WriteTo(Stream output)
        {
            using var writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen: true);
            
            writer.Write(_bufferByType.Count);

            foreach (var entry in _bufferByType)
            {
                writer.Write(entry.Key.AssemblyQualifiedName 
                    ?? throw new InvalidDataException($"Type '{entry.Key}' has no AssemblyQualifiedName"));

                entry.Value.WriteTo(output);
            }
        }

        public int RecordTypeCount => _bufferByType.Count;

        public IEnumerable<Type> RecordTypes => _bufferByType.Keys;
        
       
        public static BufferContext ReadFrom(Stream input)
        {
            return new BufferContext(input);
        }

        public static readonly int DefaultBufferCapacity = 1024;
        
        public static IBufferContext Current => BufferContextScope.CurrentContext;
    }
}