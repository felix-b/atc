using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Atc.Data.Sources
{
    public static class StreamReaderExtensions
    {
        private delegate bool TryParseFunc<T>(string s, out T value);

        public static bool FastEndOfStream(this StreamReader input)
        {
            return input.Peek() < 0;
        }
        
        public static void SkipToNextLine(this StreamReader input)
        {
            while (!input.FastEndOfStream())
            {
                char c = (char) input.Peek();
                if (c == '\r' || c == '\n')
                {
                    break;
                }
                input.Read();
            }

            while (!input.FastEndOfStream())
            {
                char c = (char) input.Peek();
                if (c != '\r' && c != '\n')
                {
                    break;
                }
                input.Read();
            }
        }

        public static string ReadToEndOfLine(this StreamReader input)
        {
            var result = new StringBuilder(capacity: 255);
            
            while (!input.FastEndOfStream())
            {
                char c = (char) input.Peek();
                if (c == '\r' || c == '\n')
                {
                    break;
                }
                result.Append(c);
                input.Read();
            }

            return result.ToString().Trim();
        }

        public static void Extract<T>(this StreamReader input, out T value, bool crossLine = false)
        {
            ExtractWhitespace(input, includeCrlf: crossLine);
            var extractor = (Extractor<T>) _extractorByType[typeof(T)];
            extractor.Extract(input, out value, throwOnFailure: true);
        }

        public static bool TryExtract<T>(this StreamReader input, out T value, bool crossLine = false)
        {
            ExtractWhitespace(input, includeCrlf: crossLine);
            var extractor = (Extractor<T>) _extractorByType[typeof(T)];
            return extractor.Extract(input, out value, throwOnFailure: false);
        }

        public static void Extract<T1,T2>(this StreamReader input, out T1 value1, out T2 value2)
        {
            Extract<T1>(input, out value1);
            Extract<T2>(input, out value2);
        }

        public static void Extract<T1,T2,T3>(this StreamReader input, out T1 value1, out T2 value2, out T3 value3)
        {
            Extract<T1>(input, out value1);
            Extract<T2>(input, out value2);
            Extract<T3>(input, out value3);
        }

        public static void Extract<T1,T2,T3,T4>(this StreamReader input, out T1 value1, out T2 value2, out T3 value3, out T4 value4)
        {
            Extract<T1>(input, out value1);
            Extract<T2>(input, out value2);
            Extract<T3>(input, out value3);
            Extract<T4>(input, out value4);
        }

        public static void Extract<T1,T2,T3,T4,T5>(this StreamReader input, out T1 value1, out T2 value2, out T3 value3, out T4 value4, out T5 value5)
        {
            Extract<T1>(input, out value1);
            Extract<T2>(input, out value2);
            Extract<T3>(input, out value3);
            Extract<T4>(input, out value4);
            Extract<T5>(input, out value5);
        }

        public static void ExtractWhitespace(this StreamReader input, bool includeCrlf = false)
        {
            char c;
            while ((c = (char)input.Peek()) >= 0)
            {
                var matched =
                    (c == '\x20' || c == '\t') ||
                    (includeCrlf && (c == '\r' || c == '\n'));
                
                if (matched)
                {
                    input.Read();
                }
                else
                {
                    break;
                }
            }
        }

        private static readonly IReadOnlyDictionary<Type, IExtractor> _extractorByType = new Dictionary<Type, IExtractor>() {
            { typeof(Int32), new Int32Extractor() },
            { typeof(Int64), new Int64Extractor() },
            { typeof(float), new FloatExtractor() },
            { typeof(double), new DoubleExtractor() },
            { typeof(string), new StringExtractor() },
        };

        private interface IExtractor
        {
        }
        
        private unsafe class Extractor<T> : IExtractor
        {
            private readonly TryParseFunc<T> _tryParse;
            private readonly Func<char, bool> _charPredicate;
            private readonly int _maxLength;

            public Extractor(TryParseFunc<T> tryParse, Func<char, bool> charPredicate, int maxLength)
            {
                _tryParse = tryParse;
                _charPredicate = charPredicate;
                _maxLength = maxLength;
            }

            public bool Extract(StreamReader input, out T value, bool throwOnFailure = false)
            {
                char* chars = stackalloc char[_maxLength];
                int length = 0;

                while (true)
                {
                    var c = (char)input.Peek();
                    if (c >= 0 && c != '\uffff' && _charPredicate(c))
                    {
                        if (length >= _maxLength)
                        {
                            value = default(T)!;
                            if (throwOnFailure)
                            {
                                throw new InvalidDataException(
                                    $"Error parsing type {typeof(T).Name}: max length exceeded, unexpected char '{c}' after [{new string(chars, 0, length)}]");
                            }
                            return false;
                        }
                        
                        chars[length++] = c;
                        input.Read();
                    }
                    else
                    {
                        break;
                    }
                }

                if (length == 0)
                {
                    value = default(T)!;
                    if (throwOnFailure)
                    {
                        char unexpected = (char)input.Peek();
                        throw new InvalidDataException(
                            $"Error parsing type {typeof(T).Name}: unexpected " +
                            (unexpected >= 0 ? $"character '{unexpected}'" : "end of input"));
                    }
                    return false;
                }

                var s = new string(chars, 0, length);
                var success = _tryParse(s, out value);
                if (!success && throwOnFailure)
                {
                    throw new InvalidDataException($"Could not parse string [{s}] as type {typeof(T).Name}");
                }
                return success;
            }
        }

        private class Int32Extractor : Extractor<Int32>
        {
            public Int32Extractor() 
                : base(
                    tryParse: Int32.TryParse,
                    charPredicate: c => (c >= '0' && c <= '9') || c == '-', 
                    maxLength: 11
                )
            {
            }
        }

        private class Int64Extractor : Extractor<Int64>
        {
            public Int64Extractor() 
                : base(
                    tryParse: Int64.TryParse,
                    charPredicate: c => (c >= '0' && c <= '9') || c == '-', 
                    maxLength: 20
                )
            {
            }
        }

        private class DoubleExtractor : Extractor<double>
        {
            public DoubleExtractor() 
                : base(
                    tryParse: double.TryParse,
                    charPredicate: c => (c >= '0' && c <= '9') || c == '-' || c == '.' || c == 'e', 
                    maxLength: 20
                )
            {
            }
        }

        private class FloatExtractor : Extractor<float>
        {
            public FloatExtractor() 
                : base(
                    tryParse: float.TryParse,
                    charPredicate: c => (c >= '0' && c <= '9') || c == '-' || c == '.' || c == 'e', 
                    maxLength: 20
                )
            {
            }
        }

        private class StringExtractor : Extractor<string>
        {
            public StringExtractor() 
                : base(
                    tryParse: (string s, out string v) => { 
                        v = s;
                        return true; 
                    },
                    charPredicate: c => c != '\x20' && c != '\t' && c != '\r' && c != '\n', 
                    maxLength: 255
                )
            {
            }
        }
    }
}
