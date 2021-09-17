namespace Just.Cli
{
    public class CommandLineInterface : CliCommand
    {
        public CommandLineInterface() : base(null, string.Empty)
        {
            
        }

        
        
        public int Execute(string[] args)
        {
            return 0;
        }

        public override int Execute()
        {
            throw new System.NotImplementedException();
        }
        
        public CliCommand? ActiveCommand { get; private set; }
    }
}
