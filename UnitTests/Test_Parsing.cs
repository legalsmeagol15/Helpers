using System;
using Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using static Parsing.Context;
using System.Reflection;
using System.Text;

namespace UnitTests
{
    [TestClass]
    public class Test_Parsing
    {
        private Function.Factory factory;

        [TestInitialize]
        public void TestParsing__Initialize()
        {
            factory = Function.Factory.StandardFactory;
        }


        [TestMethod]
        public void TestParsing_Constant_Functions()
        {
            DummyContext context = new DummyContext(null, "dummy ctxt");

            Function f1 = factory["PI"];
            Function f2 = factory["PI"];
            Assert.IsTrue(ReferenceEquals(f1, f2));
            Assert.AreEqual(factory["PI"].Evaluate(), (decimal)Math.PI);
            Assert.AreEqual(factory["E"].Evaluate(), (decimal)Math.E);

            IEvaluateable pi = Expression.FromString("PI", context).Commit();
            Assert.AreEqual("PI", pi.ToString());
            Assert.AreEqual(pi.Evaluate(), (decimal)Math.PI);
            pi = Expression.FromString("PI()", context).Commit();
            Assert.AreEqual("PI", pi.ToString());
            Assert.AreEqual(pi.Evaluate(), (decimal)Math.PI);

            IEvaluateable e = Expression.FromString("E", context).Commit();
            Assert.AreEqual("E", e.ToString());
            Assert.AreEqual(e.Evaluate(), (decimal)Math.E);
            e = Expression.FromString("E()", context).Commit();
            Assert.AreEqual("E", e.ToString());
            Assert.AreEqual(e.Evaluate(), (decimal)Math.E);

        }

        [TestMethod]
        public void TestParsing_Contexts()
        {
            DummyContext dummyA = new DummyContext(null, "dummy_context_a");
            Variable varA = (Variable)Expression.FromString("a", dummyA).Commit();
            Variable varB = (Variable) Expression.FromString("dummy_context_b.b", dummyA).Commit();
            Variable varC = (Variable)Expression.FromString("dummy_context_b.dummy_context_c.c").Commit();
            
            varA.Contents = "10";
            varB.Contents = "5";
            varC.Contents = "2";
            

            IEvaluateable exp = Expression.FromString("dummy_context_b.dummy_context_c.c", dummyA).Commit();
            Assert.AreEqual(exp, 2);

            exp = Expression.FromString("a + dummy_context_b.b + dummy_context_b.dummy_context_c.c", dummyA).Commit();
            Assert.AreEqual(exp.Evaluate(), 21);

            // This will only work if variables are scoped for all subcontexts:
            //                                                         vvv                                 vvv
            exp = Expression.FromString("dummy_context_b.dummy_context_c.c + dummy_context_b.dummy_context_c.b", dummyA).Commit();
            Assert.AreEqual(exp.Evaluate(), 7);

        }


        [TestMethod]
        public void TestParsing_Function_Factory()
        {
            Assert.IsTrue(factory.Contains("Addition"));
            Assert.IsTrue(factory.Contains("Subtraction"));
            Assert.IsTrue(factory.Contains("Multiplication"));
            Assert.IsTrue(factory.Contains("Division"));
            Assert.IsTrue(factory.Contains("Exponentiation"));
            Assert.IsTrue(factory.Contains("Negation"));
            Assert.IsTrue(factory.Contains("Subtraction"));
            Assert.IsTrue(factory.Contains("Relation"));
            Assert.IsTrue(factory.Contains("Span"));
            Assert.IsTrue(factory.Contains("And"));
            Assert.IsTrue(factory.Contains("Or"));
            Assert.IsTrue(factory.Contains("PI"));
            Assert.IsTrue(factory.Contains("E"));


        }



