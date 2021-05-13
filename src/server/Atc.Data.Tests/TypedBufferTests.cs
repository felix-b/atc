using System;
using System.IO;
using System.Runtime.CompilerServices;
using FluentAssertions;
using NUnit.Framework;

namespace Atc.Data.Tests
{
    [TestFixture]
    public unsafe class TypedBufferTests
    {
        [Test]
        public void InitiallyEmpty()
        {
            using var context = new SingleBufferContext<ASimpleRecord>(capacity: 10);

            context.Buffer.ReadOnly.Should().BeFalse();
            context.Buffer.Capacity.Should().Be(10);
            context.Buffer.RecordCount.Should().Be(0);
            context.Buffer.TotalBytes.Should().Be(10 * sizeof(ASimpleRecord));
            context.Buffer.AllocatedBytes.Should().Be(0);
        }

        [Test]
        public void AllocateSingleRecord_StateUpdated()
        {
            using var context = new SingleBufferContext<ASimpleRecord>(capacity: 10);
            
            context.Buffer.Allocate();
            
            context.Buffer.Capacity.Should().Be(10);
            context.Buffer.RecordCount.Should().Be(1);
            context.Buffer.TotalBytes.Should().Be(10 * sizeof(ASimpleRecord));
            context.Buffer.AllocatedBytes.Should().Be(sizeof(ASimpleRecord));
        }

        [Test]
        public void BufferOverflow_ReallocatedToLargerBuffer()
        {
            using var context = new SingleBufferContext<ASimpleRecord>(capacity: 3);
            context.Buffer.Allocate();
            context.Buffer.Allocate();
            context.Buffer.Allocate();
            
            context.Buffer.Capacity.Should().Be(3);
            context.Buffer.RecordCount.Should().Be(3);
            context.Buffer.TotalBytes.Should().Be(3 * sizeof(ASimpleRecord));
            context.Buffer.AllocatedBytes.Should().Be(3 * sizeof(ASimpleRecord));
            
            context.Buffer.Allocate();

            context.Buffer.Capacity.Should().Be(3);
            context.Buffer.RecordCount.Should().Be(4);
            context.Buffer.TotalBytes.Should().Be(6 * sizeof(ASimpleRecord));
            context.Buffer.AllocatedBytes.Should().Be(4 * sizeof(ASimpleRecord));
        }

        [Test]
        public void AllocateSingleRecord_ValuesZeroed()
        {
            using var context = new SingleBufferContext<ASimpleRecord>(capacity: 10);
            
            var ptr = context.Buffer.Allocate();
            
            ref ASimpleRecord record = ref ptr.Get();
            record.N.Should().Be(0);
            record.B.Should().Be(false);
            record.X.Should().Be(0);
        }
        
        [Test]
        public void ScalarsSurviveBufferRoundtrip()
        {
            using var context = new SingleBufferContext<ASimpleRecord>(capacity: 10);
            var ptr = context.Buffer.Allocate();

            ptr.Get().N = 123;
            ptr.Get().B = true;
            ptr.Get().X = 456.78;
            
            ptr.Get().N.Should().Be(123);
            ptr.Get().B.Should().Be(true);
            ptr.Get().X.Should().Be(456.78);
        }

        [Test]
        public void AllocateMultipleRecords_StateUpdated()
        {
            using var context = new SingleBufferContext<ASimpleRecord>(capacity: 10);
            var ptr1 = context.Buffer.Allocate();
            var ptr2 = context.Buffer.Allocate();
            var ptr3 = context.Buffer.Allocate();

            ptr2.ByteIndex.Should().Be(ptr1.ByteIndex + sizeof(ASimpleRecord));
            ptr3.ByteIndex.Should().Be(ptr2.ByteIndex + sizeof(ASimpleRecord));

            context.Buffer.Capacity.Should().Be(10);
            context.Buffer.RecordCount.Should().Be(3);
            context.Buffer.TotalBytes.Should().Be(10 * sizeof(ASimpleRecord));
            context.Buffer.AllocatedBytes.Should().Be(3 * sizeof(ASimpleRecord));
        }

