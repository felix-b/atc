using System;
using FluentAssertions;
using NUnit.Framework;
using Zero.Serialization.Buffers.Impl;

namespace Zero.Serialization.Buffers.Tests
{
    [TestFixture]
    public class BufferContextWalkerTests
    {
        [Test]
        public void EmptyContext()
        {
            var scope = CreateContextScope(out var context);

            var rootWalker = context.GetWalker();
            
            rootWalker.GetBuffer<StringRecord>().Should().NotBeNull();
            rootWalker.GetBuffer<TestContainer>().Should().NotBeNull();
        }

        [Test]
        public void PopulatedContext()
        {
            var scope = CreateContextScope(out var context);
            PopulateContextData(context);
            
            var rootWalker = context.GetWalker();
            
            rootWalker.GetBuffer<StringRecord>().Should().NotBeNull();
            rootWalker.GetBuffer<TestContainer>().Should().NotBeNull();

            HumanReadableTextDump.WriteToFile(context, @"D:\buffers.dump.txt");
        }
        
        private BufferContextScope CreateContextScope(out BufferContext context)
        {
            IBufferContext contextTemp;
            var scope = BufferContextBuilder
                .Begin()
                    .WithString()
                    .WithTypes<TestContainer, OuterItem, InnerItemA>()
                    .WithType<InnerItemB>(alsoAsVectorItem: true)
                    .WithIntMap<OuterItem>()
                .End(out contextTemp);

            context = (BufferContext)contextTemp;
            return scope;
        }

        private void PopulateContextData(BufferContext context)
        {
            var containerPtr = context.AllocateRecord<TestContainer>(new TestContainer() {
                N = 123,
                M = context.AllocateIntMap<OuterItem>(bucketCount: 5)
            });

            ref var outerMap = ref containerPtr.Get().M;
            
            outerMap.Set(111, new OuterItem() {
                Num = 1010,
                BoolOrNull = false,
                Inner = new InnerItemA() {
                    X = 123.4,
                    Y = 567.8
                },
                Day = DayOfWeek.Friday,
                Second = context.AllocateVector<InnerItemB>(new BufferPtr<InnerItemB>[] {
                    context.AllocateRecord<InnerItemB>(new InnerItemB() {
                        S = context.AllocateString("ABC") 
                    }),
                    context.AllocateRecord<InnerItemB>(new InnerItemB() {
                        S = context.AllocateString("DEF") 
                    })
                })
            });

            outerMap.Set(222, new OuterItem() {
                Num = 2020,
                BoolOrNull = null,
                Inner = new InnerItemA() {
                    X = 987.6,
                    Y = 543.2
                },
                Day = DayOfWeek.Monday,
                Second = context.AllocateVector<InnerItemB>(new BufferPtr<InnerItemB>[] {
                    context.AllocateRecord<InnerItemB>(new InnerItemB() {
                        S = context.AllocateString("GHI") 
                    })
                })
            });
        }
        
        public struct TestContainer
        {
            public int N;
            public IntMap<OuterItem> M;
        }

        public struct OuterItem
        {
            public int Num;
            public bool? BoolOrNull;
            public InnerItemA Inner;
            public Vector<InnerItemB>? Second;
            public DayOfWeek Day;
        }

        public struct InnerItemA
        {
            public double X;
            public double Y;
        }
        
        public struct InnerItemB
        {
            public StringRef S { get; init; }
        }
    }
}