using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Channels;
using NUnit.Framework;

namespace Atc.Telemetry.Tests.CodePath;

[TestFixture]
public class PerformanceAssumptionsTests
{
    [Test, Category("manual")]
    public void BenchmarkBinaryWriter()
    {
        var channelOfByteArrays = Channel.CreateBounded<byte[]>(capacity: 1000000);  
        var channelOfMemoryStreams = Channel.CreateBounded<MemoryStream>(capacity: 1000000);

        for (int loop = 0; loop < 10; loop++)
        {
            Console.WriteLine($"----------- loop #{loop} ----------");
            
            RunPermutation(nameof(PermutationByteArray), PermutationByteArray);
            RunPermutation(nameof(PermutationByteArrayAndWriteToChannel), PermutationByteArrayAndWriteToChannel);
            RunPermutation(nameof(PermutationMemoryStream), PermutationMemoryStream);
            RunPermutation(nameof(PermutationMemoryStreamAndWriteToChannel), PermutationMemoryStreamAndWriteToChannel);
            RunPermutation(nameof(PermutationBinaryWriter), PermutationBinaryWriter);
            RunPermutation(nameof(PermutationBinaryWriterAndWriteToChannel), PermutationBinaryWriterAndWriteToChannel);
        }

        void RunPermutation(string title, Action code)
        {
            channelOfByteArrays = Channel.CreateBounded<byte[]>(capacity: 1000000);  
            channelOfMemoryStreams = Channel.CreateBounded<MemoryStream>(capacity: 1000000);  

            var clock = Stopwatch.StartNew();
            for (int i = 0 ; i < 10000 ; i++)
            {
                code();
            }

            var elapsed = clock.Elapsed;
            Console.WriteLine($"{title} : {elapsed}");
        }

        void PermutationByteArray()
        {
            var buffer = new byte[1024];
            buffer[0] = 0x0A;
            buffer[1] = 0x0B;
            buffer[2] = 0x0C;
            buffer[3] = 0x0D;
        }

        void PermutationByteArrayAndWriteToChannel()
        {
            var buffer = new byte[1024];
            buffer[0] = 0x0A;
            buffer[1] = 0x0B;
            buffer[2] = 0x0C;
            buffer[3] = 0x0D;

            channelOfByteArrays!.Writer.TryWrite(buffer);
        }

        void PermutationMemoryStream()
        {
            var memoryStream = new MemoryStream(capacity: 1024);
            memoryStream.WriteByte(0x0A);
            memoryStream.WriteByte(0x0B);
            memoryStream.WriteByte(0x0C);
            memoryStream.WriteByte(0x0D);
            memoryStream.Close();
        }

        void PermutationMemoryStreamAndWriteToChannel()
        {
            var memoryStream = new MemoryStream(capacity: 1024);
            memoryStream.WriteByte(0x0A);
            memoryStream.WriteByte(0x0B);
            memoryStream.WriteByte(0x0C);
            memoryStream.WriteByte(0x0D);
            memoryStream.Close();

            channelOfMemoryStreams!.Writer.TryWrite(memoryStream);
        }

        void PermutationBinaryWriter()
        {
            var memoryStream = new MemoryStream(capacity: 1024);
            var binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8, leaveOpen: true);

            binaryWriter.Write(0x0A0B0C0D);
            binaryWriter.Flush();
            binaryWriter.Dispose();
            memoryStream.Close();
        }

        void PermutationBinaryWriterAndWriteToChannel()
        {
            var memoryStream = new MemoryStream(capacity: 1024);
            var binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8, leaveOpen: true);

            binaryWriter.Write(0x0A0B0C0D);
            binaryWriter.Flush();
            binaryWriter.Dispose();
            memoryStream.Close();

            channelOfMemoryStreams!.Writer.TryWrite(memoryStream);
        }
    }
}