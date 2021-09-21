using System;
using System.Collections.Immutable;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using Zero.Doubt.Logging.Engine;

namespace Zero.Doubt.Logging
{
    public class BinaryLogStream : IDisposable
    {
        private readonly object _syncRoot = new();
        private readonly Stream _output;
        private readonly BinaryWriter _writer;
        private ImmutableDictionary<string, Int32> _keyByString = ImmutableDictionary<string, Int32>.Empty;
        private int _lastStreamId = 0;
        private int _lastStringKey = 0;

        public BinaryLogStream(Stream output)
        {
            _output = output;
            _writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen: true);
        }

        public void Dispose()
        {
            _writer.Dispose();
            _output.Dispose();
        }

        public ILogStreamWriter CreateWriter()
        {
            var streamId = Interlocked.Increment(ref _lastStreamId);
            return new BinaryLogStreamWriter(streamId, this);
        }

        public Int32 GetStringKey(string s)
        {
            if (_keyByString.TryGetValue(s, out var existingKey))
            {
                return existingKey;
            }

            if (!Monitor.TryEnter(_syncRoot, 500))
            {
                Console.WriteLine("!!! BINARY-LOG-STREAM add string key failed: sync root timeout");
                return -1;
            }

            try 
            {
                if (_keyByString.TryGetValue(s, out existingKey))
                {
                    return existingKey;
                }

                var newKey = ++_lastStringKey;
                _keyByString = _keyByString.Add(s, newKey);
                
                _writer.Write((byte)LogStreamOpCode.StringKey);
                _writer.Write(newKey);
                _writer.Write(s);

                return newKey;
            }
            catch (Exception e)
            {
                Console.WriteLine($"!!! BINARY-LOG-STREAM add string key failed: {e}");
                return -2;
            }
            finally
            {
                Monitor.Exit(_syncRoot);
            }
        }
        
        public void Flush(int streamId, MemoryStream buffer)
        {
            if (Monitor.TryEnter(_syncRoot, 500))
            {
                try
                {
                    _output.WriteByte((byte)LogStreamOpCode.BeginStreamChunk);

                    _writer.Write(buffer.Length);
                    buffer.Position = 0;
                    buffer.CopyTo(_output);
                    
                    _writer.Write((byte)LogStreamOpCode.EndStreamChunk);
                    _writer.Flush();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"!!! BINARY-LOG-STREAM write failed: {e}");
                }
                finally
                {
                    Monitor.Exit(_syncRoot);
                }
            }
            else
            {
                Console.WriteLine("!!! BINARY-LOG-STREAM write failed: sync root timeout");
            }
        }

        public static BinaryLogStream Create(string filePath)
        {
            var fileStream = File.Create(filePath);
            return new BinaryLogStream(fileStream);
        }
    }
}
