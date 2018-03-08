using Tool.Core;
using System;
using System.Drawing;
using System.Linq;

namespace Tool.Gfx
{
    class Program
    {
        static void Main(string[] args)
        {
            Color c1 = Color.FromArgb(0xFF00FF);
            Color c2 = Color.FromArgb(255, 0, 255);
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
                return new PrintUsageInfo();

            string[] commandArgs = args.Skip(1).ToArray();

            string commandName = args[0];
            switch (commandName.ToLower())
            {
                default:
                    return new ConvertImage(args);
            }
        }
    }
}
