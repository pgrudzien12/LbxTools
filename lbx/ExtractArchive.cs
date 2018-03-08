using System;
using System.IO;
using Tool.Core;

namespace lbx
{
    internal class ExtractArchive : ICmdCommand
    {
        private string[] commandArgs;

        public ExtractArchive(string[] commandArgs)
        {
            this.commandArgs = commandArgs;
        }

        public bool ValidateArgs => commandArgs.Length >= 1;

        public void Execute()
        {
            var info = LbxInfo.Load(commandArgs[0]);
            for (int i = 0; i < info.FileCount; i++)
            {
                var ci = info.ContentInfo[i];

                var dir = GetDirName(ci);
                EnsureDirExistance(dir);
                string path = CreatePath(dir, i, ci);

                ci.WriteData(path);
            }
        }

        private static string CreatePath(string dir, int i, ArchiveContent ci)
        {
            string proposedName = i.ToString() + "_" + ci.Name + "_" + ci.Description;

            return Path.Combine(dir, Santizie(proposedName));
        }

        private static string Santizie(string dirty)
        {
            return String.Concat(dirty.Split(Path.GetInvalidFileNameChars()));
        }

        private string GetDirName(ArchiveContent ci)
        {
            string dir;
            if (commandArgs.Length == 2)
                dir = commandArgs[1];
            else
                dir = Path.GetFileNameWithoutExtension(commandArgs[0]);
            return dir;
        }

        private void EnsureDirExistance(string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        public void PrintUsage()
        {
            throw new System.NotImplementedException();
        }
    }
}