        [TestMethod]
        public void TestParsing_Nesting()
        {
            IEvaluateable e = Expression.FromString("2+1", null).Commit();
            Assert.AreEqual("2 + 1", e.ToString());
            Assert.AreEqual(e.Evaluate(), 3);

            e = Expression.FromString("3+(2+1)", null).Commit();
            Assert.AreEqual("3 + ( 2 + 1 )", e.ToString());
            Assert.AreEqual(e.Evaluate(), 6);

            e = Expression.FromString("(3+2)+1", null).Commit();
            Assert.AreEqual("( 3 + 2 ) + 1", e.ToString());
            Assert.AreEqual(e.Evaluate(), 6);

            e = Expression.FromString("(4+3)", null).Commit();
            Assert.AreEqual("( 4 + 3 )", e.ToString());
            Assert.AreEqual(e.Evaluate(), 7);

            e = Expression.FromString("((4+3))", null).Commit();
            Assert.AreEqual("( ( 4 + 3 ) )", e.ToString());
            Assert.AreEqual(e.Evaluate(), 7);

            e = Expression.FromString("((3+2))+1", null).Commit();
            Assert.AreEqual("( ( 3 + 2 ) ) + 1", e.ToString());
            Assert.AreEqual(e.Evaluate(), 6);

            e = Expression.FromString("3+((2+1))", null).Commit();
            Assert.AreEqual("3 + ( ( 2 + 1 ) )", e.ToString());
            Assert.AreEqual(e.Evaluate(), 6);
        }



        [TestMethod]
        public void TestParsing_Number_Equalities()
        {
            Function adder = factory["Addition"];
            // Test that functions are correctly doing calculations.
            Assert.IsTrue(adder.Evaluate(Number.One, Number.One).Equals(2m));
            Assert.IsTrue(adder.Evaluate(Number.One, Number.One).Equals(2));
            Assert.IsTrue(adder.Evaluate(Number.One, Number.One).Equals(2d));
            Assert.AreEqual(adder.Evaluate(Number.One, Number.One), 2);

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
            // Disable obnoxious warning about variable compared to itself
            Assert.IsTrue(n1 == n1);
            Assert.IsTrue(n2 == n2);
            Assert.IsFalse(n1 < n1);
            Assert.IsFalse(n1 > n1);
#pragma warning restore CS1718

        }


        [TestMethod]
        public void TestParsing_Operators()
        {
            // Addition
            IEvaluateable e = Expression.FromString("5+4", null).Commit();
            Assert.AreEqual(e.Evaluate(), 9m);
            e = Expression.FromString("5+-4", null).Commit();
            Assert.AreEqual(e.Evaluate(), 1);
            e = Expression.FromString("-5+4", null).Commit();
            Assert.AreEqual(e.Evaluate(), -1);
            e = Expression.FromString("-5+-4", null).Commit();
            Assert.AreEqual(e.Evaluate(), -9);

            // Subtraction
            e = Expression.FromString("5-4", null).Commit();
            Assert.AreEqual(e.Evaluate(), 1);
            e = Expression.FromString("5--4", null).Commit();
            Assert.AreEqual(e.Evaluate(), 9);
            e = Expression.FromString("-5-4", null).Commit();
            Assert.AreEqual(e.Evaluate(), -9);
            e = Expression.FromString("-5--4", null).Commit();
            Assert.AreEqual(e.Evaluate(), -1);

            // Multiplication
            e = Expression.FromString("5*4", null).Commit();
            Assert.AreEqual(e.Evaluate(), 20);
            e = Expression.FromString("5*-4", null).Commit();
            Assert.AreEqual(e.Evaluate(), -20);
            e = Expression.FromString("-5*4", null).Commit();
            Assert.AreEqual(e.Evaluate(), -20);
            e = Expression.FromString("-5*-4", null).Commit();
            Assert.AreEqual(e.Evaluate(), 20);

            // Division
            e = Expression.FromString("5/4", null).Commit();
            Assert.AreEqual(e.Evaluate(), 1.25);
            e = Expression.FromString("5/-4", null).Commit();
            Assert.AreEqual(e.Evaluate(), -1.25);
            e = Expression.FromString("-5/4", null).Commit();
            Assert.AreEqual(e.Evaluate(), -1.25);
            e = Expression.FromString("-5/-4", null).Commit();
            Assert.AreEqual(e.Evaluate(), 1.25);

            // Exponentiation
            e = Expression.FromString("2^4", null).Commit();
            Assert.AreEqual(e.Evaluate(), 16);
        }


