﻿using System;
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
using static UnitTests.Common;
using System.Runtime.CompilerServices;

namespace UnitTests
{
   


    [TestClass]
    public class Test_Dependency
    {
        [TestMethod]
        public void TestDependency_Linear()
        {
            int numVars = 100;
            int timings = 100;
            int timingSizes = 11;

            // Test linear transmission  of a value by changing contents.
            Variable vStart = new Variable(null, "vStart", Dependency.Number.One);
            Assert.AreEqual(Dependency.Number.One, vStart.Contents);
            Assert.AreEqual(Dependency.Number.One, vStart.Value);
            
            Variable vLast = vStart;
            List<Variable> vars = new List<Variable>();
            for (int i = 0; i < numVars; i++)
            {
                Variable vNext = new Variable(null, "v" + i, vLast);
                vars.Add(vNext);
                vLast = vNext;
            }

            Number n4 = new Number(4);
            vStart.Contents = n4;
            Assert.AreEqual(n4, vLast.Value);

            // Do a timing
            while (timingSizes > 0 && timings > 0)
            {
                // Set up a chain to experiment on.
                Variable vBegin = new Variable(null, "vBegin");
                int n = Mathematics.Int32.Exp_2(timingSizes);
                Variable vEnd = vBegin;
                List<Variable> keepItAlive = new List<Variable>();
                for (int i = 0; i < n; i++)
                {
                    Variable vNext = new Variable(null, "v" + i, vEnd);
                    keepItAlive.Add(vNext);
                    vEnd = vNext;
                }

                // Warmup
                vBegin.Contents = new Number(-1);

                // Do the indicated number of timings
                DateTime start = DateTime.Now;
                for (int i = 0; i < timings; i++)
                {
                    Number num = new Number(i);
                    vStart.Contents = num;
                }
                DateTime end = DateTime.Now;

                // Take out the overhead.
                DateTime overheadStart = DateTime.Now;
                for (int  i = 0; i < timings; i++)
                {
                    Number num = new Number(i);
                }
                DateTime overheadEnd = DateTime.Now;

                var duration = (end - start) - (overheadEnd - overheadStart);
                Console.WriteLine("n=" + n + ", duration=" + duration + " for " + timings + " runs.");
                timingSizes--;
            }
            
        }

        [TestMethod]
        public void TestDependency_Named_Functions()
        {
            IFunctionFactory functions = new Dependency.Functions.ReflectedFunctionFactory();
            IEvaluateable exp0 = Parse.FromString("ABS(11)", functions);
            IEvaluateable exp1 = Parse.FromString("ABS(-7)", functions);
            Assert.AreEqual(exp0.UpdateValue(), 11);
            Assert.AreEqual(exp1.UpdateValue(), 7);
        }

        [TestMethod]
        public void TestDependency_Parsing()
        {
            IEvaluateable exp0 = Parse.FromString("3-5+2^3/4*-7-1");    // -17
            IEvaluateable exp1 = Parse.FromString("(3-5+2^3/4*-7-1)");  // Still -17
            IEvaluateable exp2 = Parse.FromString("(3-5+(2^3)/4*-7-1)"); // Still -17
            IEvaluateable exp3 = Parse.FromString("(3-5+( ( (2^3) /4) *-7)-1)"); // Still -17

            Assert.AreEqual(exp0.UpdateValue(), -17);
            Assert.AreEqual(exp0.Value, -17);
            Assert.AreEqual(exp1.UpdateValue(), -17);
            Assert.AreEqual(exp1.Value, -17);
            Assert.AreEqual(exp2.UpdateValue(), -17);
            Assert.AreEqual(exp2.Value, -17);
            Assert.AreEqual(exp3.UpdateValue(), -17);
            Assert.AreEqual(exp3.Value, -17);

            //var timings = DoTiming(() => exp1.UpdateValue(), 512, 16);
            //PrintTimings(timings);
        }


        [TestMethod]
        public void TestDependency_Reference()
        {
            DependencyContext context = new DependencyContext();
            Line line0 = new Line();
            context.Add(line0, "line0");

            // There are two different ways of building a reference - make sure they end up at the same result.
            Reference refA = Reference.FromPath(context, "line0", "Color", "red");            
            Assert.IsTrue(refA.Source != null && refA.Source is Variable);
            Reference refB = Reference.FromPath(context)["line0"]["Color"]["red"];
            Assert.IsTrue(refB.Source != null && refB.Source is Variable);
            Assert.IsTrue(ReferenceEquals(refA.Source, refB.Source));
        }


        [TestMethod]
        public void TestDependency_ToString()
        {
            IEvaluateable exp0 = Parse.FromString("3-5+2^3/4*-7-1");    // -17
            IEvaluateable exp1 = Parse.FromString("(3-5+2^3/4*-7-1)");  // Still -17
            IEvaluateable exp2 = Parse.FromString("(3-5+(2^3)/4*-7-1)"); // Still -17
            IEvaluateable exp3 = Parse.FromString("(3-5+( ( (2^3) /4) *-7)-1)"); // Still -17

            Assert.AreEqual("3 - 5 + 2 ^ 3 / 4 * -7 - 1", exp0.ToString());
            Assert.AreEqual("( 3 - 5 + 2 ^ 3 / 4 * -7 - 1 )", exp1.ToString());
            Assert.AreEqual("( 3 - 5 + ( 2 ^ 3 ) / 4 * -7 - 1 )", exp2.ToString());
            Assert.AreEqual("( 3 - 5 + ( ( ( 2 ^ 3 ) / 4 ) * -7 ) - 1 )", exp3.ToString());
        }

