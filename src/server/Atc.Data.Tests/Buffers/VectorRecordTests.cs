using System.IO;
using Atc.Data.Buffers;
using Atc.Data.Buffers.Impl;
using FluentAssertions;
using NUnit.Framework;

namespace Atc.Data.Tests.Buffers
{
    [TestFixture]
    public class VectorRecordTests
    {
        [Test]
        public void EmptyVector()
        {
            using var context = new SingleBufferContext<VectorRecord<TestItem>>();

            var vector = context.AllocateVectorRecord<TestItem>(minBlockEntryCount: 4);

            vector.Get().Count.Should().Be(0);
            vector.Get().BlockEntryCount.Should().Be(4);
            vector.Get().BlockAllocatedEntryCount.Should().Be(0);
            vector.Get().NextBlock.HasValue.Should().BeFalse();
        }

        [Test]
        public void AddItems_SingleBlock_VectorStateUpdated()
        {
            var context = new BufferContext(typeof(VectorRecord<TestItem>), typeof(TestItem));
            using var scope = new BufferContextScope(context);
            
            var vector = context.AllocateVectorRecord<TestItem>(minBlockEntryCount: 4);
            var item1 = context.AllocateRecord<TestItem>(new TestItem {N = 123, X = 111.11});
            var item2 = context.AllocateRecord<TestItem>(new TestItem {N = 456, X = 222.22});
            
            vector.Get().Add(item1);
            vector.Get().Add(item2);
            
            vector.Get().Count.Should().Be(2);
            vector.Get().BlockEntryCount.Should().Be(4);
            vector.Get().BlockAllocatedEntryCount.Should().Be(2);
            vector.Get().NextBlock.HasValue.Should().BeFalse();
        }

        [Test]
        public void AddItems_SingleBlock_BufferRoundtrip()
        {
            var context = new BufferContext(typeof(VectorRecord<TestItem>), typeof(TestItem));
            using var scope = new BufferContextScope(context);
            var vector = context.AllocateVectorRecord<TestItem>(minBlockEntryCount: 4);
            var item1 = context.AllocateRecord<TestItem>(new TestItem {N = 123, X = 111.11});
            var item2 = context.AllocateRecord<TestItem>(new TestItem {N = 456, X = 222.22});
            
            vector.Get().Add(item1);
            vector.Get().Add(item2);

            vector.Get()[0].Get().N.Should().Be(123);
            vector.Get()[0].Get().X.Should().Be(111.11);

            vector.Get()[1].Get().N.Should().Be(456);
            vector.Get()[1].Get().X.Should().Be(222.22);
        }
        
        [Test]
        public void AddItems_MultipleBlocks_VectorStateUpdated()
        {
            var context = new BufferContext(typeof(VectorRecord<TestItem>), typeof(TestItem));
            using var scope = new BufferContextScope(context);
            
            var vector = context.AllocateVectorRecord<TestItem>(minBlockEntryCount: 2);
            var item1 = context.AllocateRecord<TestItem>(new TestItem {N = 123, X = 111.11});
            var item2 = context.AllocateRecord<TestItem>(new TestItem {N = 456, X = 222.22});
            var item3 = context.AllocateRecord<TestItem>(new TestItem {N = 789, X = 333.33});
            
            vector.Get().Add(item1);
            vector.Get().Add(item2);
            vector.Get().Add(item3);
            
            vector.Get().Count.Should().Be(3);
            vector.Get().BlockEntryCount.Should().Be(2);
            vector.Get().BlockAllocatedEntryCount.Should().Be(2);
            vector.Get().NextBlock.HasValue.Should().BeTrue();

            var nextBlock = vector.Get().NextBlock!.Value;
            nextBlock.Get().Count.Should().Be(1);
            nextBlock.Get().BlockEntryCount.Should().Be(4);
            nextBlock.Get().BlockAllocatedEntryCount.Should().Be(1);
            nextBlock.Get().NextBlock.HasValue.Should().BeFalse();
        }

        [Test]
        public void AddItems_MultipleBlocks_BufferRoundtrip()
        {
            var context = new BufferContext(typeof(VectorRecord<TestItem>), typeof(TestItem));
            using var scope = new BufferContextScope(context);
            var vector = context.AllocateVectorRecord<TestItem>(minBlockEntryCount: 2);
            var item1 = context.AllocateRecord<TestItem>(new TestItem {N = 123, X = 111.11});
            var item2 = context.AllocateRecord<TestItem>(new TestItem {N = 456, X = 222.22});
            var item3 = context.AllocateRecord<TestItem>(new TestItem {N = 789, X = 333.33});
            var item4 = context.AllocateRecord<TestItem>(new TestItem {N = 101112, X = 444.44});
            
            vector.Get().Add(item1);
            vector.Get().Add(item2);
            vector.Get().Add(item3);
            vector.Get().Add(item4);

            vector.Get().Count.Should().Be(4);

            vector.Get()[0].Get().N.Should().Be(123);
            vector.Get()[0].Get().X.Should().Be(111.11);

            vector.Get()[1].Get().N.Should().Be(456);
            vector.Get()[1].Get().X.Should().Be(222.22);

            vector.Get()[2].Get().N.Should().Be(789);
            vector.Get()[2].Get().X.Should().Be(333.33);

            vector.Get()[3].Get().N.Should().Be(101112);
            vector.Get()[3].Get().X.Should().Be(444.44);
        }

        [Test]
        public void AddItems_MultipleBlocks_StreamRoundtrip()
        {
            using var stream = new MemoryStream();
            BufferPtr<VectorRecord<TestItem>> vector;
            
            var contextBefore = new BufferContext(typeof(VectorRecord<TestItem>), typeof(TestItem));
            using (new BufferContextScope(contextBefore))
            {
                vector = contextBefore.AllocateVectorRecord<TestItem>(minBlockEntryCount: 2);
                vector.Get().Add(contextBefore.AllocateRecord<TestItem>(new TestItem {N = 123, X = 111.11}));
                vector.Get().Add(contextBefore.AllocateRecord<TestItem>(new TestItem {N = 456, X = 222.22}));
                vector.Get().Add(contextBefore.AllocateRecord<TestItem>(new TestItem {N = 789, X = 333.33}));
                vector.Get().Add(contextBefore.AllocateRecord<TestItem>(new TestItem {N = 101112, X = 444.44}));
                
                contextBefore.WriteTo(stream);
            }

            stream.Position = 0;

            var contextAfter = BufferContext.ReadFrom(stream);
            using (new BufferContextScope(contextAfter))
            {
                vector.Get().Count.Should().Be(4);

                vector.Get()[0].Get().N.Should().Be(123);
                vector.Get()[0].Get().X.Should().Be(111.11);

                vector.Get()[1].Get().N.Should().Be(456);
                vector.Get()[1].Get().X.Should().Be(222.22);

                vector.Get()[2].Get().N.Should().Be(789);
                vector.Get()[2].Get().X.Should().Be(333.33);

                vector.Get()[3].Get().N.Should().Be(101112);
                vector.Get()[3].Get().X.Should().Be(444.44);
            }
        }
        
        public struct TestItem
        {
            public int N;
            public double X;
        }
    }
}