using System.Threading;
using FluentAssertions;
using NUnit.Framework;

namespace Just.Cli.Tests
{
    [TestFixture]
    public class CommandLineInterfaceTests
    {
        [Test, Ignore("TBD")]
        public void CanHandleCommand()
        {
            var cli = new TestInterface();
            var args = new[] { "bbb", "--flag-b", "--flag-root" };

            int exitCode = cli.Execute(args);

            exitCode.Should().Be(0);
            var activeCommand = cli.ActiveCommand as TestCommandB;

            activeCommand.Should().NotBeNull();
            activeCommand.ExecuteCount.Should().Be(0);
            activeCommand.ExecuteCount.Should().Be(1);
            activeCommand.FlagB.Should().Be(true);
            cli.RootFlag.Should().Be(true);
        }

        public class TestInterface : CommandLineInterface
        {
            public TestInterface()
            {
                AddOptions(combinator => {
                    combinator.NamedFlag("--flag-root", "-r", value => RootFlag = value);
                });
                
                AddCommand(new TestCommandA(this));
                AddCommand(new TestCommandB(this));
            }
            
            public bool? RootFlag { get; private set; }
        }

        public class TestCommandA : CliCommand
        {
            public TestCommandA(CliCommand parent) : base(parent, "aaa")
            {
                AddOptions(combinator => {
                    combinator.NamedFlag("--flag-a", value => FlagA = value);
                });
            }

            public override int Execute()
            {
                ExecuteCount++;
                return 0;
            }
            
            public bool? FlagA { get; set; }
            public int ExecuteCount { get; set; }
        }

        public class TestCommandB : CliCommand
        {
            public TestCommandB(CliCommand parent) : base(parent, "bbb")
            {
                AddOptions(combinator => {
                    combinator.NamedFlag("--flag-b", value => FlagB = value);
                });
            }

            public override int Execute()
            {
                ExecuteCount++;
                return 0;
            }
            
            public bool? FlagB { get; set; }
            public int ExecuteCount { get; set; }
        }
    }
}