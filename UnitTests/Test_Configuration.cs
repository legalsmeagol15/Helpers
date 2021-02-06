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
            ConfigurationAttribute attrib = new ConfigurationAttribute(default, ">=1.2.3.4");
            Assert.IsTrue(attrib.Versions.Contains(new Version(1, 2, 3, 4)));
            Assert.IsTrue(attrib.Versions.Contains(new Version(2, 0, 0, 0)));
            Assert.IsFalse(attrib.Versions.Contains(new Version(1, 2, 3, 3)));
            Assert.IsFalse(attrib.Versions.Contains(new Version(1, 0, 0, 0)));
        }

        [TestMethod]
        public void Test_Converters()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void Test_Default()
        {
            throw new NotImplementedException();
        }
       

        [TestMethod]
        public void Test_RoundTrip()
        {
            TestClass1 testSave = new TestClass1() { Height = 10, Width = 20 };
            Configuration.Save(testSave, new Version(1,0,0,0));

            TestClass1 testLoad = new TestClass1() { Name = "changeme", Description = "changemetoo", Height = -10, Width = -20 };
            testLoad.Included = new TestClass2() { X = -50, Y = -75 };
            testLoad.NotIncluded = new TestClass2() { X = -100, Y = -200 };

            // Check that we can import a solution, which doesn't yet change the value.
            var plan = Configuration.Plan(testLoad);
            Assert.AreEqual(testLoad.Name, "changeme");
            Assert.AreEqual(testLoad.Description, "changemetoo");
            Assert.AreEqual(testLoad.Height, -10);
            Assert.AreEqual(testLoad.Width, -20);

            // Now apply the loaded values
            plan.Apply();
            Assert.AreEqual(testLoad.Name, testSave.Name);
            Assert.AreEqual(testLoad.Description, testSave.Description);
            Assert.AreEqual(testLoad.Height, testSave.Height);
            Assert.AreEqual(testLoad.Width, testSave.Width);
            Assert.IsNotNull(testLoad.Included);
            Assert.AreEqual(testLoad.Included.X, testSave.Included.X);
            Assert.AreEqual(testLoad.Included.Y, testSave.Included.Y);

            // The following should be unchanged in testLoad
            Assert.IsNotNull(testLoad.NotIncluded);
            Assert.AreEqual(testLoad.NotIncluded.X, -100);
            Assert.AreEqual(testLoad.NotIncluded.Y, -200);
        }

        #region The following shows how a class can be marked up for configuration.
        private abstract class TestParentClass
        {
            public string Description { get; set; } = "TestParentClass description";
        }
        private partial class TestClass1
        {
            public string Name { get; set; } = "Sibling class name";
        }

        [ConfigurationDeclared("Width")]
        [ConfigurationDeclared("Name")]
        [ConfigurationDeclared("Description")]
        private partial class TestClass1 : TestParentClass
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

        #endregion
    }
}
