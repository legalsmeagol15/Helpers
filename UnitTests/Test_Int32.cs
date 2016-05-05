using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Arithmetic.Int32;

namespace UnitTests
{
    [TestClass]
    public class Test_Int32
    {
        [TestMethod]
        public void Test_Mod()
        {
            Assert.AreEqual(2, Mod(-1, 3));
            Assert.AreEqual(2, Mod(-4, 3));
            Assert.AreEqual(2, Mod(-7, 3));
            Assert.AreEqual(1, Mod(-2, 3));
            Assert.AreEqual(1, Mod(-5, 3));
            Assert.AreEqual(1, Mod(-8, 3));
            Assert.AreEqual(0, Mod(-3, 3));
            Assert.AreEqual(0, Mod(-6, 3));
            Assert.AreEqual(0, Mod(-9, 3));


        }
    }
}
