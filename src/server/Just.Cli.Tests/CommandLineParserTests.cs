using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using FluentAssertions;
using NUnit.Framework;

namespace Just.Cli.Tests
{
    public class CommandLineParserTests
    {
        [Test]
        public void CanParseRequiredKeywords()
        {
            var builder = CommandLineParser.NewBuilder();
            var countHello = 0;
            var countWorld = 0;
            
            builder.RequiredKeyword("hello", () => countHello++);
            builder.RequiredKeyword("world", () => countWorld++);

            var parser = builder.Build();

            bool result = parser.Parse(new[] {"hello", "world"});

            result.Should().BeTrue();
            countHello.Should().Be(1);
            countWorld.Should().Be(1);
        }

        [Test]
        public void CanParseOptionalKeywords()
        {
            var builder = CommandLineParser.NewBuilder();
            var countHello = 0;
            var countCrazy = 0;
            var countWorld = 0;
            
            builder.RequiredKeyword("hello", () => countHello++);
            builder.OptionalKeyword("crazy", () => countCrazy++);
            builder.OptionalKeyword("world", () => countWorld++);

            var parser = builder.Build();
            bool result = parser.Parse(new[] {"hello", "world"});

            result.Should().BeTrue();
            countHello.Should().Be(1);
            countCrazy.Should().Be(0);
            countWorld.Should().Be(1);
        }

        [Test]
        public void CanParseLongBoolOptions()
        {
            var builder = CommandLineParser.NewBuilder();
            bool? firstValue = null;
            bool? secondValue = null;
            bool? thirdValue = null;
            bool? fourthValue = null;
            
            builder.NamedFlag("--first", value => firstValue = value);
            builder.NamedFlag("--second", value => secondValue = value);
            builder.NamedFlag("--third", value => thirdValue = value);
            builder.NamedFlag("--fourth", value => fourthValue = value);
            
            var parser = builder.Build();
            bool result = parser.Parse(new[] {"--second", "--fourth-", "--first+"});

            result.Should().BeTrue();
            firstValue.Should().Be(true);
            secondValue.Should().Be(true);
            thirdValue.Should().Be(null);
            fourthValue.Should().Be(false);
        }

        [Test]
        public void CanParseShortBoolOptions()
        {
            var builder = CommandLineParser.NewBuilder();
            bool? firstValue = null;
            bool? secondValue = null;
            bool? thirdValue = null;
            bool? fourthValue = null;
            
            builder.NamedFlag("--first", "-f", value => firstValue = value);
            builder.NamedFlag("--second", "-s", value => secondValue = value);
            builder.NamedFlag("--third", "-3", value => thirdValue = value);
            builder.NamedFlag("--fourth", "-4", value => fourthValue = value);
            
            var parser = builder.Build();
            bool result = parser.Parse(new[] {"-f", "-4-", "-s+", "-3-"});

            result.Should().BeTrue();
            firstValue.Should().Be(true);
            secondValue.Should().Be(true);
            thirdValue.Should().Be(false);
            fourthValue.Should().Be(false);
        }

        [Test]
        public void CanParseCombinedShortBoolOptions()
        {
            var builder = CommandLineParser.NewBuilder();
            bool? firstValue = null;
            bool? secondValue = null;
            bool? thirdValue = null;
            bool? fourthValue = null;
            
            builder.NamedFlag("--first", "-f", value => firstValue = value);
            builder.NamedFlag("--second", "-s", value => secondValue = value);
            builder.NamedFlag("--third", "-3", value => thirdValue = value);
            builder.NamedFlag("--fourth", "-4", value => fourthValue = value);
            
            var parser = builder.Build();
            bool result = parser.Parse(new[] {"-fs4-3+"});

            result.Should().BeTrue();
            firstValue.Should().Be(true);
            secondValue.Should().Be(true);
            thirdValue.Should().Be(true);
            fourthValue.Should().Be(false);
        }

