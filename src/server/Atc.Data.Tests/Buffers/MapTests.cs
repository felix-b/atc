using System;
using Atc.Data.Buffers;
using Atc.Data.Buffers.Impl;
using FluentAssertions;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Atc.Data.Tests.Buffers
{
    [TestFixture]
    public class MapTests
    {
        [Test]
        public void EmptyMap()
        {
            using var scope = CreateContextScope(out var context);

            ref IntMap<TestItem> map = ref context.AllocateRecord(new TestContainer {
                M = context.AllocateIntMap<TestItem>()
            }).Get().M;

            map.Count.Should().Be(0);
            map.Contains(123).Should().Be(false);
            map.TryGetValue(123).HasValue.Should().Be(false);
        }

        [Test]
        public void CanAddSingleItem()
        {
            using var scope = CreateContextScope(out var context);
            ref IntMap<TestItem> map = ref context.AllocateRecord(new TestContainer {
                M = context.AllocateIntMap<TestItem>()
            }).Get().M;

            map.Add(123, new TestItem() {X = 123.45}); 
            
            map.Count.Should().Be(1);
            map.Contains(123).Should().Be(true);
            map.Contains(456).Should().Be(false);
            map[123].X.Should().Be(123.45);
        }

        [Test]
        public void CanAddMultipleItems()
        {
            using var scope = CreateContextScope(out var context);
            ref IntMap<TestItem> map = ref context.AllocateRecord(new TestContainer {
                M = context.AllocateIntMap<TestItem>()
            }).Get().M;

            map.Add(123, new TestItem() {X = 123.45}); 
            map.Add(456, new TestItem() {X = 456.78}); 
            map.Add(789, new TestItem() {X = 789.01}); 
            
            map.Count.Should().Be(3);
            map.Contains(123).Should().Be(true);
            map.Contains(456).Should().Be(true);
            map.Contains(789).Should().Be(true);
            map[123].X.Should().Be(123.45);
            map[456].X.Should().Be(456.78);
            map[789].X.Should().Be(789.01);
        }

        [Test]
        public void CanAddItemsWithCollisions()
        {
            using var scope = CreateContextScope(out var context);
            ref IntMap<TestItem> map = ref context.AllocateRecord(new TestContainer {
                M = context.AllocateIntMap<TestItem>(bucketCount: 10)
            }).Get().M;

            var buffer = context.GetBuffer<VectorRecord<MapRecordEntry>>();
            
            Console.WriteLine("DUMP #0");
            buffer.DumpToConsole();

            buffer.DumpToDisk(@"D:\0.dump");
            map.Add(5, new TestItem() {X = 55});
            
            Console.WriteLine("DUMP #1");
            buffer.DumpToConsole();

            buffer.DumpToDisk(@"D:\1.dump");
            map.Add(15, new TestItem() {X = 155}); 
            buffer.DumpToDisk(@"D:\2.dump");
            
            Console.WriteLine("DUMP #2");
            buffer.DumpToConsole();
            
            map.Add(30, new TestItem() {X = 333}); 
            buffer.DumpToDisk(@"D:\3.dump");
            map.Add(25, new TestItem() {X = 255}); 
            buffer.DumpToDisk(@"D:\4.dump");
            
            Console.WriteLine("DUMP #3");
            buffer.DumpToConsole();
            
            map.Count.Should().Be(4);
            map.Contains(5).Should().Be(true);

            Console.WriteLine("DUMP #4");
            buffer.DumpToConsole();

            map.Contains(15).Should().Be(true);
            map.Contains(30).Should().Be(true);
            map.Contains(25).Should().Be(true);
            map[5].X.Should().Be(55);
            map[15].X.Should().Be(155);
            map[30].X.Should().Be(333);
            map[25].X.Should().Be(255);
        }
        
        private BufferContextScope CreateContextScope(out IBufferContext context)
        {
            return BufferContextBuilder
                .Begin()
                    .WithTypes<TestContainer, TestItem>()
                    .WithIntMap<TestItem>()
                .End(out context);
        }
        
        public struct TestContainer
        {
            public IntMap<TestItem> M;
        }
        
        public struct TestItem
        {
            public double X;
        }
    }
}