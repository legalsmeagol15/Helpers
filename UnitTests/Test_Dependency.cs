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
using static UnitTests.Common;
using System.Runtime.CompilerServices;

namespace UnitTests
{
   


    [TestClass]
    public class Test_Dependency
    {
        /// <summary>
        /// Returns the elapsed time that results from updating the dependency tree that begins at 
        /// <paramref name="vStart"/>, by updating the variables's <see cref="Variable.Contents"/> 
        /// the given number of times.
        /// </summary>
        private static  TimeSpan Time(Variable vStart, int timings)
        {
            vStart.Contents = new Number(-100);  // Warmup

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
            for (int i = 0; i < timings; i++)
            {
                Number num = new Number(i);
            }
            DateTime overheadEnd = DateTime.Now;

            return (end - start) - (overheadEnd - overheadStart);
        }

        [TestMethod]
        public void Test_Indexing()
        {
            Parse.FromString("what.is.this", null, null);
            SimpleContext ctxt = new SimpleContext();
            Vector vec = new Vector(new Number(10), new Number(11), new Number(12));
            Variable v0 = new Variable { Contents = new Vector(new Number(10), new Number(11), new Number(12)) };
            ctxt.Add("v0", v0);
            Variable v1 = new Variable(Parse.FromString("v0[2]", null, ctxt));
            ctxt.Add("v1", v1);
            Assert.AreEqual(v1.Value, vec[2]);
        }

        [TestMethod]
        public void Test_Linear()
        {
            int numVars = 100;
            int timings = 0;
            int timingSizes = 13;

            // Test linear transmission  of a value by changing contents.
            Variable vStart = new Variable(Dependency.Number.One);
            Assert.AreEqual(Dependency.Number.One, vStart.Contents);
            Assert.AreEqual(Dependency.Number.One, vStart.Value);
            
            Variable vLast = vStart;
            List<Variable> vars = new List<Variable>();
            for (int i = 0; i < numVars; i++)
            {
                Variable vNext = new Variable(vLast);
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
                Variable vBegin = new Variable();
                int n = Mathematics.Int32.Exp_2(timingSizes);
                Variable vEnd = vBegin;
                List<Variable> keepItAlive = new List<Variable>();
                for (int i = 0; i < n; i++)
                {
                    Variable vNext = new Variable(vEnd);
                    keepItAlive.Add(vNext);
                    vEnd = vNext;
                }
                
                var duration = Time(vBegin, timings);
                Console.WriteLine("n=" + n + ", duration=" + duration + " for " + timings + " runs.");
                timingSizes--;
            }            
        }

        [TestMethod]
        public void Test_Pancake()
        {

            int numVars = 100;
            int timings = 0; //1000;
            int timingSizes = 13;

            Variable vStart = new Variable( );
            List<Variable> noGC = new List<Variable>();
            for (int i = 0; i < numVars; i++)
            {
                noGC.Add(new Variable(vStart));
            }

            for (int i = 0; i < numVars; i++)
            {
                Assert.AreEqual(Dependency.Null.Instance, noGC[i].Value);
                Assert.AreNotEqual(Dependency.Number.One, noGC[i].Value);
            }

            vStart.Contents = Dependency.Number.One;

            for (int i = 0; i < numVars; i++)
            {
                Assert.AreNotEqual(Dependency.Null.Instance, noGC[i].Value);
                Assert.AreEqual(Dependency.Number.One, noGC[i].Value);
            }


            // Do a timing
            while (timingSizes > 0 && timings > 0)
            {
                // Set up a chain to experiment on.
                Variable vBegin = new Variable();
                int n = Mathematics.Int32.Exp_2(timingSizes);
                List<Variable> keepItAlive = new List<Variable>();
                for (int i = 0; i < n; i++)
                {
                    keepItAlive.Add(new Variable( vBegin));
                }

                var duration = Time(vBegin, timings);
                Console.WriteLine("n=" + n + ", duration=" + duration + " for " + timings + " runs.");
                timingSizes--;
            }
        }

        [TestMethod]
        public void Test_Named_Functions()
        {
            IFunctionFactory functions = new Dependency.Functions.ReflectedFunctionFactory();
            IEvaluateable exp0 = Parse.FromString("ABS(11)", functions);
            IEvaluateable exp1 = Parse.FromString("ABS(-7)", functions);
            Assert.AreEqual(exp0.Value, 11);
            Assert.AreEqual(exp1.Value, 7);
        }

        [TestMethod]
        public void Test_Parsing()
        {
            IEvaluateable exp0 = Parse.FromString("3-5+2^3/4*-7-1");    // -17
            IEvaluateable exp1 = Parse.FromString("(3-5+2^3/4*-7-1)");  // Still -17
            IEvaluateable exp2 = Parse.FromString("(3-5+(2^3)/4*-7-1)"); // Still -17
            IEvaluateable exp3 = Parse.FromString("(3-5+( ( (2^3) /4) *-7)-1)"); // Still -17

            Assert.AreEqual(exp0.Value, -17);
            Assert.AreEqual(exp1.Value, -17);
            Assert.AreEqual(exp2.Value, -17);
            Assert.AreEqual(exp3.Value, -17);

            IEvaluateable vector0 = Parse.FromString("{2,4,6}");

            //var timings = DoTiming(() => exp1.UpdateValue(), 512, 16);
            //PrintTimings(timings);
        }

        
        [TestMethod]
        public void Test_ToString()
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

        private readonly Variable _WidthVar = new Variable(new Number(2));
        private readonly Variable _PatternVar = new Variable(new Number(0.25));

        bool IContext.TryGetProperty(object token, out IEvaluateable source)
        {
            switch (token)
            {
                case "width": source = _WidthVar; return true;
                case "pattern": source = _PatternVar; return true;
                default: source = null; return false;
            }
        }

        bool IContext.TryGetSubcontext(object token, out IContext ctxt) { ctxt = null; return false; }
    }
    


    internal class SimpleContext : IContext
    {
        private readonly Dictionary<object, Variable> _Variables = new Dictionary<object, Variable>();
        public void Add(object key, Variable variable) => _Variables.Add(key, variable);

        public Variable Get(object key)
        {
            return _Variables[key];
        }

        bool IContext.TryGetProperty(object token, out IEvaluateable source)
        {
            if (_Variables.TryGetValue(token, out Variable v)) { source = v; return true; }
            source = null;
            return false;
        }

        bool IContext.TryGetSubcontext(object token, out IContext ctxt) { ctxt = null; return false; }
    }
}
