using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Zero.Serialization.Buffers.Impl;

namespace Zero.Serialization.Buffers.Tests
{
    [TestFixture]
    public class VectorRecordTests
    {
        [Test]
        public void EmptyVector()
        {
            var context = new BufferContext(typeof(VectorRecord<TestItem>));
            using var scope = new BufferContextScope(context);
            var vector = context.AllocateVectorRecord<TestItem>(minBlockEntryCount: 4);

            vector.Get().Count.Should().Be(0);
            vector.Get().BlockEntryCount.Should().Be(4);
            vector.Get().BlockAllocatedEntryCount.Should().Be(0);
            vector.Get().NextBlock.HasValue.Should().BeFalse();
        }

        [Test]
        public void AddItems_SingleBlock_VectorStateUpdated()
        {
            var context = new BufferContext(typeof(VectorRecord<TestItem>));
            using var scope = new BufferContextScope(context);
            var vector = context.AllocateVectorRecord<TestItem>(minBlockEntryCount: 4);
            
            vector.Get().Add(new TestItem {N = 123, X = 111.11});
            vector.Get().Add(new TestItem {N = 456, X = 222.22});
            
            vector.Get().Count.Should().Be(2);
            vector.Get().BlockEntryCount.Should().Be(4);
            vector.Get().BlockAllocatedEntryCount.Should().Be(2);
            vector.Get().NextBlock.HasValue.Should().BeFalse();
        }

        [Test]
        public void AddItems_SingleBlock_BufferRoundtrip()
        {
            var context = new BufferContext(typeof(VectorRecord<TestItem>));
            using var scope = new BufferContextScope(context);
            var vector = context.AllocateVectorRecord<TestItem>(minBlockEntryCount: 4);

            vector.Get().Add(new TestItem {N = 123, X = 111.11});
            vector.Get().Add(new TestItem {N = 456, X = 222.22});

            vector.Get()[0].N.Should().Be(123);
            vector.Get()[0].X.Should().Be(111.11);

            vector.Get()[1].N.Should().Be(456);
            vector.Get()[1].X.Should().Be(222.22);
        }
        
        [Test]
        public void AddItems_MultipleBlocks_VectorStateUpdated()
        {
            var context = new BufferContext(typeof(VectorRecord<TestItem>));
            using var scope = new BufferContextScope(context);
            
            var vector = context.AllocateVectorRecord<TestItem>(minBlockEntryCount: 2);

            vector.Get().Add(new TestItem {N = 123, X = 111.11});
            vector.Get().Add(new TestItem {N = 456, X = 222.22});
            vector.Get().Add(new TestItem {N = 789, X = 333.33});
            vector.Get().Add(new TestItem {N = 101112, X = 444.44});
            
            vector.Get().Count.Should().Be(4);
            vector.Get().BlockEntryCount.Should().Be(2);
            vector.Get().BlockAllocatedEntryCount.Should().Be(2);
            vector.Get().NextBlock.HasValue.Should().BeTrue();

            var nextBlock = vector.Get().NextBlock!.Value;
            nextBlock.Get().Count.Should().Be(2);
            nextBlock.Get().BlockEntryCount.Should().Be(4);
            nextBlock.Get().BlockAllocatedEntryCount.Should().Be(2);
            nextBlock.Get().NextBlock.HasValue.Should().BeFalse();
        }

        [Test]
        public void AddItems_MultipleBlocks_BufferRoundtrip()
        {
            var context = new BufferContext(typeof(VectorRecord<TestItem>));
            using var scope = new BufferContextScope(context);

            var vectorPtr = context.AllocateVectorRecord<TestItem>(minBlockEntryCount: 2);
            ref var vector = ref vectorPtr.Get();
           
            vector.Add(new TestItem {N = 123, X = 111.11});
            vector.Add(new TestItem {N = 456, X = 222.22});
            
            ref TestItem item0 = ref vector[0];  
            ref TestItem item1 = ref vector[1];  

            vector.Add(new TestItem {N = 789, X = 333.33});
            vector.Add(new TestItem {N = 101112, X = 444.44});
            vector.Count.Should().Be(4);
            
            ref TestItem item2 = ref vector[2];  
            ref TestItem item3 = ref vector[3];  

            vector[0].N.Should().Be(123);
            vector[0].X.Should().Be(111.11);

           
            vector[1].N.Should().Be(456);
            vector[1].X.Should().Be(222.22);
            
            vector[2].N.Should().Be(789);
            vector[2].X.Should().Be(333.33);
            
            vector[3].N.Should().Be(101112);
            vector[3].X.Should().Be(444.44);
        }