        [Test]
        public void CanParseOptionsWithValue()
        {
            var builder = CommandLineParser.NewBuilder();
            string? stringValue = null;
            int? intValue = null;
            decimal? decimalValue = null;
            DayOfWeek? enumValue = null;
            DateTime? dateTimeValue = null;
            TimeSpan? timeSpanValue = null;
            
            builder.NamedValue<string>("--str", "-s", value => stringValue = value);
            builder.NamedValue<int>("--int", "-n", value => intValue = value);
            builder.NamedValue<decimal>("--decimal", "-f", value => decimalValue = value);
            builder.NamedValue<DayOfWeek>("--enum", "-e", value => enumValue = value);
            builder.NamedValue<DateTime>("--datetime", "-d", value => dateTimeValue = value);
            builder.NamedValue<TimeSpan>("--time", "-t", value => timeSpanValue = value);
            
            var parser = builder.Build();
            bool result = parser.Parse(new[] {
                "-s", "abc",
                "--int=123",
                "--decimal", "123.45",
                "--enum=Tuesday",
                "-d", "2010-08-20T15:00:00Z",
                "-t=6:12:14:45.250"
            });

            result.Should().BeTrue();
            stringValue.Should().Be("abc");
            intValue.Should().Be(123);
            decimalValue.Should().Be(123.45m);
            enumValue.Should().Be(DayOfWeek.Tuesday);
            dateTimeValue.Should().Be(new DateTime(2010, 8, 20, 15, 0, 0, DateTimeKind.Utc));
            timeSpanValue.Should().Be(new TimeSpan(6, 12, 14, 45, 250));
        }

        [Test]
        public void CanParseOptionsWithMultipleValues()
        {
            var builder = CommandLineParser.NewBuilder();
            string? stringValue = null;
            int? intValue = null;
            List<DayOfWeek> enumValues = new();
            
            builder.NamedValue<string>("--str", "-s", value => stringValue = value);
            builder.NamedValue<int>("--int", "-n", value => intValue = value);
            builder.NamedValueList<DayOfWeek>("--enum", "-e", value => enumValues.Add(value));
            
            var parser = builder.Build();
            bool result = parser.Parse(new[] {
                "--enum=Sunday",
                "-n=123",
                "-e", "Monday",
                "--enum", "Friday",
                "-s", "abc",
                "-e=Saturday"
            });

            result.Should().BeTrue();
            enumValues.Should().BeEquivalentTo(new[] {
                DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Friday, DayOfWeek.Saturday
            });
            stringValue.Should().Be("abc");
            intValue.Should().Be(123);
        }

        [Test]
        public void CanParseCommands()
        {
            var builder = CommandLineParser.NewBuilder();
            int? addOperand1 = null;
            int? addOperand2 = null;
            int? divideOperand1 = null;
            int? divideOperand2 = null;
            bool? divideBoolFlag = null;
            int? multiplyOperand1 = null;
            int? multiplyOperand2 = null;
            int? addCommandCount = 0;
            int? divideCommandCount = 0;
            int? multiplyCommandCount = 0;

            var addCommand = builder.Command("add", () => addCommandCount++);
            addCommand.Value<int>(value => addOperand1 = value);
            addCommand.OptionalKeyword("and");
            addCommand.Value<int>(value => addOperand2 = value);
            
            var divideCommand = builder.Command("divide", () => divideCommandCount++);
            divideCommand.Value<int>(value => divideOperand1 = value);
            divideCommand.RequiredKeyword("by");
            divideCommand.Value<int>(value => divideOperand2 = value);
            divideCommand.NamedFlag("--check-division-by-zero", "-z", value => divideBoolFlag = value);

            var multiplyCommand = builder.Command("multiply", () => multiplyCommandCount++);
            multiplyCommand.Value<int>(value => multiplyOperand1 = value);
            multiplyCommand.RequiredKeyword("by");
            multiplyCommand.Value<int>(value => multiplyOperand2 = value);

            var parser = builder.Build();
            bool result = parser.Parse(new[] {
                "divide", "12", "-z", "by", "4", 
            });

            result.Should().BeTrue();
            
            addCommandCount.Should().Be(0);
            divideCommandCount.Should().Be(1);
            multiplyCommandCount.Should().Be(0);

            divideOperand1.Should().Be(12);
            divideOperand2.Should().Be(4);
            divideBoolFlag.Should().Be(true);
        }

