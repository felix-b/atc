using System.IO;
using Zero.Serialization.Buffers.Impl;
using FluentAssertions;
using NUnit.Framework;

namespace Zero.Serialization.Buffers.Tests
{
    [TestFixture]
    public class BufferContextTests
    {
        [Test]
        public void AllocatedStringRecordsAreUnique()
        {
            using var scope = BufferContextBuilder.Begin().WithString().End(out var context);

            var stringRef1 = context.AllocateString("ABC");
            var stringRef2 = context.AllocateString("DEF");
            var stringRef3 = context.AllocateString("ABC");
            var stringRef4 = context.AllocateString("DEF");

            context.GetBuffer<StringRecord>().RecordCount.Should().Be(2);
            stringRef3.ByteIndex.Should().Be(stringRef1.ByteIndex);
            stringRef4.ByteIndex.Should().Be(stringRef2.ByteIndex);
        }

        [Test]
        public void CanGetStringRefBeforeHibernation()
        {
            using var scope = BufferContextBuilder.Begin().WithString().End(out var context);
            
            var refAbc = context.AllocateString("ABC");
            var refDef = context.AllocateString("DEF");

            context.GetString("ABC").Should().Be(refAbc);
            context.GetString("ABC").Should().NotBe(refDef);
            context.GetString("DEF").Should().Be(refDef);
            context.GetString("DEF").Should().NotBe(refAbc);
        }

        [Test]
        public void CanGetStringRefAfterHibernation()
        {
            ZStringRef refAbc, refDef;

            using var stream = new MemoryStream();
            using (BufferContextBuilder.Begin().WithString().End(out var contextBefore))
            {
                refAbc = contextBefore.AllocateString("ABC");
                refDef = contextBefore.AllocateString("DEF");
                
                ((BufferContext)contextBefore).WriteTo(stream);    
            }

            stream.Position = 0;

            var contextAfter = BufferContext.ReadFrom(stream);
            using (new BufferContextScope(contextAfter))
            {
                contextAfter.GetString("ABC").Should().Be(refAbc);
                contextAfter.GetString("ABC").Should().NotBe(refDef);
                contextAfter.GetString("DEF").Should().Be(refDef);
                contextAfter.GetString("DEF").Should().NotBe(refAbc);
            }
        }

        [Test]
        public void StructsWithMutualReferences_()
        {
            using var scope = BufferContextBuilder
                .Begin()
                    .WithType<AParentStruct>()
                    .WithType<AChildStruct>(alsoAsVectorItem: true)
                .End(out var context);
            
            var parentRef = context.AllocateRecord<AParentStruct>(new AParentStruct() {
                Children = context.AllocateVector<ZRef<AChildStruct>>()
            });
            var childRef1 = context.AllocateRecord<AChildStruct>(new AChildStruct() {
                Parent = parentRef
            });
            var childRef2 = context.AllocateRecord<AChildStruct>(new AChildStruct() {
                Parent = parentRef
            });
            parentRef.Get().Children.Add(childRef1);
            parentRef.Get().Children.Add(childRef2);

            ref var parent = ref parentRef.Get();
            parent.Children.Count.Should().Be(2);
            parent.Children[0].Should().Be(childRef1);
            parent.Children[0].Get().Parent.AsZRef<AParentStruct>().Should().Be(parentRef);
            parent.Children[1].Should().Be(childRef2);
            parent.Children[1].Get().Parent.AsZRef<AParentStruct>().Should().Be(parentRef);
        }

        public struct AParentStruct
        {
            public ZVectorRef<ZRef<AChildStruct>> Children { get; init; }
        }

        public struct AChildStruct
        {
            // cannot put ZRef<AParentStruct> here - type cycles not allowed in structs (C#/CLR limitation) 
            public ZRefAny Parent { get; init; } 
        }
    }
}