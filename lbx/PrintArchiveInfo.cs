namespace Tool.Lbx
{
    using Tool.Core;

    internal class PrintArchiveInfo : ICmdCommand
    {
        private string[] args;

        public PrintArchiveInfo(string[] args)
        {
            this.args = args;
        }

        public bool ValidateArgs
        {
            get
            {
                return this.args.Length > 0;
            }
        }

        public void Execute()
        {
            System.Console.WriteLine($"Reading {this.args[0]} archive...");
            LbxInfo info = LbxInfo.Load(this.args[0]);
            System.Console.WriteLine($"Archive contains {info.FileCount} files:");
            foreach (var item in info.ContentInfo)
            {
                System.Console.WriteLine($"\t[+{item.Offset}, {item.Data.Length}] Name:{item.Name}, Description:{item.Description}");
            }

            System.Console.WriteLine($"LBX file end offset is set to: {info.EndOffset}");
        }

        public void PrintUsage()
        {
            throw new System.NotImplementedException();
        }
    }
}