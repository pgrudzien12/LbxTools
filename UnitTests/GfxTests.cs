using Tool.Gfx;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace UnitTests
{
    [TestClass]
    public class GfxTests
    {
        [TestMethod]
        public void template_1()
        {
            var tbytes = File.ReadAllBytes("1_template.bmp");
            ConvertImage ci = new ConvertImage(new[] { "0_TRLSHAMN_" });
            ci.Execute();

            var bytes = File.ReadAllBytes("1.bmp");
            AssertEquality(tbytes, bytes);
        }
        [TestMethod]
        public void template_2()
        {
            var tbytes = File.ReadAllBytes("2_template.bmp");
            ConvertImage ci = new ConvertImage(new[] { "0_TRLSHAMN_" });
            ci.Execute();

            var bytes = File.ReadAllBytes("2.bmp");
            AssertEquality(tbytes, bytes);
        }

        private static void AssertEquality(byte[] tbytes, byte[] bytes)
        {
            Assert.AreEqual(tbytes.Length, bytes.Length);
            for (int i = 0; i < tbytes.Length; i++)
            {
                Assert.AreEqual(tbytes[i], bytes[i]);
            }
        }
    }
}
