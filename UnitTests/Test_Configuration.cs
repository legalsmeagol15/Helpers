using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Helpers;

namespace UnitTests
{
    [TestClass]
    public class Test_Configuration
    {
        [TestMethod]
        public void InitializeConfigurationAttribute()
        {
            // The attrib Versions property uses the VersionIntervalSet, which is a new thing.
            ConfigurationAttribute attrib = new ConfigurationAttribute(default, Flags.None, ">=1.2.3.4");
            Assert.IsTrue(attrib.Versions.Contains(new Version(1, 2, 3, 4)));
            Assert.IsTrue(attrib.Versions.Contains(new Version(2, 0, 0, 0)));
            Assert.IsFalse(attrib.Versions.Contains(new Version(1, 2, 3, 3)));
            Assert.IsFalse(attrib.Versions.Contains(new Version(1, 0, 0, 0)));
        }

        [TestMethod]
        public void Test_Load()
        {
            
        }

        [TestMethod]
        public void Test_Save()
        {
            TestClass1 tc = new TestClass1() { Height = 10, Width = 20 };
            Configuration.Save(tc, new Version(1,0,0,0));
        }


        [ConfigurationDeclared("Width")]
        private class TestClass1
        {
            [Configuration()]
            public int Height { get; set; } = 10;
            public int Width { get; set; } = 20;

            [Configuration(key:"subsection")]
            public TestClass2 Included = new TestClass2();


            public TestClass2 NotIncluded = null;
        }

        [ConfigurationDeclared("Y", key:"override_y")]
        private class TestClass2
        {
            [Configuration]
            public int X = 1;
            public int Y = 2;
        }
    }
}
