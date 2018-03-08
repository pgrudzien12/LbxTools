using System.IO;
using System.Text;

namespace lbx
{
    public static class BinaryReaderExtensions
    {
        public static string ReadNullTerminatedFixedString(this BinaryReader me, int maxLength)
        {
            StringBuilder sb = new StringBuilder();
            var bytes = me.ReadBytes(maxLength);
            for (int i = 0; i < bytes.Length; i++)
            {
                char c = (char)bytes[i];
                if (c != '\0') sb.Append(c);
                else break;
            }

            return sb.ToString();
        }
    }
}