        [TestMethod]
        public void TestParsing_Serialization()
        {
            Context context = new DummyContext(null, "root");
            Variable aOrigin = (Variable)Expression.FromString("a", context).Commit();
            aOrigin.Contents = "2";
            IEvaluateable exp1 = Expression.FromString("3 + 5 * a ^ 2 / 4 - -1", context).Commit();


            MemoryStream outStream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(outStream, context);


            outStream.Seek(0, SeekOrigin.Begin);
            formatter = new BinaryFormatter();
            DummyContext deser = (DummyContext)formatter.Deserialize(outStream);

            Variable aPrime = (Variable)Expression.FromString("a", deser).Commit();
            Assert.AreEqual(aPrime.Name, "a");
            Assert.AreEqual(aPrime.Evaluate(), 2);

        }


        [TestMethod]
        public void TestParsing_Variables()
        {
            Context context = new DummyContext(null, "dummyContext Test_Parsing_Variables");            
            Variable varA = (Variable)Expression.FromString("a", context).Commit();
            Variable varB = (Variable)Expression.FromString("b", context).Commit();
            Variable varC = (Variable)Expression.FromString("c", context).Commit();
            Variable varA2 = (Variable)Expression.FromString("a", context).Commit();
            Variable varB2 = (Variable)Expression.FromString("b", context).Commit();
            Variable varC2 = (Variable)Expression.FromString("c", context).Commit();
            Assert.IsTrue(ReferenceEquals(varA, varA2));
            Assert.IsTrue(ReferenceEquals(varB, varB2));
            Assert.IsTrue(ReferenceEquals(varB, varB2));

            Assert.AreEqual(varA.Context, context);
            Assert.AreEqual(varB.Context, context);
            Assert.AreEqual(varC.Context, context);
            Assert.AreEqual(context["a"], varA);
            Assert.AreEqual(context["b"], varB);
            Assert.AreEqual(context["c"], varC);
            int valA = 1, valB = 2, valC = 3;
            context["a"].Contents = "" + valA;
            context["b"].Contents = "" + valB;
            context["c"].Contents = "" + valC;

            // Do a simple evaluation of an expression containing a variable.
            IEvaluateable exp = Expression.FromString("5a+3", context).Commit();
            varA.Update(out ISet<Variable> changed);
            Assert.AreEqual(0, changed.Count);
            Assert.AreEqual(exp.Evaluate(), (5 * valA + 3));
            Assert.AreEqual(exp.Evaluate(), 8);
            Assert.IsTrue(exp is Clause clause);



            // Do a more-complex evaluation of an expression containing multiple variables.
            exp = Expression.FromString("4a + 2b*(3c+4)", context).Commit();
            varB.Update(out changed);
            Assert.AreEqual(0, changed.Count);
            varC.Update(out changed);
            Assert.AreEqual(0, changed.Count);
            Assert.AreEqual(exp.Evaluate(), (4 * valA) + ((2 * valB) * (3 * valC + 4)));
            Assert.AreEqual(exp.Evaluate(), 56);


            // Change a variable's stored value, and test.
            valA = 5;
            varA.Contents = "" + new Number(valA);
            Assert.AreEqual(exp.Evaluate(), 4 * valA + 2 * valB * (3 * valC + 4));
            Assert.AreEqual(exp.Evaluate(), 72);


            // Change a variable's contents to another expression.
            varB.Contents = "4+(2*3)";
            Assert.AreEqual(varB.Evaluate(), valB = 10);
            Assert.AreEqual(exp.Evaluate(), 4 * valA + 2 * valB * (3 * valC + 4));
            Assert.AreEqual(exp.Evaluate(), 280);



            // Change a variable's contents to an expression based on another variable.
            varB.Contents = "4a-7";
            Assert.AreEqual(exp.Evaluate(), 4 * valA + 2 * (valB = 4 * valA - 7) * (3 * valC + 4));
            Assert.AreEqual(exp.Evaluate(), 358);
            Assert.AreEqual(varB.Evaluate(), valB);


            // Now, change the value of the 'a' and see if the change propogates up through the two expressions.
            varA.Contents = "-1";
            valA = -1;
            valB = 4 * valA - 7;
            int compare = 4 * valA + 2 * valB * (3 * valC + 4);
            // If 'b' equals 334, the value assigned to 'a' did not propogate.
            Assert.AreEqual(exp.Evaluate(), compare);
            Assert.AreEqual(exp.Evaluate(), -290);

            // Now, create a circular dependency and test for an exception.
            try
            {
                varA.Contents = "2b-14";
                Assert.Fail();
            }
            catch (CircularDependencyException cdex)
            {
                // Make sure state wasn't actually changed.
                Assert.AreEqual(exp.Evaluate(), compare);
                Assert.AreEqual(exp.Evaluate(), -290);
                Assert.AreEqual(cdex.Tested, varA);
                // Assert.AreEqual(cdex.Dependee, b);
            }

            // Test for exceptions from self-referencing circularity.
            Variable varD = (Variable)Expression.FromString("d", context).Commit();            
            Assert.AreEqual(varD.Evaluate(), Variable.Null);
            try
            {
                varD.Contents = "d";
                Assert.Fail();
            }
            catch (CircularDependencyException cdex)
            {
                Assert.AreEqual(varD.Evaluate(), Variable.Null);
                Assert.AreEqual(cdex.Tested, varD);
                //Assert.AreEqual(cdex.Dependee, d);
            }

        }

