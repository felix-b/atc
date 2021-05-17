using System.IO;
using FluentAssertions;
using NUnit.Framework;
using Zero.Serialization.Buffers.Impl;

namespace Zero.Serialization.Buffers.Tests
{
    [TestFixture]
    public unsafe class StringRecordTests
    {
        [Test]
        public void BufferRoundtripTest()
        {
            using var context = new SingleBufferContext<StringRecord>();

            var ptr1 = StringRecord.Allocate("abc");
            var ptr2 = StringRecord.Allocate("longer-text");
            var ptr3 = StringRecord.Allocate("");
            var ptr4 = StringRecord.Allocate("zzzz");

            ptr1.Get().Str.Should().Be("abc");
            ptr2.Get().Str.Should().Be("longer-text");
            ptr3.Get().Str.Should().Be("");
            ptr4.Get().Str.Should().Be("zzzz");
        }
        
        [Test]
        public void StreamRoundtripTest()
        {
            using var stream = new MemoryStream();
            BufferPtr<StringRecord> ptr1, ptr2, ptr3, ptr4;
            
            using (var context = new SingleBufferContext<StringRecord>())
            {
                ptr1 = StringRecord.Allocate("abc");
                ptr2 = StringRecord.Allocate("longer-text");
                ptr3 = StringRecord.Allocate("");
                ptr4 = StringRecord.Allocate("zzzz");
                
                context.Buffer.WriteTo(stream);
            }

            stream.Position = 0;
            
            using (var context = new SingleBufferContext<StringRecord>(stream))
            {
                ptr1.Get().Str.Should().Be("abc");
                ptr2.Get().Str.Should().Be("longer-text");
                ptr3.Get().Str.Should().Be("");
                ptr4.Get().Str.Should().Be("zzzz");
            }
        }
    }
}