        [Test]
        public void CanParseCommandsMixedWithRootOptions()
        {
            var builder = CommandLineParser.NewBuilder();
            bool? option1 = null;
            bool? option2 = null;
            bool? option3 = null;
            int? valueA = null;
            string? valueB = null;
            int countCommandA = 0;
            int countCommandB = 0;
            int countCommandC = 0;
            
            builder.NamedFlag("--one", "-1", value => option1 = value);
            builder.NamedFlag("--two", "-2", value => option2 = value);
            builder.NamedFlag("--three", "-3", value => option3 = value);

            var commandA = builder.Command("aaa", () => countCommandA++);
            commandA.Value<int>(value => valueA = value);

            var commandB = builder.Command("bbb", () => countCommandB++);
            commandB.Value<string>(value => valueB = value);

            var commandC = builder.Command("ccc", () => countCommandC++);

            var parser = builder.Build();
            bool result = parser.Parse(new[] {
                "--one", "bbb", "--two-", "xyz", "-3", 
            });

            result.Should().BeTrue();
            
            countCommandA.Should().Be(0);
            countCommandB.Should().Be(1);
            countCommandC.Should().Be(0);
            
            option1.Should().Be(true);
            option2.Should().Be(false);
            option3.Should().Be(true);
            valueB.Should().Be("xyz");
        }

        [Test]
        public void CommandSpecificOptionsOverrideRootOptions()
        {
            var builder = CommandLineParser.NewBuilder();
            bool? rootOption1 = null;
            int countCommandA = 0;
            int? commandOption1 = null;
            
            builder.NamedFlag("--one", "-1", value => rootOption1 = value);

            var commandA = builder.Command("aaa", () => countCommandA++);
            commandA.NamedValue<int>("--one", "-1", value => commandOption1 = value);

            var parser = builder.Build();
            bool result = parser.Parse(new[] {
                "--one-", "aaa", "--one=123", 
            });

            result.Should().BeTrue();
            
            countCommandA.Should().Be(1);
            rootOption1.Should().Be(false);
            commandOption1.Should().Be(123);
        }

        [Test]
        public void CanParseNestedCommands()
        {
            var builder = CommandLineParser.NewBuilder();
            int? valueA = null;
            string? valueB = null;
            int countCommandA = 0;
            int countCommandB = 0;
            int countCommandC = 0;
            
            var commandA = builder.Command("aaa", () => countCommandA++);
            commandA.Value<int>(value => valueA = value);

            var commandB = commandA.Command("bbb", () => countCommandB++);
            commandB.Value<string>(value => valueB = value);

            var commandC = commandA.Command("ccc", () => countCommandC++);

            var parser = builder.Build();
            bool result = parser.Parse(new[] {
                "aaa", "123", "bbb", "xyz", 
            });

            result.Should().BeTrue();
            
            countCommandA.Should().Be(1);
            countCommandB.Should().Be(1);
            countCommandC.Should().Be(0);
            
            valueA.Should().Be(123);
            valueB.Should().Be("xyz");
        }

        [Test]
        public void CanInvokeOnParseCompletedCallback()
        {
            var builder = CommandLineParser.NewBuilder();
            int? valueA = null;
            string? valueB = null;
            int countCommandA = 0;
            int countCommandB = 0;
            int countCommandC = 0;
            int countOnParseCompletedA = 0;
            int countOnParseCompletedB = 0;
            
            var commandA = builder.Command("aaa", () => countCommandA++, onParseCompleted: () => {
                countCommandA.Should().Be(1);
                countCommandB.Should().Be(1);
                valueA.Should().Be(123);
                valueB.Should().Be("xyz");
                countOnParseCompletedA++;
            });

            commandA.Value<int>(value => valueA = value);

            var commandB = commandA.Command("bbb", () => countCommandB++, onParseCompleted: () => {
                countCommandA.Should().Be(1);
                countCommandB.Should().Be(1);
                valueA.Should().Be(123);
                valueB.Should().Be("xyz");
                countOnParseCompletedB++;
            });

            commandB.Value<string>(value => valueB = value);

            var parser = builder.Build();
            bool result = parser.Parse(new[] {
                "aaa", "123", "bbb", "xyz", 
            });

            result.Should().BeTrue();
            countOnParseCompletedA.Should().Be(1);
            countOnParseCompletedB.Should().Be(1);
        }

