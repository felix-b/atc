using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Just.Cli
{
    internal class HelpTextBuilder
    {
        private CommandNode? _rootNode;
        private CommandNode? _currentNode;

        public void AddOption(CommandLineParser.HelpRecord help)
        {
            ValidateRootNode();
            _currentNode!.AddOption(help);
        }

        public void AddCommand(CommandLineParser.HelpRecord help, Action<HelpTextBuilder>? buildNestedHelp)
        {
            var saveCurrentNode = _currentNode;
            var currentIndentLevel = _currentNode?.IndentLevel ?? -1;

            var newNode = new CommandNode(currentIndentLevel + 1, help);

            _rootNode ??= newNode;
            _currentNode = newNode;
            saveCurrentNode?.AddCommandNode(newNode);
            
            buildNestedHelp?.Invoke(this);
            
            _currentNode = saveCurrentNode;
        }

        public string GetFullHelpText(string newLine, int widthChars, int indentChars)
        {
            ValidateRootNode();

            var output = new StringBuilder();
            AppendParagraphWithWordWrap( _rootNode!.Help.Help, output, newLine, leftMargin: 0, widthChars);
            _rootNode!.AppendNestedHelpText(output, newLine, widthChars, indentChars);
            return output.ToString();
        }

        public string GetSyntaxHelpText(string newLine, int widthChars, int indentChars)
        {
            return string.Empty;
        }

        private void ValidateRootNode()
        {
            if (_rootNode == null)
            {
                throw new InvalidOperationException("The root command did not contribute its help.");
            }
        }

        private static void AppendParagraphWithWordWrap(
            string text, 
            StringBuilder output, 
            string newLine, 
            int leftMargin,
            int rightMargin)
        {
            var width = rightMargin - leftMargin + 1;
            var shouldIndent = false;
            
            while (text.Length > 0)
            {
                var paragraphLine = TakeNextLine();

                if (shouldIndent)
                {
                    output.Append(' ', leftMargin);
                }

                shouldIndent = true;

                output.Append(paragraphLine);
                output.Append(newLine);
            }

            string TakeNextLine()
            {
                var wrapIndex = -1;
                int index;
                
                for (index = 0 ; index < text.Length && index < width ; index++)
                {
                    if (char.IsWhiteSpace(text[index]))
                    {
                        wrapIndex = index;
                    }
                }

                if (wrapIndex < 0)
                {
                    wrapIndex = index;
                }

                while (index < text.Length && char.IsWhiteSpace(text[index]))
                {
                    index++;
                }
                
                var result = text.Substring(0, wrapIndex);
                text = text.Substring(index);
                return result;
            }
        }

        private static void AppendTermAndDescription(
            string term, 
            string description, 
            StringBuilder output, 
            string newLine, 
            int leftMargin,
            int termWidth,
            int rightMargin)
        {
            output.Append(' ', leftMargin);
            output.Append(term.PadRight(termWidth));
            output.Append(" - ");
            AppendParagraphWithWordWrap(
                description,
                output,
                newLine,
                leftMargin: leftMargin + termWidth + 3,
                rightMargin);
        }

        private static void AppendIndentedParagraph(
            string text, 
            StringBuilder output,
            string newLine,
            int leftMargin)
        {
            output.Append(' ', leftMargin);
            output.Append(text);
            output.Append(newLine);
        }

        private class CommandNode
        {
            private readonly int _indentLevel;
            private readonly CommandLineParser.HelpRecord _thisHelp;
            private readonly List<CommandLineParser.HelpRecord> _options = new();
            private readonly List<CommandNode> _commands = new();

            public CommandNode(int indentLevel, CommandLineParser.HelpRecord help)
            {
                _indentLevel = indentLevel;
                _thisHelp = help;
            }
            
            public void AddOption(CommandLineParser.HelpRecord help)
            {
                _options.Add(help);            
            }

            public void AddCommandNode(CommandNode nestedNode)
            {
                _commands.Add(nestedNode);
            }

            public void AppendNestedHelpText(StringBuilder output, string newLine, int widthChars, int indentChars)
            {
                var leftMargin = _indentLevel * indentChars;

                if (_options.Count > 0)
                {
                    AppendIndentedParagraph("Options:", output, newLine, leftMargin);
                    output.Append(newLine);

                    var termWidth = _options.Select(opt => opt.Alias.Length).Max();
                    foreach (var option in _options)
                    {
                        AppendTermAndDescription(
                            option.Alias, 
                            option.Help, 
                            output, 
                            newLine, 
                            leftMargin, 
                            termWidth, 
                            widthChars);
                    }
                }

                if (_options.Count > 0 && _commands.Count > 0)
                {
                    output.Append(newLine);
                }

                if (_commands.Count > 0)
                {
                    AppendIndentedParagraph("Commands:", output, newLine, leftMargin);

                    var termWidth = _commands.Select(cmd => cmd.Help.Alias.Length).Max();
                    foreach (var command in _commands)
                    {
                        output.Append(newLine);
                        AppendTermAndDescription(
                            command.Help.Alias, 
                            command.Help.Help, 
                            output, 
                            newLine, 
                            leftMargin, 
                            termWidth, 
                            widthChars);
                    }
                }
            }
            
            public int IndentLevel => _indentLevel;

            public CommandLineParser.HelpRecord Help => _thisHelp;
        }
    }
}
