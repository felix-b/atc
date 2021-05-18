using System.IO;
using System.Runtime.InteropServices.ComTypes;
using Zero.Serialization.Buffers.Impl;
using NUnit.Framework;
using FluentAssertions;

namespace Zero.Serialization.Buffers.Tests
{
    [TestFixture]
    public class ZStringMapTests
    {
        [Test]
        public void EmptyMap()
        {
            using var scope = CreateContextScope(out var context);

            ref var container = ref AllocateTestContainer(context).Get();
            ref ZStringMapRef<ZRef<TestItem>> map = ref container.M;

            map.Count.Should().Be(0);
            map.Contains(container.White).Should().Be(false);
            map.TryGetValue(container.White, out _).Should().Be(false);
        }

        [Test]
        public void CanAddSingleItem()
        {
            using var scope = CreateContextScope(out var context);
            ref var container = ref AllocateTestContainer(context).Get();
            ref ZStringMapRef<ZRef<TestItem>> map = ref container.M;

            map.Add(container.White, context.AllocateRecord(new TestItem() {X = 123.45})); 
            
            map.Count.Should().Be(1);
            map.Contains(container.White).Should().Be(true);
            map.Contains(container.Red).Should().Be(false);
            map[container.White].Get().X.Should().Be(123.45);
        }

        [Test]
        public void CanAddMultipleItems()
        {
            using var scope = CreateContextScope(out var context);
            ref var container = ref AllocateTestContainer(context).Get();
            ref ZStringMapRef<ZRef<TestItem>> map = ref container.M;

            map.Add(container.Red, context.AllocateRecord(new TestItem() {X = 123.45})); 
            map.Add(container.Green, context.AllocateRecord(new TestItem() {X = 456.78})); 
            map.Add(container.Blue, context.AllocateRecord(new TestItem() {X = 789.01})); 
            
            map.Count.Should().Be(3);
            map.Contains(container.Red).Should().Be(true);
            map.Contains(container.Green).Should().Be(true);
            map.Contains(container.Blue).Should().Be(true);
            map[container.Red].Get().X.Should().Be(123.45);
            map[container.Green].Get().X.Should().Be(456.78);
            map[container.Blue].Get().X.Should().Be(789.01);
        }

        [Test]
        public void CanAddItemsWithCollisions()
        {
            using var scope = CreateContextScope(out var context);
            ref var container = ref AllocateTestContainer(context).Get();
            ref ZStringMapRef<ZRef<TestItem>> map = ref container.M;

            map.Add(container.White, context.AllocateRecord(new TestItem() {X = 55}));
            map.Add(container.Red, context.AllocateRecord(new TestItem() {X = 155})); 
            map.Add(container.Green, context.AllocateRecord(new TestItem() {X = 333})); 
            map.Add(container.Blue, context.AllocateRecord(new TestItem() {X = 255})); 

            map.Count.Should().Be(4);
            map.Contains(container.White).Should().Be(true);
            map.Contains(container.Red).Should().Be(true);
            map.Contains(container.Green).Should().Be(true);
            map.Contains(container.Blue).Should().Be(true);
            map[container.White].Get().X.Should().Be(55);
            map[container.Red].Get().X.Should().Be(155);
            map[container.Green].Get().X.Should().Be(333);
            map[container.Blue].Get().X.Should().Be(255);
        }

        [Test]
        public void DataSurvivesStreamRoundtrip()
        {
            ZRef<TestContainer> containerPtr;
            using var stream = new MemoryStream();
            using (CreateContextScope(out var contextBefore))
            {
                containerPtr = AllocateTestContainer(contextBefore);
                ref var container = ref containerPtr.Get();
                ref ZStringMapRef<ZRef<TestItem>> map = ref container.M;

                //var buffer = contextBefore.GetBuffer<VectorRecord<ZRef<MapRecordEntry>>>();

                map.Add(container.Black,   contextBefore.AllocateRecord(new TestItem() {X = 55}));
                map.Add(container.White,   contextBefore.AllocateRecord(new TestItem() {X = 155}));
                map.Add(container.Red,     contextBefore.AllocateRecord(new TestItem() {X = 333}));
                map.Add(container.Green,   contextBefore.AllocateRecord(new TestItem() {X = 255}));
                map.Add(container.Blue,    contextBefore.AllocateRecord(new TestItem() {X = 444}));
                map.Add(container.Magenta, contextBefore.AllocateRecord(new TestItem() {X = 499}));

                contextBefore.WriteTo(stream);
            }

            stream.Position = 0;

            using (CreateContextScope(stream, out var contextAfter))
            {
                ref var container = ref containerPtr.Get();
                ref ZStringMapRef<ZRef<TestItem>> map = ref container.M;

                map.Count.Should().Be(6);
                map.Contains(container.Black  ).Should().Be(true);
                map.Contains(container.White  ).Should().Be(true);
                map.Contains(container.Red    ).Should().Be(true);
                map.Contains(container.Green  ).Should().Be(true);
                map.Contains(container.Blue   ).Should().Be(true);
                map.Contains(container.Magenta).Should().Be(true);
                map.Contains(container.Cyan   ).Should().Be(false);

                map[container.Black  ].Get().X.Should().Be(55);
                map[container.White  ].Get().X.Should().Be(155);
                map[container.Red    ].Get().X.Should().Be(333);
                map[container.Green  ].Get().X.Should().Be(255);
                map[container.Blue   ].Get().X.Should().Be(444);
                map[container.Magenta].Get().X.Should().Be(499);
            }
        }

        [Test]
        public void CanLookupByStringObject()
        {
            using var scope = CreateContextScope(out var context);
            ref var container = ref AllocateTestContainer(context).Get();
            ref ZStringMapRef<ZRef<TestItem>> map = ref container.M;

            map.Add(container.Green, context.AllocateRecord(new TestItem() {X = 123.45})); 
            map.Add(container.Blue,  context.AllocateRecord(new TestItem() {X = 678.9})); 
            map.Count.Should().Be(2);
            
            map["GREEN"].Get().X.Should().Be(123.45);
            map["BLUE"].Get().X.Should().Be(678.9);
        }

        private ZRef<TestContainer> AllocateTestContainer(BufferContext context)
        {
            return context.AllocateRecord(new TestContainer {
                M = context.AllocateStringMap<ZRef<TestItem>>(),
                Black = context.AllocateString("BLACK"),
                White = context.AllocateString("WHITE"),
                Red = context.AllocateString("RED"),
                Green = context.AllocateString("GREEN"),
                Blue = context.AllocateString("BLUE"),
                Magenta = context.AllocateString("MAGENTA"),
                Cyan = context.AllocateString("CYAN")
            });
        }
        
        private BufferContextScope CreateContextScope(out BufferContext context)
        {
            IBufferContext contextTemp;
            var scope = BufferContextBuilder
                .Begin()
                    .WithString()
                    .WithTypes<TestContainer, TestItem>()
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
            public ZStringRef Black;
            public ZStringRef White;
            public ZStringRef Red;
            public ZStringRef Green;
            public ZStringRef Blue;
            public ZStringRef Magenta;
            public ZStringRef Cyan;
            public ZStringMapRef<ZRef<TestItem>> M;
        }
        
        public struct TestItem
        {
            public double X;
        }
    }
}
