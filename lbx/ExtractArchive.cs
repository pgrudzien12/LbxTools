namespace Tool.Lbx
{
    using System.IO;
    using Tool.Core;

    internal class ExtractArchive : ICmdCommand
    {
        private string[] commandArgs;

        public ExtractArchive(string[] commandArgs)
        {
            this.commandArgs = commandArgs;
        }

        public bool ValidateArgs => this.commandArgs.Length >= 1;

        public void Execute()
        {
            var info = LbxInfo.Load(this.commandArgs[0]);
            for (int i = 0; i < info.FileCount; i++)
            {
                var ci = info.ContentInfo[i];

                var dir = this.GetDirName(ci);
                this.EnsureDirExistance(dir);
                string path = CreatePath(dir, i, ci);

                ci.WriteData(path);
            }
        }

        public void PrintUsage()
        {
            throw new System.NotImplementedException();
        }

        private static string CreatePath(string dir, int i, ArchiveContent ci)
        {
            string proposedName = i.ToString() + "_" + ci.Name + "_" + ci.Description;

            return Path.Combine(dir, Santizie(proposedName));
        }

        private static string Santizie(string dirty)
        {
            return string.Concat(dirty.Split(Path.GetInvalidFileNameChars()));
        }

        private string GetDirName(ArchiveContent ci)
        {
            string dir;
            if (this.commandArgs.Length == 2)
            {
                dir = this.commandArgs[1];
            }
            else
            {
                dir = Path.GetFileNameWithoutExtension(this.commandArgs[0]);
            }

            return dir;
        }

        private void EnsureDirExistance(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
    }
}