        [Test]
        public void AddItems_MultipleBlocks_StreamRoundtrip()
        {
            using var stream = new MemoryStream();
            ZRef<VectorRecord<TestItem>> vector;
            
            var contextBefore = new BufferContext(typeof(VectorRecord<TestItem>));
            using (new BufferContextScope(contextBefore))
            {
                vector = contextBefore.AllocateVectorRecord<TestItem>(minBlockEntryCount: 2);
                vector.Get().Add(new TestItem {N = 123, X = 111.11});
                vector.Get().Add(new TestItem {N = 456, X = 222.22});
                vector.Get().Add(new TestItem {N = 789, X = 333.33});
                vector.Get().Add(new TestItem {N = 101112, X = 444.44});
                
                contextBefore.WriteTo(stream);
            }

            stream.Position = 0;

            var contextAfter = BufferContext.ReadFrom(stream);
            using (new BufferContextScope(contextAfter))
            {
                vector.Get().Count.Should().Be(4);

                vector.Get()[0].N.Should().Be(123);
                vector.Get()[0].X.Should().Be(111.11);

                vector.Get()[1].N.Should().Be(456);
                vector.Get()[1].X.Should().Be(222.22);

                vector.Get()[2].N.Should().Be(789);
                vector.Get()[2].X.Should().Be(333.33);

                vector.Get()[3].N.Should().Be(101112);
                vector.Get()[3].X.Should().Be(444.44);
            }
        }
 
        [Test]
        public void AddItems_MassiveNumberOfItems_BufferRoundtrip()
        {
            var context = new BufferContext(typeof(VectorRecord<TestItem>), typeof(TestItem));
            using var scope = new BufferContextScope(context);
            var vectorPtr = context.AllocateVectorRecord<TestItem>(minBlockEntryCount: 4);
            
            for (int i = 0; i < 10000; i++)
            {
                // if (i == 4093)
                // {
                //     HumanReadableTextDump.WriteToFile(BufferContext.Current,@"D:\AddItems_MassiveNumberOfItems_BufferRoundtrip-1.dump");
                // }
                
                ref var vector = ref vectorPtr.Get();
                vector.Add(new TestItem {N = i, X = 111.11});

                // if (i == 4093)
                // {
                //     HumanReadableTextDump.WriteToFile(BufferContext.Current,@"D:\AddItems_MassiveNumberOfItems_BufferRoundtrip-2.dump");
                // }
            }

            //vector.Count.Should().Be(5000);

            for (int i = 0; i < 10000; i++)
            {
                ref var vector = ref vectorPtr.Get();
                ref var item = ref vector[i];
                item.N.Should().Be(i);
                item.X.Should().Be(111.11);
            }
        }


        [Test]
        public void EnumerableCursor()
        {
            var context = new BufferContext(typeof(VectorRecord<TestItem>));
            using var scope = new BufferContextScope(context);
            var vectorRef = context.AllocateVectorRecord<TestItem>(minBlockEntryCount: 2);
            ref var vector = ref vectorRef.Get();

            vector.Add(new TestItem {N = 111, X = 111.11});
            vector.Add(new TestItem {N = 222, X = 222.22});
            vector.Add(new TestItem {N = 333, X = 333.33});
            vector.Add(new TestItem {N = 444, X = 444.44});
            vector.Add(new TestItem {N = 555, X = 555.55});
            vector.Add(new TestItem {N = 666, X = 666.66});
            vector.Add(new TestItem {N = 777, X = 777.77});
            
            vector.Should().BeAssignableTo<IEnumerable<ZCursor<TestItem>>>();
            var enumerated = vector.ToArray();

            enumerated.Length.Should().Be(7);
            enumerated[0].Get().N.Should().Be(111);
            enumerated[0].Get().X.Should().Be(111.11);
            enumerated[1].Get().N.Should().Be(222);
            enumerated[1].Get().X.Should().Be(222.22);
            enumerated[2].Get().N.Should().Be(333);
            enumerated[2].Get().X.Should().Be(333.33);
            enumerated[5].Get().N.Should().Be(666);
            enumerated[5].Get().X.Should().Be(666.66);
            enumerated[6].Get().N.Should().Be(777);
            enumerated[6].Get().X.Should().Be(777.77);
        }

        [Test]
        public void EnumerableCursor_EmptyVector()
        {
            var context = new BufferContext(typeof(VectorRecord<TestItem>));
            using var scope = new BufferContextScope(context);
            var vectorRef = context.AllocateVectorRecord<TestItem>(minBlockEntryCount: 2);
            ref var vector = ref vectorRef.Get();

            var enumerated = vector.ToArray();
            enumerated.Length.Should().Be(0);
        }

        public struct TestItem
        {
            public int N;
            public double X;
        }
    }
}