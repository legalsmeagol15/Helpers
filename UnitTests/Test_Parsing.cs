﻿using Parsing;
using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Helpers_Unit_Testing
{
    [TestClass]
    public class Test_Parsing
    {
        DataContext context = null;

        [TestInitialize]
        public void DataContext_ctor()
        {
            context = DataContext.FromBasic();
        }

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


        [TestMethod]
        public void Test_Variable_ctor()
        {
            Formula f0 = (Formula)Formula.FromString("3x", context);
            Variable v_x = f0.Variables.First();
            Assert.IsTrue(v_x.Name == "x");
            v_x.Contents = 3m;
            Assert.IsNull(v_x.Value);
            Assert.AreEqual(3m, v_x.Update());
            Assert.AreEqual(3m, v_x.Value);
            Assert.AreEqual(9m, f0.Update());
            Assert.AreEqual("3x", f0.ToString());


            Formula f1 = (Formula)Formula.FromString("4x+2", context);
            Assert.IsTrue(f1.Variables.First().Name == "x");
            Assert.AreEqual(v_x.Contents, 3m);
            Assert.AreEqual(v_x.Value, 3m);
            Assert.AreEqual(14m, f1.Update());
            Assert.AreEqual("4x + 2", f1.ToString());

            Formula f2 = (Formula)Formula.FromString("4 (x + 2)", context);
            Assert.IsTrue(f2.Variables.First().Name == "x");
            Assert.IsTrue(f2.Variables.Count == 1);
            Assert.AreEqual(v_x.Contents, 3m);
            Assert.AreEqual(v_x.Value, 3m);
            Assert.AreEqual(20m, f2.Update());
            Assert.AreEqual("4(x + 2)", f2.ToString());

            Variable v_y = context.AddVariable("y");
            Assert.IsNull(v_y.Contents);
            v_y.Contents = Formula.FromString("x*2", context);
            Assert.AreEqual(v_x.Contents, 3m);
            Assert.AreEqual(v_x.Value, 3m);
            Assert.IsNotNull(v_y.Contents);
            Assert.IsNull(v_y.Value);
            Assert.AreEqual(6m, v_y.Update());
            Assert.AreEqual(6m, v_y.Value);

            //Test that a change at the bottom of the update change requires calls to Update() before the top of the update change will 
            //be aware.
            v_x.Contents = 10m;
            Assert.AreEqual(10m, v_x.Contents);
            Assert.AreEqual(3m, v_x.Value);
            Assert.AreEqual(6m, v_y.Value);
            Assert.AreEqual(10m, v_x.Update());
            Assert.AreEqual(6m, v_y.Value);
            Assert.AreEqual(20m, v_y.Update());

        }


        [TestMethod]
        public void Test_NamedFunctions_Trig()
        {
            Formula c0 = (Formula)Formula.FromString("cos(1)", context);
            Assert.AreEqual("COS", c0.GetType().Name);
            Assert.AreEqual((decimal)Math.Cos(1), c0.Update());
            Assert.AreEqual("COS(1)", c0.ToString());
        }

        
    }
}
