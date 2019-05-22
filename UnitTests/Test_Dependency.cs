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
        public void TestParsing_Parsing()
        {
            Expression.FromString("(3x2+(2/y1)*-7)");
            //Expression.FromString("3x+7");

        }
    }

}
