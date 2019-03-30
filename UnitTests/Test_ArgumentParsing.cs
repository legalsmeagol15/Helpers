using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Arguments;
using static UnitTests.Common;

namespace UnitTests
{
    [TestClass]
    public class Test_ArgumentParsing
    {
        [TestMethod]
        public void Test_Options_Parsing()
        {
            TestOptions opts = Arguments.Options.Parse<TestOptions>("BoolArg", "StringArgA=valueA", "StringArgB", "valueB", "IntArgA=50");
            Assert.AreEqual(opts.BoolArg, true);
            Assert.AreEqual(opts.StringArgA, "valueA");
            Assert.AreEqual(opts.StringArgB, "valueB");
            Assert.AreEqual(opts.IntArgA, 50);
        }

        public class TestOptions
        {
            [Pattern(ArgumentPattern.KeyOnly)]
            [Group("GroupA", true)]
            public bool BoolArg;

            [Pattern(ArgumentPattern.ValueRequired)]
            [Group("GroupA", true)]
            public string StringArgA;

            [Pattern(ArgumentPattern.ValueRequired)]
            [Group("GroupA")]
            public string StringArgB;

            [Pattern(ArgumentPattern.ValueRequired)]
            [Group("GroupA")]
            public int IntArgA;
        }
    }
}
