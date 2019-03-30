using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Arguments;

namespace UnitTests
{
    [TestClass]
    public class TestArgumentParsing
    {
        [TestMethod]
        public void Test_Options_Parsing()
        {
            TestOptions opts = Arguments.Options.Parse<TestOptions>("BoolArg", "StringArgA=valueA", "StringArgB", "valueB");
            Assert.AreEqual(opts.BoolArg, true);
            Assert.AreEqual(opts.StringArgA, "valueA");
            Assert.AreEqual(opts.StringArgB, "valueB");
            Console.WriteLine("Whatevah.");
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
            [Group("GroupA", true)]
            public string StringArgB;
             
            
        }
    }
}
