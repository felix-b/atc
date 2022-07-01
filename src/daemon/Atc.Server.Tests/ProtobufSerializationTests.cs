using System.IO;
using FluentAssertions;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Atc.Server.Tests;

[TestFixture]
public class ProtobufSerializationTests
{
    [OneTimeSetUp]
    public void BeforeAll()
    {
    }
    
    [Test]
    public void CanSerializeConcreteRecordType()
    {
        using var stream = new MemoryStream();

        var original = new TestRecordOne(123, "ABC");
        
        Serializer.Serialize(stream, original);
        stream.Seek(0, SeekOrigin.Begin);

        var deserialized = Serializer.Deserialize<TestRecordOne>(stream);

        deserialized.Should().NotBeNull();
        deserialized.Num.Should().Be(123);
        deserialized.Str.Should().Be("ABC");
    }

    [Test]
    public void CanSerializeConcreteNestedRecordTypes()
    {
        using var stream = new MemoryStream();

        var original = new TestRecordTwo(
            One: new TestRecordOne(123, "ABC"), 
            Value: 123.45m);
        
        Serializer.Serialize(stream, original);
        stream.Seek(0, SeekOrigin.Begin);

        var deserialized = Serializer.Deserialize<TestRecordTwo>(stream);

        deserialized.Should().NotBeNull();
        deserialized.One.Should().NotBeNull();
        deserialized.One.Num.Should().Be(123);
        deserialized.One.Str.Should().Be("ABC");
        deserialized.Value.Should().Be(123.45m);
    }

    public record TestRecordBase();
    public record TestRecordOne(int Num, string Str) : TestRecordBase;
    public record TestRecordTwo(TestRecordOne One, decimal Value) : TestRecordBase;
}