        [TestMethod]
        public void TestParsing_Variables_Updating()
        {
            // NOTE:  THESE TESTS ARE DESIGNED TO FAIL IN THE EVENT OF BAD CONCURRENCY (RACE CONDITIONS OR DEADLOCKS) IN THE VARIABLE 
            // UPDATING ALGORITHM.  They will also validate that the updating system is capable of doing 50-500 updates per millisecond, 
            // thousands per second. Modify millisPerTest variable to change how long each test should run.

            int millisPerTest = 1000;  // 1000 ms = 1 second per test.

            // Results for 10 minutes each (9/29/18):
            // Line test performed 99160000 updates in 600034 ms, or 165.257302086215 per ms
            // Ladder test performed 116012792 updates in 600000 ms, or 193.354653333333 per ms
            // Spiral test performed 25777620 updates in 600033 ms, or 42.960337181455 per ms
            // Pancake test performed 601510000 updates in 600006 ms, or 1002.50664160025 per ms
            // Diamond test performed 421369780 updates in 600001 ms, or 702.281796197006 per ms

            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            stopwatch.Stop();
            stopwatch.Reset();
            DummyContext ctxt;           

            // The line test.  This tests variable value propogation through a simple line.
            {
                int vars = 10000;
                ctxt = new DummyContext(null, "dummy_context");
                string name = "line" + 0.ToString("D5");
                Expression.FromString(name, ctxt);
                Variable startLine = ctxt[name];                
                startLine.Contents = "-1";
                Variable endLine = null;
                for (int i = 1; i <= vars; i++)
                {
                    string newName = "line" + i.ToString("D5");
                    Expression.FromString(newName, ctxt);
                    endLine = ctxt[newName];                    
                    endLine.Contents = name;
                    name = newName;
                }
                Assert.AreEqual(endLine.Value, -1);
                startLine.Contents = "2";
                Assert.AreEqual(endLine.Value, 2);
                long totalEdges = 0;
                stopwatch.Restart();                
                while (stopwatch.ElapsedMilliseconds < millisPerTest)
                {
                    startLine.Contents = "1";
                    startLine.Contents = "2";
                    totalEdges += (vars + vars);
                }                
                stopwatch.Stop();
                Console.WriteLine("Line test performed " + totalEdges + " updates in " + stopwatch.ElapsedMilliseconds + " ms, or " + ((double)totalEdges / stopwatch.ElapsedMilliseconds) + " per ms");
            }

            
            // The ladder test.  This tests variable operational updating at depth.
            {
                int ladderVars = 50;
                ctxt = new DummyContext(null, "dummy_context");
                string newNameA = "ladderA" + 0.ToString("D5");
                string newNameB = "ladderB" + 0.ToString("D5");
                Variable startA = (Variable)Expression.FromString(newNameA, ctxt).Commit();
                Variable startB = (Variable)Expression.FromString(newNameB, ctxt).Commit();
                startA.Contents = "0";
                startB.Contents = "0";
                Assert.AreEqual(startA.Evaluate(), 0);
                Assert.AreEqual(startB.Evaluate(), 0);
                Variable endA = null, endB = null;
                int edges = 0;
                for (int i = 1; i < ladderVars; i++)
                {
                    string oldNameA = newNameA, oldNameB = newNameB;
                    newNameA = "ladderA" + i.ToString("D5");
                    newNameB = "ladderB" + i.ToString("D5");
                    endA = (Variable)Expression.FromString(newNameA, ctxt).Commit();
                    endB = (Variable)Expression.FromString(newNameB, ctxt).Commit();
                    endA.Contents = oldNameA + " + " + oldNameB;
                    endB.Contents = oldNameB + " + " + oldNameA;
                    edges += 4;
                }

                Assert.AreEqual(endA.Evaluate(), 0);
                Assert.AreEqual(endB.Evaluate(), 0);                
                startA.Contents = "1";                
                Assert.AreEqual(endA.Evaluate(), Math.Pow(2, (ladderVars - 2)));
                Assert.AreEqual(endB.Evaluate(), Math.Pow(2, (ladderVars - 2)));                
                startB.Contents = "1";
                Assert.AreEqual(endA.Evaluate(), Math.Pow(2, (ladderVars - 1)));
                Assert.AreEqual(endB.Evaluate(), Math.Pow(2, (ladderVars - 1)));

                stopwatch.Restart();
                long totalEdges = 0;
                while (stopwatch.ElapsedMilliseconds < millisPerTest)
                {
                    startA.Contents = "0";
                    startB.Contents = "0";
                    startA.Contents = "1";
                    startB.Contents = "1";
                    totalEdges += (edges + edges);
                }
                Console.WriteLine("Ladder test performed " + totalEdges + " updates in " + stopwatch.ElapsedMilliseconds + " ms, or " + ((double)totalEdges / stopwatch.ElapsedMilliseconds) + " per ms");
            }

            
            // The spiral conch shell test.  This tests a balance of deep vs wide dependency structure.
            {
                int spiralVars = 30;
                ctxt = new DummyContext(null, "dummy_context");
                Variable varCore = (Variable)Expression.FromString("core", ctxt).Commit();                
                IList<Variable> vars = new List<Variable>() { varCore };
                varCore.Contents = "0";
                int edges = 0;
                for (int i = 0; i < spiralVars; i++)
                {
                    string varName = "spiral" + i.ToString("D5");
                    Variable newVar = (Variable)Expression.FromString(varName, ctxt).Commit();                    
                    string varContents = string.Join(" + ", vars.Select(v => v.Name));
                    newVar.Contents = varContents;
                    vars.Add(newVar);
                    edges += vars.Count;
                }
                for (int i = 1; i < vars.Count; i++)
                {
                    // Name, inbound, listening structure, and unchanged value checked here.
                    Variable var = vars[i];
                    int inbound = (int)var.GetType().GetField("Inbound", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(var);
                    Assert.AreEqual(inbound, 0);
                    Assert.AreEqual(var.Name, "spiral" + (i - 1).ToString("D5"));
                    for (int j = 0; j < i; j++)
                        Assert.IsTrue(var.ListensTo(vars[j]));
                    Assert.AreEqual(var.Evaluate(), 0);
                }
                stopwatch.Restart();
                varCore.Contents = "1";                
                for (int i = 1; i < vars.Count; i++)
                {
                    Variable var = vars[i];
                    int shouldEqual = 1 << (i - 1);
                    Assert.AreEqual(var.Evaluate(), shouldEqual);
                }

                stopwatch.Restart();
                long totalEdges = 0;
                while (stopwatch.ElapsedMilliseconds < millisPerTest)
                {
                    varCore.Contents = "0";
                    varCore.Contents = "1";
                    totalEdges += (edges + edges);
                }
                stopwatch.Stop();                
                Console.WriteLine("Spiral test performed " + totalEdges + " updates in " + stopwatch.ElapsedMilliseconds + " ms, or " + ((double)totalEdges / stopwatch.ElapsedMilliseconds) + " per ms");
            }


            // The pancake test immediately goes wide before updating a single variable again.
            {
                int pancakeVars = 2500;
                ctxt = new DummyContext(null, "dummy_context");
                Variable pancakeStart = (Variable)Expression.FromString("pancakeStart", ctxt).Commit();                
                pancakeStart.Contents = 1.ToString();
                List<string> flatNames = new List<string>();
                for (int i = 0; i < pancakeVars; i++)
                {
                    Variable newVar = (Variable)Expression.FromString("pancake" + i.ToString("D5"), ctxt).Commit();                    
                    flatNames.Add(newVar.Name);
                    newVar.Contents = "pancakeStart";
                    Assert.AreEqual(newVar.Evaluate(), 1);
                }
                Variable pancakeEnd = (Variable)Expression.FromString("pancakeEnd", ctxt).Commit();                
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < flatNames.Count - 1; i++)
                {
                    sb.Append(flatNames[i]);
                    sb.Append(" + ");
                }
                sb.Append(flatNames[flatNames.Count - 1]);
                pancakeEnd.Contents = sb.ToString();
                Assert.AreEqual(pancakeEnd.Evaluate(), 1 * pancakeVars);
                pancakeStart.Contents = 2.ToString();
                Assert.AreEqual(pancakeStart.Evaluate(), 2);
                Assert.AreEqual(pancakeEnd.Evaluate(), 2 * pancakeVars);
                int edges = pancakeVars * 2;
                stopwatch.Restart();
                long totalEdges = 0;
                while (stopwatch.ElapsedMilliseconds < millisPerTest)
                {
                    pancakeStart.Contents = "1";
                    pancakeStart.Contents = "2";
                    totalEdges += (edges + edges);
                }
                stopwatch.Stop();                
                Console.WriteLine("Pancake test performed " + totalEdges + " updates in " + stopwatch.ElapsedMilliseconds + " ms, or " + ((double)totalEdges / stopwatch.ElapsedMilliseconds) + " per ms");
            }


            // The diamond test.  Goes exponentially wide before going exponentially narrow again.
            {
                int diamondWidth = 1024;               // Must be a multiple of 2 
                ctxt = new DummyContext(null, "dummy_context");
                
                List<Variable> middle = new List<Variable>();
                int i;
                long edges = 0;
                for (i = 0; i < diamondWidth; i++)
                {
                    Variable v = (Variable)Expression.FromString("diamond" + i.ToString("D8")).Commit();
                    middle.Add(v);
                }
                LinkedList<Variable> diamond = new LinkedList<Variable>(middle);
                while (diamond.Count > 1)  // The left side of the diamong.
                {
                    Variable vLeft = diamond.First();
                    diamond.RemoveFirst();
                    Variable vRight = diamond.First();
                    diamond.RemoveFirst();
                    Variable v = (Variable)Expression.FromString("diamond" + (i++).ToString("D8")).Commit();                    
                    vLeft.Contents = v.Name;
                    vRight.Contents = v.Name;
                    diamond.AddLast(v);
                    edges += 2;
                }
                Variable diamondStart = diamond.First();
                diamond = new LinkedList<Variable>(middle);
                while (diamond.Count > 1)  // The right side of the diamond.
                {
                    Variable vLeft = diamond.First();
                    diamond.RemoveFirst();
                    Variable vRight = diamond.First();
                    diamond.RemoveFirst();
                    Variable v = (Variable)Expression.FromString("diamond" + (i++).ToString("D8")).Commit();                    
                    v.Contents = vLeft.Name + " - " + vRight.Name;
                    diamond.AddLast(v);
                    //edges += 2;
                }
                edges += middle.Count;
                Variable diamondEnd = diamond.First();                
                Assert.AreEqual(diamondEnd.Evaluate(), 0);                
                diamondStart.Contents = "2";                
                Assert.AreEqual(diamondEnd.Evaluate(), 0);
                stopwatch.Restart();
                long totalEdges = 0;
                while (stopwatch.ElapsedMilliseconds < millisPerTest)
                {
                    diamondStart.Contents = "1";
                    diamondStart.Contents = "2";
                    totalEdges += (edges + edges);
                }                         
                Console.WriteLine("Diamond test performed " + totalEdges + " updates in " + stopwatch.ElapsedMilliseconds + " ms, or " + ((double)totalEdges / stopwatch.ElapsedMilliseconds) + " per ms");


            }
        }


        /// <summary>
        /// Used to test context feature.
        /// </summary>
        [Serializable]
        internal class DummyContext : Parsing.Context
        {


            public DummyContext(Context parent, string name) : base(parent, name, null) { }

            protected override bool TryCreateContext(string name, out Context sub_ctxt)
            {
                name = name.ToLower();
                if (name.StartsWith("dummy_context_"))
                {
                    sub_ctxt = new DummyContext(this, name);
                    return true;
                }
                sub_ctxt = null;
                return false;
            }

            protected override bool TryCreateVariable(string name, out Variable v)
            {
                name = name.ToLower();                
                v = new Variable(this, name);
                return true;
            }

            protected override bool TryCreateVariableContext(string name, out Variable v)
            {
                v = null;
                return false;
            }
        }
    }
}
