using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Atc.Data.Buffers.Impl
{
    public unsafe class StructTypeHandler
    {
        private readonly Type _type;
        private readonly int _size;
        private readonly bool _isVariableSize;
        private readonly StructFieldInfo[] _fields;
        private readonly VariableSizeGetterFunc _getVariableSize;
        private readonly VariableBufferGetterFunc _getVariableBuffer;

        public StructTypeHandler(Type type)
            : this(type, new HashSet<Type>())
        {
        }

        private StructTypeHandler(Type type, HashSet<Type> pendingTypeHandlers)
        {
            _type = type;
            _size = SizeOf(type);
            
            var typeFields = type.GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly);

            _fields = typeFields
                .Select(t => GetStructFieldInfo(t, pendingTypeHandlers))
                .ToArray();

            var variableBufferField = _fields.FirstOrDefault(f => f.IsVariableBuffer);
            _isVariableSize = 
                type.IsAssignableTo(typeof(IVariableSizeRecord)) && 
                variableBufferField != null;
            
            _getVariableSize = _isVariableSize
                ? GetVariableSizeGetterFunc(_type)
                : (_) => throw new NotSupportedException("This type handler does not support variable size");
            
            _getVariableBuffer = _isVariableSize && variableBufferField != null
                ? GetVariableBufferGetterFunc(_type, variableBufferField)
                : (_, _) => throw new NotSupportedException("This type handler does not support variable size");
        }

        
        public FieldValuePair[] GetFieldValues(void *pStruct)
        {
            //ref byte[] bytes = ref _buffer.GetRawBytesRef(offset);
            //byte* pRecord = (byte*) Unsafe.AsPointer(ref bytes);

            var result = new List<FieldValuePair>();
            byte* pRecordBytes = (byte*) pStruct;
            
            foreach (var field in _fields)
            {
                object? value = field.IsVariableBuffer
                    ? _getVariableBuffer(field, (byte*)pStruct)
                    : GetFieldValue(field, (byte*)pStruct);
                
                result.Add(new FieldValuePair(field, value));
            }

            return result.ToArray();
        }

        public int GetInstanceSize(void* pStruct)
        {
            return _isVariableSize
                ? _getVariableSize((byte*) pStruct)
                : _size;
        }

        // private byte[] GetVariableBufferValue(StructFieldInfo field, byte* pStruct)
        // {
        //     if (_getVariableSize == null)
        //     {
        //         throw new InvalidOperationException($"Struct '{_type.Name}' has no variable size getter.");
        //     }
        //     
        //     byte* pField = pStruct + field.Offset;
        //     var totalSize = _getVariableSize(pStruct);
        //     var bufferSize = totalSize - field.Offset;
        //
        //     var valueSpan = new Span<byte>(pField, bufferSize);
        //     return valueSpan.ToArray();
        // }

        private object? GetFieldValue(StructFieldInfo field, byte* pStruct)
        {
            byte* pField = pStruct + field.Offset;
            
            //var valueBytes = new Span<byte>(pField, field.Size);
            // object valueObject = _valueOfDelegateByType.TryGetValue(field.Type, out var valueOfFunc)
            //     ? valueOfFunc(pField)
            //     : new Span<byte>(pField, field.Size).ToArray();

            if (field.ValueTypeHandler != null)
            {
                return field.ValueTypeHandler.GetFieldValues(pField);
            }

            var value = field.GetValue(pField);
            return value;
            
            // object valueObject;
            // if (_valueOfDelegateByType.TryGetValue(field.Type, out var valueOfFunc))
            // {
            //     valueObject = valueOfFunc(pField);
            // }
            // else
            // {
            //     valueObject = new Span<byte>(pField, field.Size).ToArray();
            // }
        }

        public Type Type => _type;
        
        public int Size => _size;

        public bool IsVariableSize => _isVariableSize;
        
        public IReadOnlyList<StructFieldInfo> Fields => _fields;

        private StructFieldInfo GetStructFieldInfo(FieldInfo field, HashSet<Type> pendingTypeHandlers)
        {
            var hasFixedBufferAttribute = field.GetCustomAttribute<FixedBufferAttribute>() != null;
            var size = SizeOf(field.FieldType);
            var offset = GetFieldOffset(field);
            var isVariableBuffer = IsVariableBufferField(field, out var variableBufferType);
            var effectiveType = variableBufferType ?? field.FieldType;
            var valueOf = GetValueOfFunc(effectiveType);
            
            StructTypeHandler? handler = ShouldHaveTypeHandler(effectiveType)
                ? GetOrAddHandler(field.FieldType, pendingTypeHandlers)
                : null;

            return new StructFieldInfo(
                Name: field.Name,
                Type: effectiveType,
                Offset: offset,
                Size: size,
                IsVariableBuffer: hasFixedBufferAttribute,
                GetValue: valueOf,
                ValueTypeHandler: handler
            );
        }

        private bool ShouldHaveTypeHandler(Type type)
        {
            return type.IsValueType && !type.IsPrimitive && !type.IsEnum;
        }

        private bool IsVariableBufferField(FieldInfo field, out Type? treatAsType)
        {
            var attribute = field.GetCustomAttribute<FixedBufferAttribute>();
            if (attribute != null)
            {
                treatAsType = attribute.ElementType.MakeArrayType();
                return true;
            }

            treatAsType = null;
            return false;
        }

        // private static int GetRecordSize(Type recordType)
        // {
        //     var sizeOfFunc = GetSizeOfFunc(recordType);
        //     return sizeOfFunc();
        //     
        //     //
        //     // if (!recordType.IsGenericType)
        //     // {
        //     //     return Marshal.SizeOf(recordType);
        //     // }
        //     //
        //     // var optionsAttribute = recordType.GetCustomAttribute<RecordOptionsAttribute>();
        //     // if (optionsAttribute != null && optionsAttribute.SizeOfType != null)
        //     // {
        //     //     return Marshal.SizeOf(optionsAttribute.SizeOfType);
        //     // }
        //     //
        //     // throw new InvalidDataException("Record of generic type must specify SizeOfType in RecordOptionsAttribute");
        // }

        // private int GetFieldSize(FieldInfo info)
        // {
        //     if (info.FieldType.IsClass)
        //     {
        //         return sizeof(IntPtr);
        //     }
        //
        //     var sizeOfFunc = GetSizeOfFunc(info.FieldType);
        //     return sizeOfFunc();
        //
        //     // if (info.FieldType.IsGenericType)
        //     // {
        //     //     return Unsafe.SizeOf<IntMap<StringRecord>>();
        //     // }
        //     //
        //     // return Marshal.SizeOf(info.FieldType);
        // }

        public delegate object? ValueOfFunc(byte* pValue);

        public delegate int VariableSizeGetterFunc(byte* pValue);

        public delegate object? VariableBufferGetterFunc(StructFieldInfo field, byte* pStruct);

        private static readonly MethodInfo _getSizeOfTypeMethodInfo =
            typeof(StructTypeHandler).GetMethod(nameof(GetSizeOfType), BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new Exception("RecordTypeHandler init fail: method 'GetSizeOfType' not found");

        private static readonly MethodInfo _getValueOfTypeMethodInfo =
            typeof(StructTypeHandler).GetMethod(nameof(GetValueOfType), BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new Exception("RecordTypeHandler init fail: method 'GetValueOfType' not found");

        private static readonly MethodInfo _getStructVariableSizeMethodInfo =
            typeof(StructTypeHandler).GetMethod(nameof(GetStructVariableSize), BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new Exception("RecordTypeHandler init fail: method 'GetStructVariableSize' not found");

        private static readonly MethodInfo _getVariableBufferMethodInfo =
            typeof(StructTypeHandler).GetMethod(nameof(GetVariableBufferValue), BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new Exception("RecordTypeHandler init fail: method 'GetVariableBufferValue' not found");

        private static readonly Dictionary<Type, Func<int>> _sizeOfDelegateByType = new();

        private static readonly Dictionary<Type, ValueOfFunc> _valueOfDelegateByType = new();

        private static readonly Dictionary<Type, VariableSizeGetterFunc> _variableSizeGetterByType = new();

        private static readonly Dictionary<Type, VariableBufferGetterFunc> _variableBufferGetterByType = new();

        private static readonly Dictionary<Type, StructTypeHandler> _handlerByType = new();

        public static int SizeOf(Type type)
        {
            if (type.IsClass)
            {
                return sizeof(IntPtr);
            }

            var sizeOfFunc = GetSizeOfFunc(type);
            return sizeOfFunc();
        }

        public static StructTypeHandler Get(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!type.IsValueType)
            {
                throw new ArgumentException("Type must be a struct", nameof(type));
            }

            return GetOrAddHandler(type, pendingHandlerTypes: new HashSet<Type>());
        }

        private static StructTypeHandler GetOrAddHandler(Type type, HashSet<Type> pendingHandlerTypes)
        {
            if (_handlerByType.TryGetValue(type, out var existingHandler))
            {
                return existingHandler;
            }

            if (pendingHandlerTypes.Contains(type))
            {
                throw new NotSupportedException("Recursive struct types are not supported");
            }

            var newHandler = new StructTypeHandler(type, pendingHandlerTypes);
            _handlerByType.Add(type, newHandler);
            return newHandler;
        }
        
        private static Func<int> GetSizeOfFunc(Type type)
        {
            if (_sizeOfDelegateByType.TryGetValue(type, out var existingDelegate))
            {
                return existingDelegate;
            }

            var newDelegate = _getSizeOfTypeMethodInfo.MakeGenericMethod(type).CreateDelegate<Func<int>>();
            _sizeOfDelegateByType.Add(type, newDelegate);
            return newDelegate;
        }

        private static ValueOfFunc GetValueOfFunc(Type type)
        {
            if (_valueOfDelegateByType.TryGetValue(type, out var existingDelegate))
            {
                return existingDelegate;
            }

            var newDelegate = _getValueOfTypeMethodInfo.MakeGenericMethod(type).CreateDelegate<ValueOfFunc>();
            _valueOfDelegateByType.Add(type, newDelegate);
            return newDelegate;
        }

        private static VariableSizeGetterFunc GetVariableSizeGetterFunc(Type type)
        {
            if (_variableSizeGetterByType.TryGetValue(type, out var existingDelegate))
            {
                return existingDelegate;
            }

            var newDelegate = _getStructVariableSizeMethodInfo.MakeGenericMethod(type).CreateDelegate<VariableSizeGetterFunc>();
            _variableSizeGetterByType.Add(type, newDelegate);
            return newDelegate;
        }

        private static VariableBufferGetterFunc GetVariableBufferGetterFunc(Type structType, StructFieldInfo field)
        {
            if (_variableBufferGetterByType.TryGetValue(structType, out var existingDelegate))
            {
                return existingDelegate;
            }

            var elementType = field.Type.GetElementType()
                ?? throw new InvalidOperationException($"Variable buffer element type unknown: struct '{structType.FullName}'");
            
            var newDelegate = _getVariableBufferMethodInfo
                .MakeGenericMethod(structType, elementType)
                .CreateDelegate<VariableBufferGetterFunc>();
            _variableBufferGetterByType.Add(structType, newDelegate);
            return newDelegate;
        }

        private static int GetFieldOffset(FieldInfo info)
        {
            //WARNING: this relies on CLR implementation details
            //https://stackoverflow.com/a/56512720/4544845
            return Marshal.ReadInt32(info.FieldHandle.Value + (4 + IntPtr.Size)) & 0xFFFFFF; 
        }
        
        private static int GetSizeOfType<T>()
        {
            return Unsafe.SizeOf<T>();
        }

        private static object? GetValueOfType<T>(byte *pValue)
        {
            return Unsafe.AsRef<T>(pValue);
        }

        private static int GetStructVariableSize<T>(byte *pStruct)
        {
            ref T instanceRef = ref Unsafe.AsRef<T>(pStruct);
            return ((IVariableSizeRecord) instanceRef!).SizeOf();
        }

        private static object GetVariableBufferValue<TStruct, TElement>(StructFieldInfo field, byte* pStruct)
        {
            ref TStruct instanceRef = ref Unsafe.AsRef<TStruct>(pStruct);
            var totalSize = ((IVariableSizeRecord) instanceRef!).SizeOf();
            
            byte* pField = pStruct + field.Offset;
            var bufferByteSize = totalSize - field.Offset;
            var bufferItemCount = bufferByteSize / Unsafe.SizeOf<TElement>();

            var valueSpan = new Span<TElement>(pField, bufferItemCount);
            return valueSpan.ToArray();
        }
        
        public record StructFieldInfo(
            string Name, 
            Type Type, 
            int Offset, 
            int Size, 
            bool IsVariableBuffer,
            ValueOfFunc GetValue,
            StructTypeHandler? ValueTypeHandler
        );
        
        public record FieldValuePair(
            StructFieldInfo Field, 
            object? Value
        );
    }
}