using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using Zero.Serialization.Buffers.Impl;

namespace Zero.Serialization.Buffers.Tests
{
    [TestFixture]
    public class ZIntMapTests
    {
        [Test]
        public void EmptyMap()
        {
            using var scope = CreateContextScope(out var context);

            ref ZIntMapRef<ZRef<TestItem>> map = ref context.AllocateRecord(new TestContainer {
                M = context.AllocateIntMap<ZRef<TestItem>>()
            }).Get().M;

            map.Count.Should().Be(0);
            map.Contains(123).Should().Be(false);
            map.TryGetValue(123, out _).Should().Be(false);
        }

        [Test]
        public void CanAddSingleItem()
        {
            using var scope = CreateContextScope(out var context);
            ref ZIntMapRef<ZRef<TestItem>> map = ref context.AllocateRecord(new TestContainer {
                M = context.AllocateIntMap<ZRef<TestItem>>()
            }).Get().M;

            map.Add(123, context.AllocateRecord(new TestItem() {X = 123.45})); 
            
            map.Count.Should().Be(1);
            map.Contains(123).Should().Be(true);
            map.Contains(456).Should().Be(false);

            map[123].Get().X.Should().Be(123.45);
            map.TryGetValue(123, out var value).Should().Be(true);
            value.Get().X.Should().Be(123.45);
        }

        [Test]
        public void CanAddMultipleItems()
        {
            using var scope = CreateContextScope(out var context);
            ref ZIntMapRef<ZRef<TestItem>> map = ref context.AllocateRecord(new TestContainer {
                M = context.AllocateIntMap<ZRef<TestItem>>()
            }).Get().M;

            map.Add(123, context.AllocateRecord(new TestItem() {X = 123.45})); 
            map.Add(456, context.AllocateRecord(new TestItem() {X = 456.78})); 
            map.Add(789, context.AllocateRecord(new TestItem() {X = 789.01})); 
            
            map.Count.Should().Be(3);
            map.Contains(123).Should().Be(true);
            map.Contains(456).Should().Be(true);
            map.Contains(789).Should().Be(true);
            map[123].Get().X.Should().Be(123.45);
            map[456].Get().X.Should().Be(456.78);
            map[789].Get().X.Should().Be(789.01);
        }

        [Test]
        public void CanAddItemsWithCollisions()
        {
            using var scope = CreateContextScope(out var context);
            ref ZIntMapRef<ZRef<TestItem>> map = ref context.AllocateRecord(new TestContainer {
                M = context.AllocateIntMap<ZRef<TestItem>>(bucketCount: 10)
            }).Get().M;

            //var buffer = context.GetBuffer<VectorRecord<ZRef<MapRecordEntry>>>();
            
            // Console.WriteLine("DUMP #0");
            // buffer.DumpToConsole();

            //buffer.DumpToDisk(@"D:\0.dump");
            map.Add(5, context.AllocateRecord(new TestItem() {X = 55}));
            
            // Console.WriteLine("DUMP #1");
            // buffer.DumpToConsole();

            //buffer.DumpToDisk(@"D:\1.dump");
            map.Add(15, context.AllocateRecord(new TestItem() {X = 155})); 
            //buffer.DumpToDisk(@"D:\2.dump");
            
            // Console.WriteLine("DUMP #2");
            // buffer.DumpToConsole();
            
            map.Add(30, context.AllocateRecord(new TestItem() {X = 333})); 
            //buffer.DumpToDisk(@"D:\3.dump");
            map.Add(25, context.AllocateRecord(new TestItem() {X = 255})); 
            //buffer.DumpToDisk(@"D:\4.dump");
            
            // Console.WriteLine("DUMP #3");
            // buffer.DumpToConsole();
            
            map.Count.Should().Be(4);
            map.Contains(5).Should().Be(true);

            // Console.WriteLine("DUMP #4");
            // buffer.DumpToConsole();

            map.Contains(15).Should().Be(true);
            map.Contains(30).Should().Be(true);
            map.Contains(25).Should().Be(true);
            map[5].Get().X.Should().Be(55);
            map[15].Get().X.Should().Be(155);
            map[30].Get().X.Should().Be(333);
            map[25].Get().X.Should().Be(255);
        }

        [Test]
        public void DataSurvivesStreamRoundtrip()
        {
            ZRef<TestContainer> containerPtr;
            using var stream = new MemoryStream();
            using (CreateContextScope(out var contextBefore))
            {
                containerPtr = contextBefore.AllocateRecord(new TestContainer {
                    M = contextBefore.AllocateIntMap<ZRef<TestItem>>(bucketCount: 10)
                });
                
                ref ZIntMapRef<ZRef<TestItem>> map = ref containerPtr.Get().M;
                //var buffer = contextBefore.GetBuffer<VectorRecord<ZRef<MapRecordEntry>>>();

                map.Add(5,  contextBefore.AllocateRecord(new TestItem() {X = 55}));
                map.Add(15, contextBefore.AllocateRecord(new TestItem() {X = 155}));
                map.Add(30, contextBefore.AllocateRecord(new TestItem() {X = 333}));
                map.Add(25, contextBefore.AllocateRecord(new TestItem() {X = 255}));
                map.Add(40, contextBefore.AllocateRecord(new TestItem() {X = 444}));
                map.Add(49, contextBefore.AllocateRecord(new TestItem() {X = 499}));
                
                contextBefore.WriteTo(stream);
            }

            stream.Position = 0;
            
            using (CreateContextScope(stream, out var contextAfter))
            {
                ref ZIntMapRef<ZRef<TestItem>> map = ref containerPtr.Get().M;

                map.Count.Should().Be(6);
                map.Contains(5).Should().Be(true);
                map.Contains(15).Should().Be(true);
                map.Contains(30).Should().Be(true);
                map.Contains(25).Should().Be(true);
                map.Contains(40).Should().Be(true);
                map.Contains(49).Should().Be(true);
                map.Contains(50).Should().Be(false);

                map[5]. Get().X.Should().Be(55);
                map[15].Get().X.Should().Be(155);
                map[30].Get().X.Should().Be(333);
                map[25].Get().X.Should().Be(255);
                map[40].Get().X.Should().Be(444);
                map[49].Get().X.Should().Be(499);
            }
        }

