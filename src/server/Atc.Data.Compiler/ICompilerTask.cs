namespace Atc.Data.Compiler
{
    public interface ICompilerTask
    {
        bool ValidateArguments(InputArguments args);
        void Execute(InputArguments args);
    }
}