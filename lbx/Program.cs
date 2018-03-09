namespace Tool.Lbx
{
    using System.Linq;
    using Tool.Core;

    internal class Program
    {
        private static void Main(string[] args)
        {
            ICmdCommand command = CreateCommand(args);
            if (!command.ValidateArgs)
            {
                command.PrintUsage();
                return;
            }

            command.Execute();
        }

        private static ICmdCommand CreateCommand(string[] args)
        {
            if (args.Length == 0)
            {
                return new PrintUsageInfo();
            }

            string[] commandArgs = args.Skip(1).ToArray();

            string commandName = args[0];
            switch (commandName.ToLower())
            {
                case "info":
                    return new PrintArchiveInfo(commandArgs);
                case "extract":
                    return new ExtractArchive(commandArgs);
                default:
                    return new ExtractArchive(args);
            }
        }
    }
}