        [Test]
        public void CanEnumerateKeys()
        {
            using var scope = CreateContextScope(out var context);
            ref ZIntMapRef<ZRef<TestItem>> map = ref context.AllocateRecord(new TestContainer {
                M = context.AllocateIntMap<ZRef<TestItem>>(bucketCount: 10)
            }).Get().M;

            map.Add(12, context.AllocateRecord(new TestItem() {X = 123.45})); 
            map.Add(34, context.AllocateRecord(new TestItem() {X = 456.78})); 
            map.Add(56, context.AllocateRecord(new TestItem() {X = 789.01}));

            int[] enumerated = map.Keys.ToArray();
                
            enumerated.Length.Should().Be(3);
            enumerated[0].Should().Be(12);
            enumerated[1].Should().Be(34);
            enumerated[2].Should().Be(56);
        }

        [Test]
        public void CanEnumerateKeysWithCollisions()
        {
            using var scope = CreateContextScope(out var context);
            ref ZIntMapRef<ZRef<TestItem>> map = ref context.AllocateRecord(new TestContainer {
                M = context.AllocateIntMap<ZRef<TestItem>>(bucketCount: 10)
            }).Get().M;

            map.Add(34, context.AllocateRecord(new TestItem() {X = 333.3}));
            map.Add(24, context.AllocateRecord(new TestItem() {X = 222.2})); 
            map.Add(12, context.AllocateRecord(new TestItem() {X = 111.1})); 
            map.Add(44, context.AllocateRecord(new TestItem() {X = 444.4}));
            map.Add(55, context.AllocateRecord(new TestItem() {X = 555.5}));

            int[] enumerated = map.Keys.ToArray();
                
            enumerated.Length.Should().Be(5);
            enumerated[0].Should().Be(12);
            enumerated[1].Should().Be(34);
            enumerated[2].Should().Be(24);
            enumerated[3].Should().Be(44);
            enumerated[4].Should().Be(55);
        }

        [Test]
        public void CanEnumerateValues()
        {
            using var scope = CreateContextScope(out var context);
            ref ZIntMapRef<ZRef<TestItem>> map = ref context.AllocateRecord(new TestContainer {
                M = context.AllocateIntMap<ZRef<TestItem>>()
            }).Get().M;

            var ptr1 = context.AllocateRecord(new TestItem() {X = 123.45});
            var ptr2 = context.AllocateRecord(new TestItem() {X = 456.78});
            var ptr3 = context.AllocateRecord(new TestItem() {X = 789.01});
            
            map.Add(123, ptr1); 
            map.Add(456, ptr2); 
            map.Add(789, ptr3);

            ZCursor<ZRef<TestItem>>[] enumerated = map.Values.ToArray();
                
            enumerated.Length.Should().Be(3);
            enumerated[0].Get().Should().Be(ptr1);
            enumerated[1].Get().Should().Be(ptr2);
            enumerated[2].Get().Should().Be(ptr3);
        }

        [Test]
        public void CanEnumerateValuesWithCollisions()
        {
            using var scope = CreateContextScope(out var context);
            ref ZIntMapRef<ZRef<TestItem>> map = ref context.AllocateRecord(new TestContainer {
                M = context.AllocateIntMap<ZRef<TestItem>>(bucketCount: 10)
            }).Get().M;

            var ptr1 = context.AllocateRecord(new TestItem() {X = 111.1});
            var ptr2 = context.AllocateRecord(new TestItem() {X = 222.2});
            var ptr3 = context.AllocateRecord(new TestItem() {X = 333.3});
            var ptr4 = context.AllocateRecord(new TestItem() {X = 444.4});
            var ptr5 = context.AllocateRecord(new TestItem() {X = 555.5});
            
            map.Add(34, ptr3);
            map.Add(24, ptr2); 
            map.Add(12, ptr1); 
            map.Add(44, ptr4);
            map.Add(55, ptr5);

            ZCursor<ZRef<TestItem>>[] enumerated = map.Values.ToArray();
                
            enumerated.Length.Should().Be(5);
            enumerated[0].Get().Should().Be(ptr1);
            enumerated[1].Get().Should().Be(ptr3);
            enumerated[2].Get().Should().Be(ptr2);
            enumerated[3].Get().Should().Be(ptr4);
            enumerated[4].Get().Should().Be(ptr5);
        }
        
        private BufferContextScope CreateContextScope(out BufferContext context)
        {
            IBufferContext contextTemp;
            var scope = BufferContextBuilder
                .Begin()
                    .WithTypes<TestContainer, TestItem>(alsoAsVectorItem: true)
                    .WithMapTo<ZRef<TestItem>>()
                .End(out contextTemp);

            context = (BufferContext)contextTemp;
            return scope;
        }

        private BufferContextScope CreateContextScope(Stream stream, out BufferContext context)
        {
            context = BufferContext.ReadFrom(stream);
            return new BufferContextScope(context);
        }

        public struct TestContainer
        {
            public ZIntMapRef<ZRef<TestItem>> M;
        }
        
        public struct TestItem
        {
            public double X;
        }
    }
}