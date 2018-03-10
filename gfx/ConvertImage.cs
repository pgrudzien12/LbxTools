namespace Tool.Gfx
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using ImageSharp;
    using Tool.Core;

    public class ConvertImage : ICmdCommand
    {
        private string[] args;

        public ConvertImage(string[] args)
        {
            this.args = args;
        }

        public bool ValidateArgs => true;

        public void Execute()
        {
            string filename = this.args[0];
            using (var stream = File.OpenRead(filename))
            {
                int i = 0;
                foreach (var img in RunLengthDecoder.ConvertToBmps(stream))
                {
                    img.Save($"{i++}.bmp");
                }
            }
        }

        public void PrintUsage()
        {
        }
    }
}