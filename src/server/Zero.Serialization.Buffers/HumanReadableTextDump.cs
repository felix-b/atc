using System;
using System.Collections.Generic;
using System.IO;
using Zero.Serialization.Buffers.Impl;

namespace Zero.Serialization.Buffers
{
    public class HumanReadableTextDump
    {
        private readonly BufferContextWalker _walker;

        public HumanReadableTextDump(BufferContextWalker walker)
        {
            _walker = walker;
        }

        public void WriteTo(StreamWriter output)
        {
            output.WriteLine($"######################## BEGIN BUFFER CONTEXT ########################");
            
            foreach (var type in _walker.RecordTypes)
            {
                output.WriteLine();
                output.WriteLine($"============ BUFFER [{type.FullName}] ============");
                output.WriteLine();

                var buffer = _walker.GetBuffer(type);
                WriteBuffer(buffer, output);
            }

            output.WriteLine();
            output.WriteLine($"######################## END BUFFER CONTEXT ########################");
            output.Flush();
        }

        public void WriteBuffer(BufferContextWalker.BufferWalker buffer, StreamWriter output)
        {
            for (int i = 0 ; i < buffer.RecordCount ; i++)
            {
                var record = buffer.GetRecord(i);
                var byteSize = record.GetByteSize();
                var address = record.GetAbsoluteAddress();
                
                output.WriteLine($"------------ record [{i}/{buffer.RecordCount-1}] @ byte[0x{record.Offset:X4}] ram[0x{address:X8}], {byteSize} bytes ------------");
                WriteRecord(record, output);
            }
        }

        public void WriteRecord(BufferContextWalker.RecordWalker record, StreamWriter output)
        {
            void AppendValues(StructTypeHandler.FieldValuePair[] pairs, int indentLevel)
            {
                foreach (var pair in pairs)
                {
                    var indentString = GetIndentString(indentLevel); 
                    output.Write($"{indentString}{pair.Field.Name}: ");
                    
                    if (pair.Value is StructTypeHandler.FieldValuePair[] subPairs)
                    {
                        output.WriteLine($"/* struct {pair.Field.Type.Name} */ {{");
                        AppendValues(subPairs, indentLevel + 1);
                        output.WriteLine($"{indentString}}}");
                    }
                    else
                    {
                        WriteValue(pair, indentLevel, output);
                        output.WriteLine();
                    }
                }
            }
                
            output.WriteLine($"/* struct {record.TypeHandler.Type.Name} */ {{");
            AppendValues(record.GetFieldValues(), indentLevel: 1);
            output.WriteLine("}");
        }

        public BufferContextWalker Walker => _walker;

        private delegate void ValueWriterFunc(StructTypeHandler.FieldValuePair pair, int indentLevel, StreamWriter output);
        
        private static readonly Dictionary<Type, ValueWriterFunc> _valueWriterByType = new() {
            [typeof(string)] = WriteStringValue,
            [typeof(sbyte)] = WriteNumberValue<sbyte>,
            [typeof(Int16)] = WriteNumberValue<Int16>,
            [typeof(Int32)] = WriteNumberValue<Int32>,
            [typeof(Int64)] = WriteNumberValue<Int64>,
            [typeof(byte)] = WriteNumberValue<byte>,
            [typeof(UInt16)] = WriteNumberValue<UInt16>,
            [typeof(UInt32)] = WriteNumberValue<UInt32>,
            [typeof(UInt64)] = WriteNumberValue<UInt64>,
            [typeof(sbyte[])] = WriteArrayValue<sbyte>,
            [typeof(Int16[])] = WriteArrayValue<Int16>,
            [typeof(Int32[])] = WriteArrayValue<Int32>,
            [typeof(Int64[])] = WriteArrayValue<Int64>,
            [typeof(byte[])] = WriteArrayValue<byte>,
            [typeof(UInt16[])] = WriteArrayValue<UInt16>,
            [typeof(UInt32[])] = WriteArrayValue<UInt32>,
            [typeof(UInt64[])] = WriteArrayValue<UInt64>,
            [typeof(char[])] = WriteArrayValue<char>,
        };

        public static void WriteToFile(IBufferContext context, string filePath)
        {
            var dump = new HumanReadableTextDump(context.GetWalker());
            using var file = File.CreateText(filePath);
            dump.WriteTo(file);
        }
        
        private static string GetIndentString(int level, int suffixLength = 0)
        {
            return new string(' ', level * 3 + suffixLength);
        }

        private static string GetHexFormatString<T>()
        {
            return $"{{0:X{StructTypeHandler.SizeOf(typeof(T)) * 2}}}";
        }

        private static void WriteNumberValue<T>(StructTypeHandler.FieldValuePair pair, int indentLevel, StreamWriter output)
        {
            T number = (T) pair.Value!;
            var formatString = GetHexFormatString<T>();
            output.Write($"0x{string.Format(formatString, number)}");
        }


        private static void WriteArrayValue<T>(StructTypeHandler.FieldValuePair pair, int indentLevel, StreamWriter output)
        {
            var array = (T[]) pair.Value!;
            var hexFormatString = GetHexFormatString<T>();
            var itemIndentString = GetIndentString(indentLevel + 1);

            string formatAsNumber<N>(N value)
            {
                var valueString = string.Format(hexFormatString!, value);
                return $"0x{valueString}";
            }

            string formatAsChar(T value)
            {
                char c = (char)(object)value!;
                int code = c;
                return code >= 0x20
                    ? $"{formatAsNumber<int>(code)}({c})"
                    : $"{formatAsNumber<int>(code)}( )";
            }

            Func<T, string> formatItem = typeof(T) == typeof(char)
                ? formatAsChar
                : formatAsNumber<T>;
            
            output.Write($"/* {typeof(T).Name}[{array.Length}] */ [ ");

            for (int i = 0 ; i < array.Length ; i++)
            {
                if ((i % 8) == 0)
                {
                    output.WriteLine();
                    output.Write($"{itemIndentString}/* 0x{i:X4} */ ");
                }
                else if ((i % 8) == 4)
                {
                    output.Write("   ");
                }

                output.Write(formatItem(array[i]));

                if (i < array.Length - 1)
                {
                    output.Write(", ");
                }
            }

            output.WriteLine();
            output.Write(GetIndentString(indentLevel) + "]");
        }

        private static void WriteStringValue(StructTypeHandler.FieldValuePair pair, int indentLevel, StreamWriter output)
        {
            var s = (string)pair.Value!;
            var escaped = s
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
            output.Write("\"" + escaped + "\"");
        }
        
        private static void WriteValue(StructTypeHandler.FieldValuePair pair, int indentLevel, StreamWriter output)
        {
            var value = pair.Value;
            if (value == null)
            {
                output.Write("null");
                return;
            }

            if (_valueWriterByType.TryGetValue(value.GetType(), out var valueWriter))
            {
                valueWriter(pair, indentLevel, output);
            }
            else
            {
                output.Write($"{value}");
            }
        }
    }
}