using System;
using System.Buffers;
using System.Text;
using AtcProto;
using FluentAssertions;
using NUnit.Framework;
using ProtoBuf;

namespace Atc.Server.Tests
{
    public class MessageSerializationTests
    {
        [Test]
        public void CanUseArrayBufferWriter()
        {
            var buffer = new ArrayBufferWriter<byte>(initialCapacity: 1024);
            
            new byte[] {
                0x1, 0x2, 0x3, 0x4, 0x5
            }.AsSpan().CopyTo(
                buffer.GetSpan(10)
            );
            
            buffer.Advance(5);
            
            new byte[] {
                0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7
            }.AsSpan().CopyTo(
                buffer.GetSpan(10)
            );

            buffer.Advance(7);

            buffer.WrittenCount.Should().Be(12);
            buffer.WrittenSpan.Length.Should().Be(12);
            buffer.WrittenSpan.ToArray().Should().BeEquivalentTo(new byte[] {
                0x1, 0x2, 0x3, 0x4, 0x5, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7
            });
        }

        [Test]
        public void CanUseArraySegment()
        {
            var buffer = new byte[1024];

            new byte[] {
                0x1, 0x2, 0x3, 0x4, 0x5
            }.AsSpan().CopyTo(
                new ArraySegment<byte>(buffer, 0, 100).AsSpan()
            );
            
            new byte[] {
                0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7
            }.AsSpan().CopyTo(
                new ArraySegment<byte>(buffer, 5, 100).AsSpan()
            );
            
            buffer.AsSpan(0, 12).ToArray().Should().BeEquivalentTo(new byte[] {
                0x1, 0x2, 0x3, 0x4, 0x5, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7
            });
        }

        [Test]
        public void CanSerializeClientToServer()
        {
            var original = new ClientToServer() {
                Id = 123,
                SentAt = new DateTime(2021, 05, 20, 10, 30, 00)
            };
            original.connect = new ClientToServer.Connect() {
                Token = "ABCDEF"
            };

            var writer = new ArrayBufferWriter<byte>(initialCapacity: 4096);
            Serializer.Serialize(writer, original);

            writer.WrittenCount.Should().BeGreaterOrEqualTo(10);
            writer.WrittenCount.Should().BeLessThan(100);
            
            var deserialized = Serializer.Deserialize<ClientToServer>(writer.WrittenSpan);

            deserialized.Id.Should().Be(123);
            deserialized.SentAt.Should().Be(new DateTime(2021, 05, 20, 10, 30, 00));
            deserialized.PayloadCase.Should().Be(ClientToServer.PayloadOneofCase.connect);
            deserialized.connect.Should().NotBeNull();
            deserialized.connect.Token.Should().Be("ABCDEF");
        }

        [Test]
        public void CanSerializeServerToClient()
        {
            var original = new ServerToClient() {
                Id = 123,
                ReplyToRequestId = 456,
                SentAt = new DateTime(2021, 05, 20, 10, 30, 00)
            };
            original.reply_connect = new ServerToClient.ReplyConnect() {
                ServerBanner = "Hello"
            };

            var writer = new ArrayBufferWriter<byte>(initialCapacity: 4096);
            Serializer.Serialize(writer, original);

            writer.WrittenCount.Should().BeGreaterOrEqualTo(10);
            writer.WrittenCount.Should().BeLessThan(100);
            
            var deserialized = Serializer.Deserialize<ServerToClient>(writer.WrittenSpan);

            deserialized.Id.Should().Be(123);
            deserialized.ReplyToRequestId.Should().Be(456);
            deserialized.SentAt.Should().Be(new DateTime(2021, 05, 20, 10, 30, 00));
            deserialized.PayloadCase.Should().Be(ServerToClient.PayloadOneofCase.reply_connect);
            deserialized.reply_connect.Should().NotBeNull();
            deserialized.reply_connect.ServerBanner.Should().Be("Hello");
        }
    }
}