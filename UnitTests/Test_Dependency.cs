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
        [TestMethod]
        public void Test_Circularity_Detection()
        {
            SimpleContext root = new SimpleContext();
            Variable v0 = new Variable(), v1 = new Variable(), v2 = new Variable();
            root.Add("v0", v0);
            root.Add("v1", v1);
            root.Add("v2", v2);

            //v0 = v1
            v0.Contents = Parse.FromString("v1", null, root);

            //v1 = v2
            v1.Contents = Parse.FromString("v2", null, root);

            //v2 = v0
            Update update = Update.ForVariable(v2, Parse.FromString("v0", null, root));
            update.Execute();
            update.Await();

            Assert.IsInstanceOfType(v2.Value, typeof(CircularityError));
            Assert.IsInstanceOfType(v0.Value, typeof(CircularityError));
            Assert.IsInstanceOfType(v1.Value, typeof(CircularityError));

            CircularityError ce = (CircularityError)v0.Value;
            Assert.IsTrue(v1.Value.Equals(v0.Value));
            Assert.IsTrue(v2.Value.Equals(v0.Value));
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
            Assert.IsTrue(Dependency.Helpers.GetDependees(v1).Contains(v0));
            Variable v2 = new Variable(Parse.FromString("(v0[2] + 3) * 2", null, root));
            Assert.IsTrue(Dependency.Helpers.GetDependees(v2).Contains(v0));
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

        [TestMethod]
        public void Test_Indexing_Complex()
        {
            throw new NotImplementedException();
        }


        [TestMethod]
        public void Test_Linear()
        {
            // NOTE:  expect a long run time because setting up the line is an N^2 operation, but updates should be 
            // lightning-quick.

            // 401 ms update 1000 vars over 100 times, or 4 ms/run
            // 415 ms update 1000 vars over 100 times, or 4 ms/run
            // 567 ms update 1000 vars over 100 times, or 5 ms/run
            // 361 ms update 1000 vars over 100 times, or 3 ms/run
            // 376 ms update 1000 vars over 100 times, or 3 ms/run
            int numVars = 1000;
            bool timeUpdates = false;
           
            // Test linear transmission  of a value by changing contents.
            Variable vStart = new Variable(Dependency.Number.One);
            Assert.AreEqual(Dependency.Number.One, vStart.Contents);
            Assert.AreEqual(Dependency.Number.One, vStart.Value);

            SimpleContext root = new SimpleContext();
            root.Add("v0", vStart);
            List<Variable> vars = new List<Variable>();
            for (int i = 1; i <= numVars; i++)
            {
                Variable vNext = new Variable();
                IEvaluateable refer_to_last = Parse.FromString("v" + (i - 1), null, root);
                Update u = Update.ForVariable(vNext, refer_to_last);
                u.Execute();
                u.Await();
                
                Assert.AreEqual(vStart.Value, vNext.Value);
                root.Add("v" + i, vNext);
            }
            if (!root.TryGetProperty("v" + numVars, out IEvaluateable last_iev) || !(last_iev is Variable vLast))
                throw new Exception("Bad testing harness.");

            Update update = Update.ForVariable(vStart, new Number(2));
            update.Execute();
            update.Await();
            Assert.AreEqual(vStart.Value, vLast.Value);
            

            if (timeUpdates)
            {
                for (int k = 0; k < 5; k++)
                {
                    long ms = 0;
                    long runs = 100;
                    for (int i = 0; i < runs; i++)
                    {
                        ms += Common.Time((j) => { vStart.Contents = new Number(j);  }, i);
                    }
                    Console.WriteLine("// " + ms + " ms update " + numVars + " vars over " + runs + " times, or " + (ms / runs) + " ms/run");
                }
            }
        }

        [TestMethod]
        public void Test_Pancake()
        {
            // 76 ms update 1000 vars over 100 times, or 0 ms/run
            // 95 ms update 1000 vars over 100 times, or 0 ms/run
            // 91 ms update 1000 vars over 100 times, or 0 ms/run
            // 43 ms update 1000 vars over 100 times, or 0 ms/run
            // 36 ms update 1000 vars over 100 times, or 0 ms/run
            int numVars = 1000;
            bool timeUpdates = false;

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

            if (timeUpdates)
            {
                for (int k = 0; k < 5; k++)
                {
                    long ms = 0;
                    long runs = 100;
                    for (int i = 0; i < runs; i++)
                    {
                        ms += Common.Time((j) => { vStart.Contents = new Number(j);  }, i);
                    }
                    Console.WriteLine("// " + ms + " ms update " + numVars + " vars over " + runs + " times, or " + (ms / runs) + " ms/run");
                }
            }
        }

        [TestMethod]
        public void Test_Named_Functions()
        {
            IFunctionFactory functions = new Dependency.Functions.ReflectedFunctionFactory();
            IEvaluateable exp0 = Parse.FromString("ABS(11)", functions);
            IEvaluateable exp1 = Parse.FromString("ABS(-7)", functions);
            Dependency.Helpers.Recalculate(exp0);
            Dependency.Helpers.Recalculate(exp1);
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

            Dependency.Helpers.Recalculate(exp0);
            Dependency.Helpers.Recalculate(exp1);
            Dependency.Helpers.Recalculate(exp2);
            Dependency.Helpers.Recalculate(exp3);

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



        [TestMethod]
        public void Test_Triangle()
        {
            int numVars = 1000;
            bool timeUpdates = true;

            IFunctionFactory functions = new Dependency.Functions.ReflectedFunctionFactory();

            Variable vStart = new Variable(Dependency.Number.One);
            SimpleContext root = new SimpleContext();
            root.Add("vstart", vStart);
            
            Assert.AreEqual(Dependency.Number.One, vStart.Contents);
            Assert.AreEqual(Dependency.Number.One, vStart.Value);

            int vars = 0;
            int rank = 0;
            List<KeyValuePair<string, Variable>> lastRank = new List<KeyValuePair<string, Variable>>
            {
                new KeyValuePair<string, Variable>("vstart", vStart)
            };

            while (vars < numVars)
            {
                List<KeyValuePair<string, Variable>> thisRank = new List<KeyValuePair<string, Variable>>();
                foreach (var kvp in lastRank)
                {
                    string lastName = kvp.Key;
                    Variable lastVar = kvp.Value;

                    string expressionA = "ABS(" + lastName + ")";
                    string expressionB = "-ABS(" + lastName + ")";

                    IEvaluateable contentsA = Parse.FromString( expressionA, functions, root);
                    IEvaluateable contentsB = Parse.FromString(expressionB, functions, root);
                    
                    Variable vA = new Variable(contentsA);
                    Variable vB = new Variable(contentsB);

                    string aName = "v_" + rank + "_" + vars + "a";
                    string bName = "v_" + rank + "_" + vars + "b";
                    vars += 2;

                    root.Add(aName, vA);
                    root.Add(bName, vB);
                    thisRank.Add(new KeyValuePair<string, Variable>(aName, vA));
                    thisRank.Add(new KeyValuePair<string, Variable>(bName, vB));

                    Assert.AreEqual(vA.Value, 1);
                    Assert.AreEqual(vB.Value, -1);
                }
                rank++;
                lastRank = thisRank;
            }

            Update update = Update.ForVariable(vStart, new Number(-1));
            update.Execute();
            update.Await();
            
            for (int i = 0; i < lastRank.Count; i += 2)
            {
                Variable vA = lastRank[i].Value;
                Variable vB = lastRank[i+1].Value;
                Assert.AreEqual(vA.Value, 1);
                Assert.AreEqual(vB.Value, -1);
            }

            vStart.Contents = new Number(2);            
            for (int i = 0; i < lastRank.Count; i += 2)
            {
                Variable vA = lastRank[i].Value;
                Variable vB = lastRank[i + 1].Value;
                Assert.AreEqual(vA.Value, 2);
                Assert.AreEqual(vB.Value, -2);
            }

            vStart.Contents = new Number(0);            
            for (int i = 0; i < lastRank.Count; i += 2)
            {
                Variable vA = lastRank[i].Value;
                Variable vB = lastRank[i + 1].Value;
                Assert.AreEqual(vA.Value, 0);
                Assert.AreEqual(vB.Value, 0);
            }

            if (timeUpdates)
            {
                for (int k = 0; k < 5; k++)
                {
                    long ms = 0;
                    long runs = 100;
                    int val = 1;
                    for (int i = 0; i < runs; i++)
                    {
                        ms += Common.Time((j) => { vStart.Contents = new Number(val);  }, i);
                        val *= -1;
                    }
                    Console.WriteLine("// " + ms + " ms update " + numVars + " vars over " + runs + " times, or " + (ms / runs) + " ms/run");
                }
            }
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

        public override bool Equals(object obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();
    }
}
