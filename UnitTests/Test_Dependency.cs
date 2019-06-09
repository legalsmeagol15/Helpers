using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using Dependency;

namespace UnitTests
{
    [TestClass]
    public class Test_Dependency
    {
        [TestMethod]
        public void TestDependency_Parsing()
        {
            IEvaluateable exp1 = Parse.FromString("(3-5+2^3/4*-7)");  // -16

            IEvaluateable val = exp1.UpdateValue();
            Assert.AreEqual(val, -16);
        }
    }

}
