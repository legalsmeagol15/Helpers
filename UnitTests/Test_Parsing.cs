using System;
using Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace UnitTests
{
    [TestClass]
    public class Test_Parsing
    {
        private Function.Factory factory;
        [TestInitialize]
        public void TestParsing__Initialize()
        {
            factory = Function.Factory.StandardFactory();
        }


        [TestMethod]
        public void TestParsing_FunctionFactory()
        {   
            Assert.IsTrue(factory.Contains("Addition"));
            Assert.IsTrue(factory.Contains("Subtraction"));
            Assert.IsTrue(factory.Contains("Multiplication"));
            Assert.IsTrue(factory.Contains("Division"));
            Assert.IsTrue(factory.Contains("Exponentiation"));
            Assert.IsTrue(factory.Contains("Negation"));
            Assert.IsTrue(factory.Contains("Subtraction"));
            Assert.IsTrue(factory.Contains("Relation"));
            Assert.IsTrue(factory.Contains("Range"));
            Assert.IsTrue(factory.Contains("And"));
            Assert.IsTrue(factory.Contains("Or"));
            Assert.IsTrue(factory.Contains("PI"));
            Assert.IsTrue(factory.Contains("E"));
            
        }


        [TestMethod]
        public void TestParsing_Number_Equalities()
        {
            Function adder = factory["Addition"];
            // Test that functions are correctly doing calculations.
            Assert.IsTrue(adder.Evaluate(1, 1).Equals(2m));
            Assert.IsTrue(adder.Evaluate(1, 1).Equals(2));
            Assert.IsTrue(adder.Evaluate(1, 1).Equals(2d));
            Assert.IsTrue(adder.Evaluate(1m, 1m).Equals(2m));
            Assert.IsTrue(adder.Evaluate(1d, 1d).Equals(2m));
            Assert.AreEqual(adder.Evaluate(1, 1), 2);

            Number n1 = 1;
            Number n2 = 2;
            Assert.IsTrue(n1 < n2);
            Assert.IsFalse(n1 > n2);
            Assert.IsFalse(n1 == n2);
            Assert.IsTrue(n1 != n2);
            Assert.IsFalse(n2 < n1);
            Assert.IsTrue(n2 > n1);
            Assert.IsFalse(n2 == n1);
            Assert.IsTrue(n2 != n1);
#pragma warning disable CS1718
            Assert.IsTrue(n1 == n1);
            Assert.IsTrue(n2 == n2);
            Assert.IsFalse(n1 < n1);
            Assert.IsFalse(n1 > n1);
#pragma warning restore CS1718
            
        }

        [TestMethod]
        public void TestParsing_Nesting()
        {
            IEvaluatable e = Expression.FromString("2+1", factory, out ISet<Variable> d, null);
            Assert.AreEqual("2 + 1", e.ToString());
            Assert.AreEqual(e.Evaluate(), 3);

            e = Expression.FromString("3+(2+1)", factory, out d, null);
            Assert.AreEqual("3 + ( 2 + 1 )", e.ToString());
            Assert.AreEqual(e.Evaluate(), 6);
        }
    }
}
