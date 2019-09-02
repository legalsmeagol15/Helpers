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
using Dependency.Variables;
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
        public void Test_Indexing_Simple()
        {
            Common.AssertThrows<Dependency.Parse.SyntaxException>(() => Parse.FromString("no.references.without.context", null, null));
            
            SimpleContext root = new SimpleContext();
            Parse.FromString("what.is.this", null, root);

            // Show that new references have the correct values.
            Vector vec = new Vector(new Number(10), new Number(11), new Number(12));
            Variable v0 = new Variable { Contents = new Vector(new Number(10), new Number(11), new Number(12)) };
            root.Add("v0", v0);
            Variable v1 = new Variable(Parse.FromString("v0[2]", null, root));
            Assert.AreEqual(v1.Value, vec[2]);
            Variable v2 = new Variable(Parse.FromString("(v0[2] + 3) * 2", null, root));
            Assert.AreEqual(v2.Value, (((Number)vec[2]) + 3) * 2);

            // Show that value changes propogate through.
            vec = new Vector(new Number(20), new Number(21), new Number(22));
            v0.Contents = vec;
            Assert.AreEqual(v1.Value, vec[2]);
            Assert.AreEqual(v2.Value, (((Number)vec[2]) + 3) * 2);
            
            // Show that subcontext properties can be referenced in deeper paths.
            v1.Contents = Parse.FromString("v0", null, root);
            v2.Contents = Parse.FromString("v0", null, root);
            SimpleContext sub1 = new SimpleContext();
            root.Add("sub1", sub1);
            sub1.Add("v1", v1);
            SimpleContext sub2 = new SimpleContext();
            sub1.Add("sub2", sub2);
            sub2.Add("v2", v2);
            Variable v3 = new Variable(Parse.FromString("sub1.v1", null, root));
            Assert.AreEqual(v3.Value, vec);
            Variable v4 = new Variable(Parse.FromString("sub1.sub2.v2[2]", null, root));
            Assert.AreEqual(v4.Value, vec[2]);

            // Show that values propogate through deep paths.
            vec = new Vector(new Number(31), new Number(32), new Number(33));
            v0.Contents = vec;
            Assert.AreEqual(v3.Value, vec);
            Assert.AreEqual(v4.Value, vec[2]);

            // Mixed paths (both References and Indexes) will be tested elsewhere.
        }

        
        public void Test_Indexing_Complex()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void Test_Linear()
        {
            int numVars = 1000;
           
            // Test linear transmission  of a value by changing contents.
            Variable vStart = new Variable(Dependency.Number.One);
            Assert.AreEqual(Dependency.Number.One, vStart.Contents);
            Assert.AreEqual(Dependency.Number.One, vStart.Value);

            SimpleContext root = new SimpleContext();
            root.Add("v0", vStart);
            List<Variable> vars = new List<Variable>();
            for (int i = 1; i <= numVars; i++)
            {
                Variable vNext = new Variable(Parse.FromString("v" + (i-1), null,  root));
                Assert.AreEqual(vStart.Value, vNext.Value);
                root.Add("v" + i, vNext);
            }
            if (!root.TryGetProperty("v" + numVars, out IEvaluateable last_iev) || !(last_iev is Variable vLast))
                throw new Exception("Bad testing harness.");
            vStart.Contents = new Number(2);
            Assert.AreEqual(vStart.Value, vLast.Value);

            // Right now, numVars = 10,000 takes 22 sec.  Searching for circularity makes this a quadraticly-timed 
            // test.  Not acceptable.
            // TODO:  improve efficiency
        }

        [TestMethod]
        public void Test_Pancake()
        {
            int numVars = 10000;

            // Test linear transmission  of a value by changing contents.
            Variable vStart = new Variable(Dependency.Number.One);
            Assert.AreEqual(Dependency.Number.One, vStart.Contents);
            Assert.AreEqual(Dependency.Number.One, vStart.Value);

            SimpleContext root = new SimpleContext();
            root.Add("v0", vStart);
            List<Variable> vars = new List<Variable>();
            for (int i = 1; i <= numVars; i++)
            {
                Variable vNext = new Variable(Parse.FromString("v0", null, root));
                Assert.AreEqual(vStart.Value, vNext.Value);
                root.Add("v" + i, vNext);
            }
            if (!root.TryGetProperty("v" + numVars, out IEvaluateable last_iev) || !(last_iev is Variable vLast))
                throw new Exception("Bad testing harness.");
            vStart.Contents = new Number(2);
            Assert.AreEqual(vStart.Value, vLast.Value);

            // At present, numVars=10000 takes 225 ms.  Doing okay.  Yay for multithreading.
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
        private readonly Dictionary<object, SimpleContext> _Subcontexts = new Dictionary<object, SimpleContext>();
        public void Add(object key, Variable variable) => _Variables.Add(key, variable);
        public void Add(object key, SimpleContext subcontext) => _Subcontexts.Add(key, subcontext);
        
        public bool TryGetProperty(object token, out IEvaluateable source)
        {
            if (_Variables.TryGetValue(token, out Variable v)) { source = v; return true; }
            source = null;
            return false;
        }

        public  bool TryGetSubcontext(object token, out IContext ctxt)
        {
            if (_Subcontexts.TryGetValue(token, out SimpleContext sc)) { ctxt = sc; return true; }
            ctxt = null;
            return false;
        }
    }
}
