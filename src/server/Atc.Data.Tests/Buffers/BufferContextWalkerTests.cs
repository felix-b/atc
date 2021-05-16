using System;
using Atc.Data.Buffers;
using Atc.Data.Buffers.Impl;
using FluentAssertions;
using NUnit.Framework;

namespace Atc.Data.Tests.Buffers
{
    [TestFixture]
    public class BufferContextWalkerTests
    {
        [Test]
        public void CanWalkStringRecord()
        {
            var scope = CreateContextScope(out var context);

            var contextWalker = context.GetWalker();
            var stringWalker = contextWalker.GetBuffer(typeof(StringRecord));

            stringWalker.TypeHandler.Fields.Count.Should().Be(3);
        }
        
        private BufferContextScope CreateContextScope(out BufferContext context)
        {
            IBufferContext contextTemp;
            var scope = BufferContextBuilder
                .Begin()
                    .WithString()
                    .WithTypes<TestContainer, OuterItem, InnerItem>()
                    .WithIntMap<MapBenchmarks.TestItem>()
                .End(out contextTemp);

            context = (BufferContext)contextTemp;
            return scope;
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
            public InnerItem Inner;
            //public BufferPtr<OuterItem> Second;
            public DayOfWeek Day;
        }

        public struct InnerItem
        {
            public double X;
            public double Y;
        }
        
        
    }
}