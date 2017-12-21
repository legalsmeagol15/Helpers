using Parsing;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Helpers_Unit_Testing
{
    [TestClass]
    public class Test_Parsing
    {
        [TestMethod]
        public void Test_Formula_ctor()
        {
            object f0 = Formula.FromString("7");
            Assert.IsInstanceOfType(f0, typeof(decimal));
            Assert.AreEqual(7m, f0);

            object f1 = Formula.FromString("7.1");
            Assert.IsInstanceOfType(f1, typeof(decimal));
            Assert.AreEqual(7.1m, f1);

            object f2 = Formula.FromString("-7.3");
            Assert.IsInstanceOfType(f2, typeof(decimal));
            Assert.AreEqual(-7.3m, f2);

            object f3 = Formula.FromString("(7)");
            Assert.IsInstanceOfType(f3, typeof(Formula));
            Assert.AreEqual(7m, ((Formula)f3).Update());

            object f4 = Formula.FromString("(-7)");
            Assert.IsInstanceOfType(f4, typeof(Formula));
            Assert.AreEqual(-7m, ((Formula)f4).Update());

            object f5 = Formula.FromString("-(7)");
            Assert.IsInstanceOfType(f5, typeof(Formula));
            Assert.AreEqual(-7m, ((Formula)f5).Update());

            object f6 = Formula.FromString("-(-7)");
            Assert.IsInstanceOfType(f6, typeof(Formula));
            Assert.AreEqual(7m, ((Formula)f6).Update());
        }

        [TestMethod]
        public void Test_Formula_Add()
        {
            Formula add0 = (Formula)Formula.FromString("1+2");
            Assert.AreEqual(3m, add0.Update());
            Assert.AreEqual(3m, add0.Value);
            Assert.AreEqual("1 + 2", add0.ToString());

            Formula add1 = (Formula)Formula.FromString("1+2+3");
            Assert.AreEqual(6m, add1.Update());
            Assert.AreEqual("1 + 2 + 3", add1.ToString());

            Formula add2 = (Formula)Formula.FromString("1 + (2+3)");
            Assert.AreEqual(6m, add2.Update());
            Assert.AreEqual("1 + (2 + 3)", add2.ToString());

            Formula add3 = (Formula)Formula.FromString("-1 + (2+3)");
            Assert.AreEqual(4m, add3.Update());
            Assert.AreEqual("-1 + (2 + 3)", add3.ToString());

            Formula add4 = (Formula)Formula.FromString("1 + (-2+3)");
            Assert.AreEqual(2m, add4.Update());
            Assert.AreEqual("1 + (-2 + 3)", add4.ToString());

            Formula add5 = (Formula)Formula.FromString("1+(2+-3)");
            Assert.AreEqual(0m, add5.Update());
            Assert.AreEqual("1 + (2 + -3)", add5.ToString());

            Formula add6 = (Formula)Formula.FromString("-1+(-2+-3)");
            Assert.AreEqual(-6m, add6.Update());
            Assert.AreEqual("-1 + (-2 + -3)", add6.ToString());

            Formula add7 = (Formula)Formula.FromString("-1+-(-2+-3)");
            Assert.AreEqual(4m, add7.Update());
            Assert.AreEqual("-1 + -(-2 + -3)", add7.ToString());

            Formula add8 = (Formula)Formula.FromString("1 + 2 + 3 + 4 + 5");
            Assert.AreEqual(15m, add8.Update());
            Assert.AreEqual("1 + 2 + 3 + 4 + 5", add8.ToString());

            Formula add9 = (Formula)Formula.FromString("1 - 2 + 3 - 4 + 5");
            Assert.AreEqual(3m, add9.Update());
            Assert.AreEqual("1 - 2 + 3 - 4 + 5", add9.ToString());

            Formula add10 = (Formula)Formula.FromString("1+2-3+4-5");
            Assert.AreEqual(-1m, add10.Update());
            Assert.AreEqual("1 + 2 - 3 + 4 - 5", add10.ToString());
        }

        [TestMethod]
        public void Test_Formula_Divide()
        {
            Formula d0 = (Formula)Formula.FromString("1/2");
            Assert.AreEqual(0.5m, d0.Update());
            Assert.AreEqual("1 / 2", d0.ToString());

            Formula d1 = (Formula)Formula.FromString("-1/2");
            Assert.AreEqual(-0.5m, d1.Update());
            Assert.AreEqual("-1 / 2", d1.ToString());

            Formula d2 = (Formula)Formula.FromString("1/-2");
            Assert.AreEqual(-0.5m, d2.Update());
            Assert.AreEqual("1 / -2", d2.ToString());

            Formula d3 = (Formula)Formula.FromString("-1 / -2");
            Assert.AreEqual(0.5m, d3.Update());
            Assert.AreEqual("-1 / -2", d3.ToString());

            Formula d4 = (Formula)Formula.FromString("1/2/4");
            Assert.AreEqual(0.125m, d4.Update());
            Assert.AreEqual("1 / 2 / 4", d4.ToString());
        }

        [TestMethod]
        public void Test_Formula_Multiply()
        {
            Formula m0 = (Formula)Formula.FromString("2*3");
            Assert.AreEqual(6m, m0.Update());
            Assert.AreEqual("2 * 3", m0.ToString());

            Formula m1 = (Formula)Formula.FromString("-2*3");
            Assert.AreEqual(-6m, m1.Update());
            Assert.AreEqual("-2 * 3", m1.ToString());

            Formula m2 = (Formula)Formula.FromString("2*-3");
            Assert.AreEqual(-6m, m2.Update());
            Assert.AreEqual("2 * -3", m2.ToString());

            Formula m3 = (Formula)Formula.FromString("-2*-3");
            Assert.AreEqual(6m, m3.Update());
            Assert.AreEqual("-2 * -3", m3.ToString());

            Formula m4 = (Formula)Formula.FromString("2*3*4*5");
            Assert.AreEqual(120m, m4.Update());
            Assert.AreEqual("2 * 3 * 4 * 5", m4.ToString());            
        }


        [TestMethod]
        public void Test_Formula_Subtract()
        {
            Formula s0 = (Formula)Formula.FromString("100-10");
            Assert.AreEqual(90m, s0.Update());
            Assert.AreEqual("100 - 10", s0.ToString());

            Formula s1 = (Formula)Formula.FromString("100--10");
            Assert.AreEqual(110m, s1.Update());
            Assert.AreEqual("100 - -10", s1.ToString());

            Formula s2 = (Formula)Formula.FromString("-100--10");
            Assert.AreEqual(-90m, s2.Update());
            Assert.AreEqual("-100 - -10", s2.ToString());

            Formula s3 = (Formula)Formula.FromString("100-(10-1)");
            Assert.AreEqual(91m, s3.Update());
            Assert.AreEqual("100 - (10 - 1)", s3.ToString());

            Formula s4 = (Formula)Formula.FromString("100--(10-1)");
            Assert.AreEqual(109m, s4.Update());
            Assert.AreEqual("100 - -(10 - 1)", s4.ToString());

            Formula s5 = (Formula)Formula.FromString("-100--(-10--1)");
            Assert.AreEqual(-109m, s5.Update());
            Assert.AreEqual("-100 - -(-10 - -1)", s5.ToString());
        }

        
    }
}