        [Test]
        public void CanDetectMissingRequiredKeyword()
        {
            var builder = CommandLineParser.NewBuilder();
            
            builder.RequiredKeyword("lock");
            builder.RequiredKeyword("and");
            builder.RequiredKeyword("key");

            var parser = builder.Build();
            
            bool result = parser.Parse(new[] {
                "lock", "key", 
            });

            result.Should().BeFalse();
        }

        [Test]
        public void CanDetectMissingValue()
        {
            var builder = CommandLineParser.NewBuilder();
            
            builder.Value<int>(value => {});

            var parser = builder.Build();
            bool result = parser.Parse(new string[0]);

            result.Should().BeFalse();
        }

        [Test]
        public void CanDefineAndPrintHelpNonRecursive()
        {
            var builder = CommandLineParser.NewBuilder();
            
            builder
                .NamedFlag("--alpha", "-a", value => {})
                .WithHelp("This is the alpha flag, by default it's false");
            
            var commandA = builder
                .Command("aaa", () => {})
                .WithHelp("This is the first command named A");
            commandA
                .NamedValue<int>("value_a", value => {})
                .WithHelp("This is a required integer value for the command A");

            var commandB = builder
                .Command("bbbb", () => {})
                .WithHelp("This is command B, one of the two possible sub-commands under A");
            commandB
                .Value<int>(value => {})
                .WithHelp("value_b", "A required string");

            var commandC = commandA.Command("bbb", () => {});
            commandC.Value<string>(value => {});

            var parser = builder.Build();
            string syntaxText = parser.GetSyntaxHelpText(recursive: false);
            string helpText = parser.GetFullHelpText(widthChars: 50, recursive: false);

            //syntaxText.Should().Be("[--alpha] aaa|bbb");
            helpText.Should().Be(
                "Options:\n" + 
                "\n" +
                "--alpha - This is the alpha flag, by default it's\n" +
                "          false\n" +
                "\n" +
                "Commands:" +
                "\n" +
                "aaa  - This is the first command named A\n" +
                "\n" +
                "bbbb - This is command B, one of the two possible\n" +
                "       sub-commands under A\n"
            );
        }

        [Test]
        public void CanDefineAndPrintHelpWithRecursion()
        {
            var builder = CommandLineParser
                .NewBuilder()
                .WithHelp("This is a help text testing utility that let's us demo how the help text is built.");
            
            builder
                .NamedFlag("--alpha", "-a", value => {})
                .WithHelp("This is the alpha flag, by default it's false");
            
            var commandA = builder
                .Command("aaa", () => {})
                .WithHelp("This is the first command named A");
            commandA
                .NamedValue<int>("--value_a", value => {})
                .WithHelp("This is a required integer value for the command A");

            var commandB = commandA
                .Command("bbb", () => {})
                .WithHelp("This is command B, one of the two possible sub-commands under A");
            commandB
                .Value<int>(value => {})
                .WithHelp("value_b", "A required string");

            var commandC = commandA.Command("bbb", () => {});
            commandC.Value<string>(value => {});

            var parser = builder.Build();
            string syntaxText = parser.GetSyntaxHelpText(recursive: false);
            string helpText = parser.GetFullHelpText(recursive: true, widthChars: 50, indentChars: 3, newLine: "\n");

            //syntaxText.Should().Be("[--alpha] aaa value_a <number> (bbb <value_b>)|(ccc <value>)");
            helpText.Should().Be(
                "This is a help text testing utility that let's us\n" + 
                "demo how the help text is built.\n" +
                "Options:\n" + 
                "\n" +
                "--alpha - This is the alpha flag, by default it's\n" +
                "          false\n" +
                "\n" +
                "Commands:" +
                "\n" +
                "aaa - This is the first command named A\n" +
                "      Options:\n" + 
                "      \n" +
                "      value_a - This is a required integer value\n" +
                "                for the command A\n" +
                "      \n" +
                "      Commands:" +
                "      \n" +
                "      bbb - This is command B, one of the two\n" + 
                "            possible sub-commands under A\n" + 
                "            Options:\n" + 
                "            \n" +
                "            value_b - a required string\n" + 
                "            \n" +
                "            Commands:" +
                "            \n" +
                "            ccc - (no help available)\n" + 
                "                  Options:\n" + 
                "                  \n" +
                "                  (value) - (no help available)\n" 
            );
        }
    }
}
