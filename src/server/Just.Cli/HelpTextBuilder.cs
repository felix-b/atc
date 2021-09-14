using System;
using System.Collections.Generic;

namespace Just.Cli
{
    internal class HelpTextBuilder
    {
        public void AddOption(CommandLineParser.HelpRecord help)
        {
            
        }

        public void AddCommand(CommandLineParser.HelpRecord help, Action<HelpTextBuilder>? buildNestedHelp)
        {
            
        }

        public string GetFullHelpText(string newLine, int widthChars, int indentChars)
        {
            return string.Empty;
        }

        public string GetSyntaxHelpText(string newLine, int widthChars, int indentChars)
        {
            return string.Empty;
        }

        private class CommandNode
        {
            private readonly string _thisIndent;
            private readonly CommandLineParser.HelpRecord _thisHelp;
            private readonly List<CommandLineParser.HelpRecord> _options = new();
            private readonly List<CommandNode> _commands = new();

            public CommandNode(string indent, CommandLineParser.HelpRecord help)
            {
                _thisIndent = indent;
                _thisHelp = help;
            }
            
            public void AddOption(CommandLineParser.HelpRecord help)
            {
                _options.Add(help);            
            }

            public void AddCommand(CommandLineParser.HelpRecord help, int indentChars, Action<HelpTextBuilder>? buildNestedHelp)
            {
                var nestedIndent = string.Empty.PadRight(_thisIndent.Length + indentChars);
                var nestedNode = new CommandNode(nestedIndent, help);
                _commands.Add(nestedNode);
            }
        }
    }
}