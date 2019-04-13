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
            //AssertThrows<ProfileException>(() => Arguments.Options.Parse < ProfileExceptionOptionsA>("BoolArg") );
        }

        [TestMethod]
        public void Test_Options_Aliases()
        {
            AliasOptionsA opts = Arguments.Options.Parse<AliasOptionsA>("test", "uploadmbps=64.17", "downloadmbps=22.79");
            Assert.AreEqual(opts.Test, true);
            Assert.AreEqual(opts.UploadMbps, 64.17d);
            Assert.AreEqual(opts.DownloadMbps, 22.79d);

            opts = Arguments.Options.Parse<AliasOptionsA>("test", "UploadMean=64.17", "downloadmean=22.79");
            Assert.AreEqual(opts.Test, true);
            Assert.AreEqual(opts.UploadMbps, 64.17d);
            Assert.AreEqual(opts.DownloadMbps, 22.79d);
        }
        
        internal class AliasOptionsA
        {   
            [Group("testing")]
            internal bool Test = false;

            [Alias("uploadmbps",  false)]
            [Alias("uploadmean", false)]
            [Group("testing")]
            internal double UploadMbps = 0.0;

            [Alias("DownloadMbps", false)]
            [Alias("downloadmean", false)]
            [Group("testing")]
            internal double DownloadMbps = 0.0;
        }

        public class ProfileExceptionOptionsA
        {            
            public bool BoolArg;
        }

        public class SimpleOptionsA
        {
            
            [Group("GroupA", true)]
            public bool BoolArg;

            
            [Group("GroupA", true)]
            public string StringArgA;

            
            [Group("GroupA")]
            public string StringArgB;

            
            [Group("GroupA")]
            public int IntArgA;
        }
    }
}
