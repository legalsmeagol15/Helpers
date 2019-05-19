using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Arguments;
using static UnitTests.Common;
using System.Net;

namespace UnitTests
{
    [TestClass]
    public class Test_Arguments_Parsing
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

        [TestMethod]
        public void Test_Example_Http()
        {
            string[] args = new string[6];
            args[0] = "GET /docs/index.html HTTP/1.1";
            args[1] = "Host: www.nowhere123.com";
            args[2] = "Accept: image/gif, image/jpeg, */*";
            args[3] = "Accept-Language: en-us";
            args[4] = "Accept-Encoding: gzip, deflate";
            args[5] = "User-Agent: Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1)";

            ExampleHttp http = Arguments.Options.Parse<ExampleHttp>(args);

            // Do a timing.
            var prof = new Arguments.Profile<ExampleHttp>();
            int i = 0;
            DateTime start = DateTime.Now;
            for (; i < (1<<17); i++)
            {
                ExampleHttp e = new ExampleHttp();
                
                foreach (string arg in args)
                    prof.Parse(e, arg);
                prof.Validate();
                prof.Apply(e);
                prof.ResetAll();
            }
            DateTime end = DateTime.Now;
            Console.WriteLine("Performed " + i + " parsings at " + ((end - start).TotalMilliseconds / i) + " ms each.");
        }



        public class ExampleHttp
        {
            public string Request;
            public string Http;
            public IPAddress Host;
            public string Accept;
            [Alias("Accept-Language")]
            public string AcceptLanguage;
            [Alias("Accept-Encoding")]
            public string AcceptEncoding;
            [Group("Required", true)]
            [Alias("User-Agent")]
            public string UserAgent;

            [Invocation("GET")]
            private void InterpretGet(string value)
            {
                string[] split = value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                Request = split[0];
                split = split[1].Split('/');
                Http = split[1];
            }
            [Invocation("Host")]
            private IPAddress InterpretHost(string value)
            {
                return IPAddress.Parse("127.0.0.1");
            }
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