        private static void PrintTimings(double[] timings)
        {
            double sum = 0d;
            for (int i = 0; i < timings.Length; i++)
            {
                Console.Write(i + ":\t" + timings[i].ToString("0.00000"));
                if (i > 0)
                    Console.Write("\t" + ( timings[i] / timings[i - 1]));
                Console.Write("\n");
                sum += timings[i];
            }

            Console.WriteLine("Average: " + (sum / timings.Length));
            
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private static double[] DoTiming(Action action, int runs = 16, int doublings = 8)
        {
            double[] results = new double[doublings];
            action();
            DateTime loader = DateTime.Now;            
            for (int idx = 0; idx < doublings; idx++)
            {
                runs <<= 1;
                DateTime start = DateTime.Now;
                for (int j = 0; j < runs; j++)
                {
                    action();
                }
                DateTime end = DateTime.Now;
                TimeSpan ts = end - start;

                start = DateTime.Now;
                for (int j = 0; j < runs; j++)
                {
                    // Do nothing.
                }
                end = DateTime.Now;

                ts -= (end - start);
                results[idx] = ts.TotalMilliseconds / runs;
            }

            return results;
        }
    }





    internal class Brush : ISubcontext
    {
        IContext ISubcontext.Parent { get; set; }

        bool IContext.TryGetProperty(string token, out IEvaluateable source)
        {
            switch (token)
            {
                case "width": source = new Number(2); return true;
                case "pattern": source = new Number(0.25); return true;
                default: source = null; return false;
            }
        }

        bool IContext.TryGetSubcontext(string token, out IContext ctxt) { ctxt = null; return false; }
    }

    [TestClass]
    public class Test_ManagedContext
    {
        [TestMethod]
        public void TestManagedContext_Creation()
        {
            DependencyContext root = new DependencyContext();

            // The line class does not implement IContext, so it will have to be a managed context.
            Line line0 = new Line() { X1 = 0, Y1 = 0, X2 = -20, Y2 = 3 };
            root.Add(line0, "line0");
            Assert.IsTrue(root.TryGetSubcontext("line0", out IContext ctxt) && ctxt.GetType().Name == "ManagedContext");

            // The Brush class DOES implement IContext, so it will NOT be a managed context.
            Brush brush0 = new Brush();
            root.Add(brush0, "brush0");
            Assert.IsTrue(root.TryGetSubcontext("brush0", out ctxt) && ctxt is Brush);

            // Test that non-existent properties don't create something dysfunctional.
            Assert.IsFalse(root.TryGetSubcontext("nonexistent", out _));
            Assert.IsFalse(ctxt.TryGetProperty("missing", out _));

            // check that the reference to the line's X1 property returns a Variable with the proper value.
            IEvaluateable e = null;
            Assert.IsTrue(root.TryGetSubcontext("line0", out ctxt) && ctxt.TryGetProperty("X1", out e));
            Assert.IsTrue(e is Variable v);
            Assert.AreEqual(e.Value, 10);

            e = Reference.FromPath(root, "line0", "X2").Source;
            Assert.IsTrue(e is Variable);
            Assert.AreEqual(e.Value, -20);

            AssertThrows<Parse.ReferenceException>(() => e = Reference.FromPath(root, "line0", "Y1").Source);

            Variable varY2 = Reference.FromPath(root, "line0", "Y2").Source as Variable;
            Assert.IsNotNull(varY2);
            Assert.AreEqual(varY2.Name, "Y2");
            Assert.AreEqual(varY2.Value, 10); // If reference works, this will work.
        }

        [TestMethod]
        public void TestManagedContext_Interdependency()
        {
            // Show that a variable in a subcontext can  be found from a root context.

            DependencyContext root = new DependencyContext();
            Line line0 = new Line() { X1 = 0, Y1 = 0, X2 = -20, Y2 = 3 };
            root.Add(line0, "line0");
            Variable varX1 = Reference.FromPath(root, "line0", "X1").Source as Variable;
            Variable varY1 = Reference.FromPath(root, "line0", "Y1").Source as Variable;
            Assert.IsNotNull(varX1);
            Assert.IsNotNull(varY1);

            varX1.Contents = Parse.FromString("Y1", null, root);
            Assert.AreNotEqual(varX1, varY1);
            Assert.IsTrue(varX1.Contents is Expression);
            Assert.IsTrue(varX1.GetTerms().Contains(varY1));
        }


    }


    internal class Line
    {
        // Using this class to test the AutoContext

        private double _X1, _Y1, _X2, _Y2;

        [Property(source: true, listener: true, initialContents:"10")]
        public double X1 { get => _X1; set { _X1 = value; Length = GetLength(); } }

        [Property(source: true, listener: false)]
        public double X2 { get => _X2; set { _X2 = value; Length = GetLength(); } }

        [Property(source: false, listener: true, initialContents: "14")]
        public double Y1 { get => _Y1; set { _Y1 = value; Length = GetLength(); } }

        [Property(source: true, listener: true, initialContents: "X1")]
        public double Y2 { get => _Y2; set { _Y2 = value; Length = GetLength(); } }

        private double _Length;
        [Property(source: true, listener: false, initialContents: "")]
        public double Length
        {
            get => _Length; private set
            {
                _Length = value;
                if (DependencyContext.IsManaged(this))
                    Reference.FromPath(this, "Length").Contents = _Length;
            }
        }

        private double GetLength() => Math.Sqrt(Math.Pow(_X2 - _X1, 2) + Math.Pow(_Y2 - _Y1, 2));

        [SubContext]
        public Color Color { get; } = new Color();
    }

    internal class Color
    {
        [Property(true, true, "1.0", "red", "R")]
        public double R { get; set; }

        [Property(source: true, listener: true, initialContents: "0.8")]
        public double G { get; set; }

        [Property(source: true, listener: true, initialContents: "0.5")]
        public double B { get; set; }
    }

}
