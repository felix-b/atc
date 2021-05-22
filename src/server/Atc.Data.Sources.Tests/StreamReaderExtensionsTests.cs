using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace Atc.Data.Sources.Tests
{
    [TestFixture]
    public class StreamReaderExtensionsTests
    {
        [Test]
        public void CanExtractInt32()
        {
            using var reader = ReadInputStream(
                contents: "XYZ 12345 OPQ", 
                skipToPosition: 4 
            );

            reader.Extract(out int value);

            value.Should().Be(12345);
            reader.ReadToEnd().Should().Be(" OPQ");
        }

        [Test]
        public void CanExtractString()
        {
            using var reader = ReadInputStream(
                contents: "ABD this;is,some.str-ing VALUE", 
                skipToPosition: 4 
            );

            reader.Extract(out string value);

            value.Should().Be("this;is,some.str-ing");
            reader.ReadToEnd().Should().Be(" VALUE");
        }

        [Test]
        public void CanSkipLeadingWhitespaceBeforeNumericValue()
        {
            using var reader = ReadInputStream(
                contents: "ABC  \t 12345 \t VALUE", 
                skipToPosition: 4 
            );

            reader.Extract(out int value);

            value.Should().Be(12345);
            reader.ReadToEnd().Should().Be(" \t VALUE");
        }

        [Test]
        public void CanSkipLeadingWhitespaceBeforeStringValue()
        {
            using var reader = ReadInputStream(
                contents: "ABC  \t the-string \t VALUE", 
                skipToPosition: 4 
            );

            reader.Extract(out string value);

            value.Should().Be("the-string");
            reader.ReadToEnd().Should().Be(" \t VALUE");
        }

        [Test]
        public void CanSkipNewLinesAsLeadingWhitespace()
        {
            using var reader = ReadInputStream(
                contents: "ABC  \t\r\n\t\n \n \r\n \t 12345\t THE-END", 
                skipToPosition: 4 
            );

            reader.Extract(out int value);

            value.Should().Be(12345);
            reader.ReadToEnd().Should().Be("\t THE-END");
        }

        [Test]
        public void CanReadToEndOfLine()
        {
            using var reader = ReadInputStream(
                contents: "FIRST_LINE\r\nSECOND_LINE", 
                skipToPosition: 5 
            );

            var value = reader.ReadToEndOfLine();

            value.Should().Be("_LINE");
            reader.ReadToEnd().Should().Be("\r\nSECOND_LINE");
        }

        [Test]
        public void ReadToEndOfLine_TrimLeadingAndTrailingWhitespace()
        {
            using var reader = ReadInputStream(
                contents: "First \t line to end \t \r\nSecond line to end", 
                skipToPosition: 5 
            );

            var value = reader.ReadToEndOfLine();

            value.Should().Be("line to end");
            reader.ReadToEnd().Should().Be("\r\nSecond line to end");
        }

        [Test]
        public void CanSkipToNextLine()
        {
            using var reader = ReadInputStream(
                contents: "First line \t \r\nSecond line", 
                skipToPosition: 3 
            );

            reader.SkipToNextLine();

            reader.ReadToEnd().Should().Be("Second line");
        }

        [Test]
        public void CanExtractMultipleValues_2()
        {
            using var reader = ReadInputStream(
                "123 tadam the-end"
            );

            reader.Extract(
                out int intValue, 
                out string stringValue);

            intValue.Should().Be(123);
            stringValue.Should().Be("tadam");

            reader.ReadToEnd().Should().Be(" the-end");
        }

        [Test]
        public void CanExtractMultipleValues_4()
        {
            using var reader = ReadInputStream(
                "123 999888777666 -98765.321 tadam the-end"
            );

            reader.Extract(
                out int intValue, 
                out long longValue, 
                out double doubleValue, 
                out string stringValue);

            intValue.Should().Be(123);
            longValue.Should().Be(999888777666);
            doubleValue.Should().Be(-98765.321);
            stringValue.Should().Be("tadam");

            reader.ReadToEnd().Should().Be(" the-end");
        }

        [Test]
        public void CanExtractMultipleValues_5()
        {
            using var reader = ReadInputStream(
                "123 999888777666 45.5 -98765.321 tadam the-end"
            );

            reader.Extract(
                out int intValue, 
                out long longValue, 
                out float floatValue, 
                out double doubleValue, 
                out string stringValue);

            intValue.Should().Be(123);
            longValue.Should().Be(999888777666);
            floatValue.Should().Be(45.5f);
            doubleValue.Should().Be(-98765.321);
            stringValue.Should().Be("tadam");

            reader.ReadToEnd().Should().Be(" the-end");
        }

        [Test]
        public void CanSkipWhitespaceWhenExtractingMultipleValues()
        {
            using var reader = ReadInputStream(
                " \t 123  \t  -98765.321  \t\t  tadam\t \tthe-end"
            );

            reader.Extract(
                out int intValue, 
                out double doubleValue, 
                out string stringValue);

            intValue.Should().Be(123);
            doubleValue.Should().Be(-98765.321);
            stringValue.Should().Be("tadam");

            reader.ReadToEnd().Should().Be("\t \tthe-end");
        }

        [Test]
        public void ExtractThrowsOnInvalidFirstChar()
        {
            using var reader = ReadInputStream(
                " \t A12"
            );

            var exception = Assert.Catch<InvalidDataException>(() => {
                reader.Extract(out int intValue); 
            });

            exception.Message.Should().Be("Error parsing type Int32: unexpected character 'A'");
        }

        private StreamReader ReadInputStream(string contents, int skipToPosition = 0)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream, leaveOpen: true);
            
            writer.Write(contents);
            writer.Flush();
            stream.Position = 0;

            var reader = new StreamReader(stream);
            for (int i = 0; i < skipToPosition; i++)
            {
                reader.Read();
            }

            return reader;
        }
    }
}