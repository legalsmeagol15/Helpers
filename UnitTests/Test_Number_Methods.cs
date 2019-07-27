using System;
using Mathematics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class Test_Number_Methods
    {
        [TestMethod]
        public void Test_Log_2()
        {
            int root = 1;
            Assert.IsTrue(Mathematics.Int32.Log_2(root) == 0);
            root <<= 1;
            for (int log = 1; log <28; log++)
            {
                Assert.IsTrue(Mathematics.Int32.Log_2(root) == log);
                Assert.IsTrue(Mathematics.Int32.Log_2(root+1) == log);
                root <<= 1;
            }
        }
    }
}
