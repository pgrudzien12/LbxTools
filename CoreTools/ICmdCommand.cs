namespace Tool.Core
{
    public interface ICmdCommand
    {
        bool ValidateArgs { get; }

        void PrintUsage();
        void Execute();
    }
}