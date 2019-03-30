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
            SimpleOptionsA opts = Arguments.Options.Parse<SimpleOptionsA>("BoolArg", "StringArgA=valueA", "StringArgB", "valueB", "IntArgA=50");
            Assert.AreEqual(opts.BoolArg, true);
            Assert.AreEqual(opts.StringArgA, "valueA");
            Assert.AreEqual(opts.StringArgB, "valueB");
            Assert.AreEqual(opts.IntArgA, 50);
        }

        [TestMethod]
        public void Test_Options_ProfileExceptions()
        {
            AssertThrows<ProfileException>(() => Arguments.Options.Parse < ProfileExceptionOptionsA>("BoolArg") );
        }

        [TestMethod]
        public void Test_Options_Aliases()
        {
            AliasOptionsA opts = Arguments.Options.Parse<AliasOptionsA>("test");
            Assert.AreEqual(opts.Test, true);
        }

        public class AliasOptionsA
        {   
            internal bool Test = false;
        }

        public class ProfileExceptionOptionsA
        {
            [Pattern(ArgumentPattern.ValueOptional)]
            public bool BoolArg;
        }

        public class SimpleOptionsA
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
