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
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;

namespace UnitTests
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class Test_Dependency
    {
        [TestMethod]
        public void Test_Circularity_Detection()
        {
            TestConext root = new TestConext();
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

            TestConext root = new TestConext();
            Parse.FromString("what.is.this", null, root);

            // Show that new references have the correct values.
            Vector vec = new Vector(10, 11, 12);
            Variable v0 = new Variable { Contents = vec };
            root.Add("v0", v0);
            Variable v1 = new Variable();
            IEvaluateable idxing = Parse.FromString("v0[2]", null, root);
            v1.Contents = idxing;
            Assert.AreEqual(v1.Value, vec[2]);


            // Show that value changes propogate through.
            vec = new Vector(20, 21, 22);
            Update update = Update.ForVariable(v0, vec);
            update.Execute();
            Assert.AreEqual(v1.Value, vec[2]);

            // Show that the values will propogate through a second variable as well.
            Variable v2 = new Variable(Parse.FromString("(v0[2] + 3) * 2", null, root));
            //Assert.IsTrue(Dependency.Helpers.GetDependees(v2).Contains(v0));
            Assert.AreEqual(v2.Value, (((Number)vec[2]) + 3) * 2);

            // Show that subcontext properties can be referenced in deeper paths.
            v1.Contents = Parse.FromString("v0", null, root);
            v2.Contents = Parse.FromString("v0", null, root);
            TestConext sub1 = new TestConext();
            root.Add("sub1", sub1);
            sub1.Add("v1", v1);
            IEvaluateable ref_sub1_v1 = Parse.FromString("sub1.v1", null, root);
            Variable v3 = new Variable();
            update = Update.ForVariable(v3, ref_sub1_v1);
            update.Execute();
            Assert.AreEqual(v3.Value, vec);
            vec = new Vector(31, 32, 33);
            v0.Contents = vec;
            Assert.AreEqual(v3.Value, vec);

            TestConext sub2 = new TestConext();
            sub1.Add("sub2", sub2);
            sub2.Add("v2", v2);
            Variable v4 = new Variable(Parse.FromString("sub1.sub2.v2[2]", null, root));

            Assert.AreEqual(v4.Value, vec[2]);

            // Show that values propogate through deep paths.
            vec = new Vector(31, 32, 33);
            v0.Contents = vec;

            Assert.AreEqual(v3.Value, vec);
            Assert.AreEqual(v4.Value, vec[2]);

            // Mixed paths (both References and Indexes) will be tested elsewhere.
        }

        [TestMethod]
        public void Test_Indexing_Complex()
        {
            // The point of this method is to test the the functionality of the follow complex expression:
            // drawings[spreadsheet.c5].splineA.Xs.count + 5

            //setup
            var master = new TestConext();
            var drawings = new TestConext();
            master.Add("drawings", drawings);
            var head = new TestConext();
            drawings[new Number(0)] = new Number(20);
            var spreadsheet = new TestConext();
            drawings[new Number(1)] = head;
            
            var c5 = new Variable(Number.Zero);            
            spreadsheet.Add("c5", c5);

            var splineA = new TestConext();
            head.Add("splineA", splineA);

            var Xs = new TestConext();
            splineA.Add("Xs", Xs);

            Variable count = new Variable(new Number(10));
            Xs.Add("count", count);

            // Now try instantiation.
            Variable host = new Variable();
            host.Contents = Parse.FromString("drawings[spreadsheet.c5].splineA.Xs.count + 5",null , master);
            
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

            TestConext root = new TestConext();
            root.Add("v0", vStart);
            System.Collections.Generic.List<Variable> vars = new System.Collections.Generic.List<Variable>();
            for (int i = 1; i <= numVars; i++)
            {
                Variable vNext = new Variable();
                IEvaluateable refer_to_last = Parse.FromString("v" + (i - 1), null, root);
                
                Update u = Update.ForVariable(vNext, refer_to_last);
                u.Execute();

                Assert.AreEqual(vStart.Value, vNext.Value);
                root.Add("v" + i, vNext);
            }
            if (!root.TryGetProperty("v" + numVars, out IEvaluateable last_iev) || !(last_iev is Variable vLast))
                throw new Exception("Bad testing harness.");

            Update update = Update.ForVariable(vStart, new Number(2));
            update.Execute();
            Assert.AreEqual(vStart.Value, vLast.Value);


            if (timeUpdates)
            {
                for (int k = 0; k < 5; k++)
                {
                    long ms = 0;
                    long runs = 100;
                    for (int i = 0; i < runs; i++)
                    {
                        ms += Common.Time((j) => { vStart.Contents = new Number(j); }, i);
                    }
                    Console.WriteLine("// " + ms + " ms update " + numVars + " vars over " + runs + " times, or " + (ms / runs) + " ms/run");
                }
            }
        }
        
        [TestMethod]
        public void Test_List()
        {
            IEvaluateable updateValueOld, updateValueNew;
            int updateCount = 0;

            //Dependency.Variables.List<int> list = new Dependency.Variables.List<int>();
            //list.Updated += _List_Updated;

            //Assert.AreEqual(0, list.Count);
            //list.Add(0);
            //Assert.AreEqual(1, list.Count);

            void _List_Updated(object sender, Helpers.ValueChangedArgs<IEvaluateable> e)
            {
                updateCount++;
                updateValueOld = e.Before;
                updateValueNew = e.After;
            }
        }

        

        [TestMethod]
        public void Test_Numbers()
        {
            // Testing numbers
            Number x = new Number(5);
            Number y = new Number(15);
            Assert.IsTrue(x + y == 20);
            Assert.IsTrue(-x == -5);
            Assert.IsTrue(x - y == -10);
            Assert.IsTrue(x * y == 75);
            Assert.IsTrue(y / x == 3);

            AssertThrows<InvalidCastException>(() => new Number(double.NegativeInfinity));
            AssertThrows<InvalidCastException>(() => new Number(double.PositiveInfinity));
            AssertThrows<InvalidCastException>(() => new Number(double.NaN));

            Assert.IsTrue(new Number(3d) == 3);
            Assert.IsTrue(new Number(3m) == 3);
            AssertNoThrow(() => { int i = 3; Number num = i; });
            AssertNoThrow(() => { double d = 3.5; Number num = d; });
            AssertNoThrow(() => { decimal m = 3.5m; Number num = m; });
            AssertNoThrow(() => { bool boolean = true; Number num = boolean; });
            AssertNoThrow(() => { byte by = 17; Number num = by; });

            Number nd = (Number)Number.FromDouble(5.5d);
            Assert.IsTrue(nd == 5.5d);
            Assert.IsInstanceOfType(Number.FromDouble(double.NaN), typeof(InvalidValueError));
            Assert.IsInstanceOfType(Number.FromDouble(double.NegativeInfinity), typeof(InvalidValueError));
            Assert.IsInstanceOfType(Number.FromDouble(double.PositiveInfinity), typeof(InvalidValueError));

            Assert.IsTrue(new Number(3).IsInteger);
            Assert.IsFalse(new Number(4.5).IsInteger);

            bool some_bool = true;
            Number n = some_bool;
            Assert.IsTrue(n == 1);
            some_bool = false;
            n = some_bool;
            Assert.IsTrue(n == 0);

            Assert.IsTrue(Number.TryParse("4", out Number parsed0) && parsed0 == 4);
            Assert.IsTrue(Number.TryParse("4.5", out Number parsed1) && parsed1 == 4.5);
            Assert.IsTrue(Number.TryParse("-5.5", out Number parsed2) && parsed2 == -5.5);
            Assert.IsFalse(Number.TryParse("three", out Number parsed3) && parsed3 == 0);

            Assert.AreEqual(new Number(2) ^ new Number(3), 8);
            Assert.IsTrue(new Number(5) == 5m);
            Assert.IsTrue(new Number(6) == 6d);
            Assert.IsTrue(new Number(14).Equals(14));
            Assert.IsTrue(new Number(15).Equals(15m));
            Assert.IsTrue(new Number(16).Equals(16d));

            Assert.IsTrue(new Number(7) != -7);
            Assert.IsTrue(new Number(8) != -8m);
            Assert.IsTrue(new Number(9) != -9d);
            Assert.IsTrue(new Number(29.5) != 29);

            Assert.IsTrue(new Number(10) < new Number(11));
            Assert.IsFalse(new Number(12) > new Number(13));

            AssertNoThrow(() => { int i = new Number(17); });
            AssertNoThrow(() => { decimal m = new Number(18); });
            AssertNoThrow(() => { double d = new Number(19); });
            AssertNoThrow(() => { bool b = new Number(20); });
            AssertNoThrow(() => { byte b = new Number(21); });

            Assert.IsTrue(new Number(22) == new Number(22));
            Assert.IsTrue(new Number(23) != new Number(24));

            Assert.IsTrue(new Number(24).Equals(new Number(24)));
            Assert.IsFalse(new Number(25).Equals(new TestConext()));

            Assert.AreEqual(new Number(27).ToString(), "27");

            Number n_hash = new Number(28);
            decimal m_hash = 28m;
            Assert.AreEqual(n_hash.GetHashCode(), m_hash.GetHashCode());
            m_hash = 28.5m;
            Assert.AreNotEqual(n_hash.GetHashCode(), m_hash.GetHashCode());
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

            TestConext root = new TestConext();
            root.Add("v0", vStart);
            System.Collections.Generic.List<Variable> vars = new System.Collections.Generic.List<Variable>();
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
                        ms += Common.Time((j) => { vStart.Contents = new Number(j); }, i);
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
        public void Test_Regex()
        {
            // It's a complicated regex I use to break up strings.  Here's where it is tested.
            FieldInfo regexField = typeof(Dependency.Parse).GetField("_Regex", BindingFlags.NonPublic | BindingFlags.Static);
            Regex regex = (Regex)regexField.GetValue(null);
            _AssertPatterns("3x", "3", "numPattern", "x", "referencePattern");

            void _AssertPatterns(string str, params string[] patterns)
            {
                MatchCollection matches = regex.Matches(str);
                HashSet<string> validNames = new HashSet<string>(regex.GetGroupNames().Where(name => !Int32.TryParse(name, out int _)));

                int i = 0;
                Assert.IsTrue(matches.Count*2 <= patterns.Length, "More matches than expected.");
                Assert.IsTrue(matches.Count*2 >= patterns.Length, "Fewer matches than expected.");
                foreach (Match m in matches)
                {
                    string matchedGroup = null;
                    string expected = patterns[i * 2], pattern = patterns[(i * 2) + 1];
                    if (!validNames.Contains(pattern))
                        throw new ArgumentException("Pattern \"" + pattern + "\" is not a valid pattern.  Select from: " + string.Join(",", validNames));
                    foreach (string gname in validNames)
                    {
                        string value = m.Groups[gname].Value;
                        if (string.IsNullOrWhiteSpace(value)) continue;
                        Assert.AreEqual(expected, value, "Expected string did not match.");                        
                        Assert.IsNull(matchedGroup, "Group \"" + matchedGroup + "\" already matched \"" + expected + "\".  Group \"" + gname + "\" cannot also do so.");
                        Assert.AreEqual(pattern, gname, "Expected pattern wrong for match \"" + expected + "\"");
                        matchedGroup = gname;
                    }
                    Assert.IsNotNull(matchedGroup, "Could not determine matched group for \"" + expected + "\"");
                    i++;
                }
                
                
            }
        }
        private class MatchSorter : IComparer<Match>
        {
            int IComparer<Match>.Compare(Match x, Match y) => x.Index.CompareTo(y.Index);
        }

        [TestMethod]
        public void Test_Struct()
        {
            Struct<Mathematics.Geometry.VectorN> host = new Struct<Mathematics.Geometry.VectorN>();
            AssertThrows<ArgumentException>(() => { object bad = host["no_exists"]; });

            Variable xVar = (Variable)host["X"], yVar = (Variable)host["Y"];
            Assert.AreEqual(xVar.Value, 0);
            Assert.AreEqual(xVar.Contents, 0);
            Assert.AreEqual(yVar.Value, 0);
            Assert.AreEqual(yVar.Contents, 0);
            Assert.AreEqual(host.Value, new Dependency.Vector(0, 0));
            Assert.AreEqual(host.Contents, new Dependency.Vector(0, 0));

            host.Native = new Mathematics.Geometry.VectorN(1, 2);
            Assert.AreEqual(xVar.Value, 1);
            Assert.AreEqual(xVar.Contents, 1);
            Assert.AreEqual(yVar.Value, 2);
            Assert.AreEqual(yVar.Contents, 2);
            Assert.AreEqual(host.Value, new Dependency.Vector(1, 2));
            Assert.AreEqual(host.Contents, new Dependency.Vector(1, 2));


            xVar.Contents = new Number(3);
            Assert.AreEqual(xVar.Value, 3);
            Assert.AreEqual(xVar.Contents, 3);
            Assert.AreEqual(yVar.Value, 2);
            Assert.AreEqual(yVar.Contents, 2);
            Assert.AreEqual(host.Value, new Dependency.Vector(3, 2));
            Assert.AreEqual(host.Contents, new Dependency.Vector(3, 2));
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
            TestConext root = new TestConext();
            root.Add("vstart", vStart);

            Assert.AreEqual(Dependency.Number.One, vStart.Contents);
            Assert.AreEqual(Dependency.Number.One, vStart.Value);

            int vars = 0;
            int rank = 0;
            System.Collections.Generic.List<KeyValuePair<string, Variable>> lastRank = new System.Collections.Generic.List<KeyValuePair<string, Variable>>
            {
                new KeyValuePair<string, Variable>("vstart", vStart)
            };

            while (vars < numVars)
            {
                System.Collections.Generic.List<KeyValuePair<string, Variable>> thisRank = new System.Collections.Generic.List<KeyValuePair<string, Variable>>();
                foreach (var kvp in lastRank)
                {
                    string lastName = kvp.Key;
                    Variable lastVar = kvp.Value;

                    string expressionA = "ABS(" + lastName + ")";
                    string expressionB = "-ABS(" + lastName + ")";

                    IEvaluateable contentsA = Parse.FromString(expressionA, functions, root);
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

            for (int i = 0; i < lastRank.Count; i += 2)
            {
                Variable vA = lastRank[i].Value;
                Variable vB = lastRank[i + 1].Value;
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
                        ms += Common.Time((j) => { vStart.Contents = new Number(val); }, i);
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
                    Console.Write("\t" + (timings[i] / timings[i - 1]));
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




    [ExcludeFromCodeCoverage]
    internal class Brush : ISubcontext
    {
        IContext ISubcontext.Parent { get; set; }

        private readonly Variable _WidthVar = new Variable(new Number(2));
        private readonly Variable _PatternVar = new Variable(new Number(0.25));

        bool IContext.TryGetProperty(string  token, out IEvaluateable source)
        {
            switch (token)
            {
                case "width": source = _WidthVar; return true;
                case "pattern": source = _PatternVar; return true;
                default: source = null; return false;
            }
        }

        bool IContext.TryGetSubcontext(string token, out IContext ctxt) { ctxt = null; return false; }
    }


    [ExcludeFromCodeCoverage]
    [DebuggerStepThrough]
    internal class TestConext : IContext, IIndexed, IEvaluateable
    {
        private readonly Dictionary<IEvaluateable, IEvaluateable> _Indices = new Dictionary<IEvaluateable, IEvaluateable>();
        private readonly Dictionary<object, Variable> _Variables = new Dictionary<object, Variable>();
        private readonly Dictionary<object, TestConext> _Subcontexts = new Dictionary<object, TestConext>();

        IEvaluateable IEvaluateable.Value => this;

        public void Add(object key, Variable variable) => _Variables.Add(key, variable);
        public void Add(object key, TestConext subcontext) => _Subcontexts.Add(key, subcontext);

        public bool TryGetProperty(string  token, out IEvaluateable source)
        {
            if (_Variables.TryGetValue(token, out Variable v)) { source = v; return true; }
            source = null;
            return false;
        }

        public bool TryGetSubcontext(string token, out IContext ctxt)
        {
            if (_Subcontexts.TryGetValue(token, out TestConext sc)) { ctxt = sc; return true; }
            ctxt = null;
            return false;
        }

        public override bool Equals(object obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();

        public bool TryIndex(IEvaluateable ordinal, out IEvaluateable val)
            => _Indices.TryGetValue(ordinal, out val);

        public IEvaluateable this[IEvaluateable ordinal]
        {
            get => _Indices[ordinal];
            set { _Indices[ordinal] = value; }
        }

    }
}
