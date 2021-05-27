﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Zero.Serialization.Buffers.Impl
{
    public class BufferContext : IBufferContext
    {
        private readonly Dictionary<Type, ITypedBuffer> _bufferByType = new();
        private readonly Dictionary<string, ZRef<StringRecord>> _stringRefByValue = new();

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

            TryLoadStringDictionary();
        }

        public TypedBuffer<T> GetBuffer<T>() where T : struct
        {
            return (TypedBuffer<T>)_bufferByType[typeof(T)];
        }

        public ITypedBuffer GetBuffer(Type recordType)
        {
            return _bufferByType[recordType];
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
        
        public BufferContextWalker GetWalker()
        {
            return new BufferContextWalker(this);
        }

        internal bool TryGetString(string s, out ZRef<StringRecord>? stringRef)
        {
            if (_stringRefByValue.TryGetValue(s, out var ptr))
            {
                stringRef = ptr;
                return true;
            }

            stringRef = null;
            return false;
        }

        bool IBufferContext.TryGetString(string s, out ZStringRef stringRef)
        {
            if (_stringRefByValue.TryGetValue(s, out var ptr))
            {
                stringRef = new ZStringRef(ptr);
                return true;
            }

            stringRef = default;
            return false;
        }

        public ZStringRef GetString(string s)
        {
            if (_stringRefByValue.TryGetValue(s, out var ptr))
            {
                return new ZStringRef(ptr);
            }

            throw new ArgumentException($"String '{s}' was not found in this BufferContext");
        }

        public int RecordTypeCount => _bufferByType.Count;

        public IEnumerable<Type> RecordTypes => _bufferByType.Keys;

        internal void RegisterAllocatedString(string s, ZRef<StringRecord> stringRef)
        {
            _stringRefByValue.Add(s, stringRef);
        }

        private void TryLoadStringDictionary()
        {
            if (_bufferByType.TryGetValue(typeof(StringRecord), out var buffer))
            {
                var stringBuffer = (TypedBuffer<StringRecord>) buffer;
                
                for (int i = 0; i < stringBuffer.RecordCount; i++)
                {
                    var recordRef = new ZRef<StringRecord>(stringBuffer.RecordOffsets[i]);
                    ref StringRecord record = ref stringBuffer[recordRef.ByteIndex];
                    _stringRefByValue[record.Str] = recordRef;
                }
            }
        }

        public static BufferContext ReadFrom(Stream input)
        {
            return new BufferContext(input);
        }

        public static readonly int DefaultBufferCapacity = 1024;
        
        public static IBufferContext Current => BufferContextScope.CurrentContext;
    }
}