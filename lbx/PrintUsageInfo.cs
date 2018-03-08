using Tool.Core;

namespace lbx
{
    internal class PrintUsageInfo : ICmdCommand
    {
        public bool ValidateArgs => true;

        public void Execute()
        {
            throw new System.NotImplementedException();
        }

        public void PrintUsage()
        {
            System.Console.WriteLine("lbx [command]");
        }
    }
}