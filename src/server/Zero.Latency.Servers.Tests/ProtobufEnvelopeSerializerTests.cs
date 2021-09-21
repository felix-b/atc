using System;
using System.Buffers;
using FluentAssertions;
using NUnit.Framework;
using ProtoBuf;
using TestProto;

namespace Zero.Latency.Servers.Tests
{
    [TestFixture]
    public class ProtobufEnvelopeSerializerTests
    {
        private readonly IEndpointLogger _logger = new NoopEndpointLogger();

        [Test]
        public void CanDeserializeClientToServer()
        {
            var original = new TestClientToServer() {
                Id = 123,
                hello = new() { Query = "ABC"}
            };

            var writer = new ArrayBufferWriter<byte>(initialCapacity: 4096);
            Serializer.Serialize(writer, original);
            var messageBytes = writer.WrittenSpan.ToArray();
            var serializerUnderTest = new ProtobufEnvelopeSerializer<TestClientToServer>(_logger);

            var deserialized = (TestClientToServer)serializerUnderTest.DeserializeIncomingEnvelope(
                new ArraySegment<byte>(messageBytes, 0, messageBytes.Length)
            ); 
                
            deserialized.Id.Should().Be(123);
            deserialized.PayloadCase.Should().Be(TestClientToServer.PayloadOneofCase.hello);
            deserialized.hello.Should().NotBeNull();
            deserialized.hello.Query.Should().Be("ABC");
        }

        [Test]
        public void CanSerializeServerToClient()
        {
            var original = new TestServerToClient() {
                Id = 123,
                Show = new() {
                    Data = "XYZ"
                }
            };

            var writer = new ArrayBufferWriter<byte>(initialCapacity: 4096);
            var serializerUnderTest = new ProtobufEnvelopeSerializer<TestClientToServer>(_logger);
            serializerUnderTest.SerializeOutgoingEnvelope(original, writer);

            var deserialized = Serializer.Deserialize<TestServerToClient>(writer.WrittenSpan);

            deserialized.Id.Should().Be(123);
            deserialized.PayloadCase.Should().Be(TestServerToClient.PayloadOneofCase.Show);
            deserialized.Show.Should().NotBeNull();
            deserialized.Show.Data.Should().Be("XYZ");
        }    
    }
}
