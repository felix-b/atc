using System;

namespace Just.Cli
{
    public abstract class CliCommand
    {
        protected CliCommand(CliCommand? parent, string name)
        {
            Name = name;
        }

        public void AddOptions(Action<CommandLineParser.Combinator> define)
        {
            
        }

        public void AddCommand(CliCommand command)
        {
            
        }

        public abstract int Execute();
        
        public string Name { get; }
    }
}