        [Test]
        public void AllocateMultipleRecords_ValuesZeroed()
        {
            using var context = new SingleBufferContext<ASimpleRecord>(capacity: 10);
            var ptr1 = context.Buffer.Allocate();
            var ptr2 = context.Buffer.Allocate();
            var ptr3 = context.Buffer.Allocate();

            ptr1.Get().N.Should().Be(0);
            ptr1.Get().B.Should().Be(false);
            ptr1.Get().X.Should().Be(0);

            ptr2.Get().N.Should().Be(0);
            ptr2.Get().B.Should().Be(false);
            ptr2.Get().X.Should().Be(0);

            ptr3.Get().N.Should().Be(0);
            ptr3.Get().B.Should().Be(false);
            ptr3.Get().X.Should().Be(0);
        }

        [Test]
        public void StreamRoundtrip_BufferStateSurvived()
        {
            using var stream = new MemoryStream();
            BufferPtr<ASimpleRecord> ptr1;
            void* rawPtr1Before;
            
            using (var context = new SingleBufferContext<ASimpleRecord>(capacity: 10))
            {
                ptr1 = context.Buffer.Allocate();
                context.Buffer.Allocate();
                context.Buffer.Allocate();

                rawPtr1Before = Unsafe.AsPointer(ref ptr1.Get());

                context.Buffer.WriteTo(stream);
            }

            stream.Length.Should().BeGreaterThan(3 * sizeof(ASimpleRecord));
            stream.Position = 0;

            using (var context = new SingleBufferContext<ASimpleRecord>(stream))
            {
                context.Buffer.ReadOnly.Should().BeTrue();
                context.Buffer.Capacity.Should().Be(3);
                context.Buffer.RecordCount.Should().Be(3);
                context.Buffer.TotalBytes.Should().Be(3 * sizeof(ASimpleRecord));
                context.Buffer.AllocatedBytes.Should().Be(3 * sizeof(ASimpleRecord));

                void* rawPtr1After = Unsafe.AsPointer(ref ptr1.Get());
                (rawPtr1After != rawPtr1Before).Should().BeTrue();
            }
        }

        [Test]
        public void StreamRoundtrip_ScalarValuesSurvived()
        {
            using var stream = new MemoryStream();
            BufferPtr<ASimpleRecord> ptr1, ptr2, ptr3;
            
            using (var context = new SingleBufferContext<ASimpleRecord>(capacity: 10))
            {
                ptr1 = context.Buffer.Allocate();
                ptr2 = context.Buffer.Allocate();
                ptr3 = context.Buffer.Allocate();

                ptr1.Get().N = 123;
                ptr1.Get().B = true;
                ptr1.Get().X = 111.22;

                ptr2.Get().N = 456;
                ptr2.Get().B = false;
                ptr2.Get().X = 333.44;

                ptr3.Get().N = 789;
                ptr3.Get().B = true;
                ptr3.Get().X = 555.66;
                
                context.Buffer.WriteTo(stream);
            }

            stream.Position = 0;

            using (var context = new SingleBufferContext<ASimpleRecord>(stream))
            {
                ptr1.Get().N.Should().Be(123);
                ptr1.Get().B.Should().Be(true);
                ptr1.Get().X.Should().Be(111.22);

                ptr2.Get().N.Should().Be(456);
                ptr2.Get().B.Should().Be(false);
                ptr2.Get().X.Should().Be(333.44);

                ptr3.Get().N.Should().Be(789);
                ptr3.Get().B.Should().Be(true);
                ptr3.Get().X.Should().Be(555.66);
            }
        }
        
        public struct ASimpleRecord
        {
            public int N;
            public bool B;
            public double X;
        }
    }
}