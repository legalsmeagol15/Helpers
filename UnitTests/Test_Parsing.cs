using System;
using Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using Parsing.Dependency;

namespace UnitTests
{
    [TestClass]
    public class Test_Parsing
    {
        
        [TestMethod]
        public void TestParsing_Dependency()
        {

        }


        #region Topologies

        public static Context2 CreateDiamondTopology(int width, out long edges, out Variable2 start, out Variable2 end)
        {
            Context2 ctxt = Context2.FromRoot();

            List<Variable2> middle = new List<Variable2>();
            int i;
            edges = 0;
            for (i = 0; i < width; i++)
            {
                Variable2 v = ctxt.Declare("diamond" + i.ToString("D8"));
                middle.Add(v);
            }
            LinkedList<Variable2> diamond = new LinkedList<Variable2>(middle);
            while (diamond.Count > 1)  // The left side of the diamong.
            {
                Variable2 vLeft = diamond.First();
                diamond.RemoveFirst();
                Variable2 vRight = diamond.First();
                diamond.RemoveFirst();
                string name = "diamond" + (i++).ToString("D8");
                Variable2 v = ctxt.Declare(name);
                vLeft.Contents = name;
                vRight.Contents = name;
                diamond.AddLast(v);
                edges += 2;
            }
            start = diamond.First();
            diamond = new LinkedList<Variable2>(middle);
            while (diamond.Count > 1)  // The right side of the diamond.
            {
                Variable2 vLeft = diamond.First();
                diamond.RemoveFirst();
                Variable2 vRight = diamond.First();
                diamond.RemoveFirst();
                string name = "diamond" + (i++).ToString("D8");
                Variable2 v = ctxt.Declare(name);
                v.Contents = name + " - " + name;
                diamond.AddLast(v);
                //edges += 2;
            }

            edges += middle.Count;
            end = diamond.First();

            return ctxt;
        }

        public static Context2 CreateLadderTopology(int vars, out long edges, out Variable2 startA, out Variable2 startB, out Variable2 endA, out Variable2 endB)
        {
            Context2 ctxt = Context2.FromRoot();            
            string newNameA = "ladderA" + 0.ToString("D5");
            string newNameB = "ladderB" + 0.ToString("D5");
            startA = ctxt.Declare(newNameA);
            startB = ctxt.Declare(newNameB);
            startA.Contents = "0";
            startB.Contents = "0";
            Assert.AreEqual(startA.Evaluate(), 0);
            Assert.AreEqual(startB.Evaluate(), 0);
            endA = null;
            endB = null;
            edges = 0;
            for (int i = 1; i < vars; i++)
            {
                string oldNameA = newNameA, oldNameB = newNameB;
                newNameA = "ladderA" + i.ToString("D5");
                newNameB = "ladderB" + i.ToString("D5");
                endA = ctxt.Declare(newNameA);
                endB = ctxt.Declare(newNameB);
                endA.Contents = oldNameA + " + " + oldNameB;
                endB.Contents = oldNameB + " + " + oldNameA;
                edges += 4;
            }
            return ctxt;
        }

        
        public static Context2 CreateLineTopology(int vars, out long edges, out Variable2 start, out Variable2 end)
        {
            throw new NotImplementedException();
            //Context ctxt = Context.FromRoot();           
            //string name = "line" + 0.ToString("D5");            
            //start = ctxt.Declare(name);
            //start.Contents = "-1";
            //end = null;
            //edges = 0;
            //for (int i = 1; i <= vars; i++)
            //{
            //    edges++;
            //    string newName = "line" + i.ToString("D5");
            //    start =ctxt.Declare(newName);
            //    end = ctxt[newName];
            //    end.Contents = name;
            //    name = newName;
            //}
            //return ctxt;
        }

        public static Context2 CreatePancakeTopology(int pancakeVars, out long edges, out Variable2 pancakeStart, out Variable2 pancakeEnd)
        {
            Context2 ctxt = Context2.FromRoot();
            pancakeStart = ctxt.Declare("pancakeStart");
            edges = 0;
            List<string> flatNames = new List<string>();
            for (int i = 0; i < pancakeVars; i++)
            {
                string name = "pancake" + i.ToString("D5");
                Variable2 newVar = ctxt.Declare(name);
                flatNames.Add(name);
                newVar.Contents = "pancakeStart";
            }
            pancakeEnd = ctxt.Declare("pancakeEnd");
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < flatNames.Count - 1; i++)
            {
                sb.Append(flatNames[i]);
                sb.Append(" + ");
                edges += flatNames.Count;
            }
            sb.Append(flatNames[flatNames.Count - 1]);
            pancakeEnd.Contents = sb.ToString();

            return ctxt;
        }

        public static Context2 CreateSpiralTopology(int spiralVars, out long edges, out Variable2 varCore, out List<Variable2> vars)
        {
            throw new NotImplementedException();
            ////int spiralVars = 30;
            //Context ctxt = Context.FromRoot();            
            //varCore = ctxt.Declare("core");
            //vars = new List<Variable>() { varCore };
            //varCore.Contents = "0";
            //edges = 0;
            //for (int i = 0; i < spiralVars; i++)
            //{
            //    string varName = "spiral" + i.ToString("D5");
            //    Variable newVar = ctxt.Declare(varName);
            //    string varContents = string.Join(" + ", vars.Select(v => v.Name));
            //    newVar.Contents = varContents;
            //    vars.Add(newVar);
            //    edges += vars.Count;
            //}
            //return ctxt;
        }

        [TestMethod]
        public void TestParsing_Validate_Topologies()
        {
            // Diamond topology
            {
                Context2 ctxt = CreateDiamondTopology(1024, out long edges, out Variable2 start, out Variable2 end);
                Assert.AreEqual(end.Evaluate(), 0);
                start.Contents = "2";
                Assert.AreEqual(start.Evaluate(), 2);
                Assert.AreEqual(end.Evaluate(), 0);
            }

            // Ladder topology
            {
                int vars = 50;
                Context2 ctxt = CreateLadderTopology(vars, out long edges, out Variable2 startA, out Variable2 startB, out Variable2 endA, out Variable2 endB);
                Assert.AreEqual(endA.Evaluate(), 0);
                Assert.AreEqual(endB.Evaluate(), 0);
                startA.Contents = "1";
                Assert.AreEqual(endA.Evaluate(), Math.Pow(2, (vars - 2)));
                Assert.AreEqual(endB.Evaluate(), Math.Pow(2, (vars - 2)));
                startB.Contents = "1";
                Assert.AreEqual(endA.Evaluate(), Math.Pow(2, (vars - 1)));
                Assert.AreEqual(endB.Evaluate(), Math.Pow(2, (vars - 1)));
            }

            // Line topology
            {
                Context2 ctxt = CreateLineTopology(10000, out long edges, out Variable2 startLine, out Variable2 endLine);
                startLine.Contents = "-1";
                Assert.AreEqual(endLine.Value, -1);
                startLine.Contents = "2";
                Assert.AreEqual(endLine.Value, 2);
            }

            // Pancake topology
            {
                int vars = 2500;
                Context2 ctxt = CreatePancakeTopology(vars, out long edges, out Variable2 pancakeStart, out Variable2 pancakeEnd);
                pancakeStart.Contents = 1.ToString();
                Assert.AreEqual(pancakeEnd.Evaluate(), 1 * vars);
                pancakeStart.Contents = 2.ToString();
                Assert.AreEqual(pancakeStart.Evaluate(), 2);
                Assert.AreEqual(pancakeEnd.Evaluate(), 2 * vars);
            }

            // Spiral topology
            {
                Context2 ctxt = CreateSpiralTopology(30, out long edges, out Variable2 varCore, out List<Variable2> vars);
                varCore.Contents = "1";
                for (int i = 1; i < vars.Count; i++)
                {
                    Variable2 var = vars[i];
                    int shouldEqual = 1 << (i - 1);
                    Assert.AreEqual(var.Evaluate(), shouldEqual);
                }
                //int val = 1;
                //for (int i = 1; i < vars.Count; i++)
                //{
                //    // Name, inbound, listening structure, and unchanged value checked here.
                //    Variable var = vars[i];
                //    int inbound = (int)var.GetType().GetField("UnresolvedInbound", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(var);
                //    Assert.AreEqual(inbound, 0);
                //    Assert.AreEqual(var.Name, "spiral" + (i - 1).ToString("D5"));
                //    for (int j = 0; j < i; j++)
                //        Assert.IsTrue(var.ListensTo(vars[j]));
                //    Assert.IsTrue(var.ListensTo(varCore));
                //    Assert.AreEqual(var.Evaluate(), val << (i - 1), var.Name);

                //}
                throw new NotImplementedException();
            }
        }


        #endregion



        [TestMethod]
        public void TestParsing_Constant_Functions()
        {            
            Context2 context = Context2.FromRoot();

            Function.Factory factory = context.Functions;

            Function f1 = factory["PI"];
            Function f2 = factory["PI"];
            Assert.IsTrue(ReferenceEquals(f1, f2));
            Assert.AreEqual(factory["PI"].Evaluate(), (decimal)Math.PI);
            Assert.AreEqual(factory["E"].Evaluate(), (decimal)Math.E);

            Variable2 pi = context.Declare("pi_var", "PI");            
            Assert.AreEqual("PI", pi.Contents.ToString());
            Assert.AreEqual(pi.Evaluate(), (decimal)Math.PI);
            pi.SetContents("PI()");            
            Assert.AreEqual("PI", pi.Contents.ToString());
            Assert.AreEqual(pi.Evaluate(), (decimal)Math.PI);

            Variable2 e = context.Declare("e_var", "E");            
            Assert.AreEqual("E", e.Contents.ToString());
            Assert.AreEqual(e.Evaluate(), (decimal)Math.E);
            e = context.Declare("e_var", "E()");  // Try a redundant Declare() instead of a SetContents()            
            Assert.AreEqual("E", e.Contents.ToString());
            Assert.AreEqual(e.Evaluate(), (decimal)Math.E);

        }



        [TestMethod]
        public void TestParsing_Contexts()
        {
            Context2 dummyA = Context2.FromRoot();
            Variable2 varA = dummyA.Declare("a");
            Variable2 varB = dummyA.Declare("dummy_context_b.b");
            Context2 dummyB = dummyA.GetSubcontext("dummy_context_b");
            Variable2 varC = dummyB.Declare("dummy_context_c.c");
            Context2 dummyC = dummyB.GetSubcontext("dummy_context_c");
            
            varA.Contents = "10"; 
            varB.Contents = "5";
            varC.Contents = "2";

            Variable2 v = dummyA.GetVariable("dummy_context_b.dummy_context_c.c");
            Assert.IsTrue(ReferenceEquals(v, varC));
            Assert.AreEqual(v.Evaluate(), 2);

            Variable2 sum = dummyA.Declare("a + dummy_context_b.b + dummy_context_b.dummy_context_c.c");
            Assert.AreEqual(sum.Value, 17);

            sum = dummyC.Declare("c + b");
            Assert.AreEqual(sum.Value, 7, "If it equals 2, it means that variable getting isn't looking at super-contexts.");
            
        }



        [TestMethod]
        public void TestParsing_Function_Factory()
        {
            Function.Factory factory = Function.Factory.StandardFactory;

            Assert.IsTrue(factory.Contains("Addition"));
            Assert.IsTrue(factory.Contains("Subtraction"));
            Assert.IsTrue(factory.Contains("Multiplication"));
            Assert.IsTrue(factory.Contains("Division"));
            Assert.IsTrue(factory.Contains("Exponentiation"));
            Assert.IsTrue(factory.Contains("Negation"));
            Assert.IsTrue(factory.Contains("Subtraction"));
            //Assert.IsTrue(factory.Contains("Relation"));
            Assert.IsTrue(factory.Contains("Span"));
            Assert.IsTrue(factory.Contains("And"));
            Assert.IsTrue(factory.Contains("Or"));
            Assert.IsTrue(factory.Contains("PI"));
            Assert.IsTrue(factory.Contains("E"));
        }

        

        [TestMethod]
        public void TestParsing_Nesting()
        {
            IEvaluateable e = Expression.FromString("2+1", null);
            
            Assert.AreEqual("2 + 1", e.ToString());
            Assert.AreEqual(e.Evaluate(), 3);

            e = Expression.FromString("3+(2+1)", null);
            Assert.AreEqual("3 + ( 2 + 1 )", e.ToString());
            Assert.AreEqual(e.Evaluate(), 6);

            e = Expression.FromString("(3+2)+1", null);
            Assert.AreEqual("( 3 + 2 ) + 1", e.ToString());
            Assert.AreEqual(e.Evaluate(), 6);

            e = Expression.FromString("(4+3)", null);
            Assert.AreEqual("( 4 + 3 )", e.ToString());
            Assert.AreEqual(e.Evaluate(), 7);

            e = Expression.FromString("((4+3))", null);
            Assert.AreEqual("( ( 4 + 3 ) )", e.ToString());
            Assert.AreEqual(e.Evaluate(), 7);

            e = Expression.FromString("((3+2))+1", null);
            Assert.AreEqual("( ( 3 + 2 ) ) + 1", e.ToString());
            Assert.AreEqual(e.Evaluate(), 6);

            e = Expression.FromString("3+((2+1))", null);
            Assert.AreEqual("3 + ( ( 2 + 1 ) )", e.ToString());
            Assert.AreEqual(e.Evaluate(), 6);
        }


        
        [TestMethod]
        public void TestParsing_Number_Equalities()
        {
            Function.Factory factory = Function.Factory.StandardFactory;

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
            IEvaluateable e = Expression.FromString("5+4", null);
            Assert.AreEqual(e.Evaluate(), 9m);
            e = Expression.FromString("5+-4", null);
            Assert.AreEqual(e.Evaluate(), 1);
            e = Expression.FromString("-5+4", null);
            Assert.AreEqual(e.Evaluate(), -1);
            e = Expression.FromString("-5+-4", null);
            Assert.AreEqual(e.Evaluate(), -9);

            // Subtraction
            e = Expression.FromString("5-4", null);
            Assert.AreEqual(e.Evaluate(), 1);
            e = Expression.FromString("5--4", null);
            Assert.AreEqual(e.Evaluate(), 9);
            e = Expression.FromString("-5-4", null);
            Assert.AreEqual(e.Evaluate(), -9);
            e = Expression.FromString("-5--4", null);
            Assert.AreEqual(e.Evaluate(), -1);

            // Multiplication
            e = Expression.FromString("5*4", null);
            Assert.AreEqual(e.Evaluate(), 20);
            e = Expression.FromString("5*-4", null);
            Assert.AreEqual(e.Evaluate(), -20);
            e = Expression.FromString("-5*4", null);
            Assert.AreEqual(e.Evaluate(), -20);
            e = Expression.FromString("-5*-4", null);
            Assert.AreEqual(e.Evaluate(), 20);

            // Division
            e = Expression.FromString("5/4", null);
            Assert.AreEqual(e.Evaluate(), 1.25);
            e = Expression.FromString("5/-4", null);
            Assert.AreEqual(e.Evaluate(), -1.25);
            e = Expression.FromString("-5/4", null);
            Assert.AreEqual(e.Evaluate(), -1.25);
            e = Expression.FromString("-5/-4", null);
            Assert.AreEqual(e.Evaluate(), 1.25);

            // Exponentiation
            e = Expression.FromString("2^4", null);
            Assert.AreEqual(e.Evaluate(), 16);
        }



        [TestMethod]
        public void TestParsing_Regex()
        {
            /// private const string StringPattern = "(?<stringPattern>\".*\")";
            /// private const string OpenerPattern = @"(?<openerPattern>[\(\[{])";
            /// private const string CloserPattern = @"(?<closerPattern>[\)\]}])";
            /// private const string OperPattern = @"(?<operPattern>[+-/*&|^~!])";
            /// private const string VarPattern = @"(?<varPattern> \$? [a-zA-Z_][\w_]* (?:\.[a-zA-Z_][\w_]*)*)";
            /// private const string NumPattern = @"(?<numPattern>(?:-)? (?: \d+\.\d* | \d*\.\d+ | \d+ ))";
            /// private const string SpacePattern = @"(?<spacePattern>\s+)";
            /// private static Regex _Regex = new Regex(regExPattern, RegexOptions.IgnorePatternWhitespace);
            /// private static string regExPattern = string.Format("({0}) | ({1}) | ({2}) | ({3}) | ({4}) | ({5}) | ({6})",
            /// StringPattern,        //0
            /// OpenerPattern,        //1
            /// CloserPattern,        //2
            /// OperPattern,          //3
            /// VarPattern,           //4
            /// NumPattern,           //5
            /// SpacePattern);        //6

            string StringPattern = (string)typeof(Expression).GetField("StringPattern", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetRawConstantValue();
            string OpenerPattern = (string)typeof(Expression).GetField("OpenerPattern", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetRawConstantValue();
            string CloserPattern = (string)typeof(Expression).GetField("CloserPattern", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetRawConstantValue();
            string OperPattern = (string)typeof(Expression).GetField("OperPattern", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetRawConstantValue();
            string VarPattern = (string)typeof(Expression).GetField("VarPattern", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetRawConstantValue();
            string NumPattern = (string)typeof(Expression).GetField("NumPattern", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetRawConstantValue();
            string SpacePattern = (string)typeof(Expression).GetField("SpacePattern", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetRawConstantValue();

            string overallPattern = (string)typeof(Expression).GetField("regExPattern", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetValue(null);

            // When the split length is 1 and the only part of the split equals the original string, it means that part of the RegEx doesn't apply

            Regex regex;
            regex = new Regex(StringPattern, RegexOptions.IgnorePatternWhitespace);
            Assert.IsFalse(Applies(regex, "a"));

            regex = new Regex(OpenerPattern, RegexOptions.IgnorePatternWhitespace);
            Assert.IsFalse(Applies(regex, "a"));

            regex = new Regex(CloserPattern, RegexOptions.IgnorePatternWhitespace);
            Assert.IsFalse(Applies(regex, "a"));

            regex = new Regex(OperPattern, RegexOptions.IgnorePatternWhitespace);
            Assert.IsFalse(Applies(regex, "a"));

            regex = new Regex(VarPattern, RegexOptions.IgnorePatternWhitespace);
            Assert.IsTrue(Applies(regex, "a"));

            regex = new Regex(SpacePattern, RegexOptions.IgnorePatternWhitespace);
            Assert.IsFalse(Applies(regex, "a"));

            regex = new Regex(overallPattern, RegexOptions.IgnorePatternWhitespace);
            Console.WriteLine(overallPattern);
            Assert.IsTrue(Matches(regex, "(5a+3)", "(", "5", "a", "+", "3", ")"));



            bool Applies(Regex pattern, string str)
            {
                string[] split = pattern.Split(str);
                if (split.Length == 1)
                {
                    if (!split[0].Equals(str)) Assert.Fail("Failed to reject string \"" + str + "\"");
                    return false;
                }
                if (split.Length != 3) Assert.Fail("Incorrect split length (" + split.Length + ")");
                if (split[0] != "") Assert.Fail("First split from " + str + " was not \"\"");
                if (split[1] != str) Assert.Fail("First split was \"" + split[1] + "\", which was not expected \"" + str + "\"");
                if (split[2] != "") Assert.Fail("First split from " + str + " was not \"\"");
                return true;
            }
            bool Matches(Regex pattern, string str, params string[] expected)
            {
                string[] split = pattern.Split(str);
                if (split.Length != (expected.Length * 2) + 1)
                    Assert.Fail("Split length (" + split.Length + ") was different from expected length (" + ((expected.Length * 2) + 1) + ").");

                for (int i = 0; i < (split.Length) - 1; i += 2)
                {
                    if (split[i] != "") Assert.Fail("Split #" + i + " from \"" + str + "\" was not \"\"");
                    if (split[i + 1] != expected[i / 2])
                        Assert.Fail("Split #" + (i + 1) + " was \"" + split[i + 1] + "\", expected was \"" + expected[i / 2] + "\"");
                }
                if (split[split.Length - 1] != "")
                    Assert.Fail("Split #" + (split.Length - 1) + " (last split) from \"" + str + "\" was \"" + split[split.Length - 1] + "\", not \"\"");
                return true;
            }

        }



        [TestMethod]
        public void TestParsing_Serialization()
        {
            Context2 context = Context2.FromRoot();
            Variable2 aOrigin = context.Declare( "a");
            aOrigin.Contents = "2";
            IEvaluateable exp1 = Expression.FromString("3 + 5 * a ^ 2 / 4 - -1", context);


            MemoryStream outStream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(outStream, context);


            outStream.Seek(0, SeekOrigin.Begin);
            formatter = new BinaryFormatter();
            Context2 deser = (Context2)formatter.Deserialize(outStream);

            Variable2 aPrime = deser.GetVariable("a");            
            Assert.AreEqual(aPrime.Evaluate(), 2);
        }



        [TestMethod]
        public void TestParsing_Variables()
        {
            Context2 context = Context2.FromRoot();
            Variable2 varA = context.Declare("a");
            Variable2 varB = context.Declare("b");
            Variable2 varC = context.Declare("c");
            Variable2 varA2 = context.GetVariable("a");
            Variable2 varB2 = context.GetVariable("b");
            Variable2 varC2 = context.GetVariable("c");
            
            Assert.IsTrue(ReferenceEquals(varA, varA2));
            Assert.IsTrue(ReferenceEquals(varB, varB2));
            Assert.IsTrue(ReferenceEquals(varB, varB2));
            
            Assert.AreEqual((Context2)typeof(Variable2).GetField("Parent").GetValue(varA), context);
            Assert.AreEqual((Context2)typeof(Variable2).GetField("Parent").GetValue(varB), context);
            Assert.AreEqual((Context2)typeof(Variable2).GetField("Parent").GetValue(varC), context);
            Assert.AreEqual(context.GetVariable("a"), varA);
            Assert.AreEqual(context.GetVariable("b"), varB);
            Assert.AreEqual(context.GetVariable("c"), varC);
            int valA = 1, valB = 2, valC = 3;
            context.GetVariable("a").Contents = "" + valA;
            context.GetVariable("b").Contents = "" + valB;
            context.GetVariable("c").Contents = "" + valC;

            // Do a simple evaluation of an expression containing a variable.
            IEvaluateable exp = Expression.FromString("5a+3", context);
            varA.UpdateValue(out IDictionary<Variable2, DataStructures.ChangedEventArgs<Variable2, IEvaluateable>> changed);
            Assert.AreEqual(0, changed.Count);
            Assert.AreEqual(exp.Evaluate(), (5 * valA + 3));
            Assert.AreEqual(exp.Evaluate(), 8);
            Assert.IsTrue(exp is Clause clause);



            // Do a more-complex evaluation of an expression containing multiple variables.
            exp = Expression.FromString("4a + 2b*(3c+4)", context);
            varB.UpdateValue(out changed);
            Assert.AreEqual(0, changed.Count);
            varC.UpdateValue(out changed);
            Assert.AreEqual(0, changed.Count);
            Assert.AreEqual(exp.Evaluate(), (4 * valA) + ((2 * valB) * (3 * valC + 4)));
            Assert.AreEqual(exp.Evaluate(), 56);


            // Change a variable's stored value, and test.
            valA = 5;
            varA.Contents = "" + valA;
            Assert.AreEqual(exp.Evaluate(), 4 * valA + 2 * valB * (3 * valC + 4));
            Assert.AreEqual(exp.Evaluate(), 72);


            // Change a variable's contents to another expression.
            varB.Contents = "4+(2*3)";
            Assert.AreEqual(varB.Evaluate(), valB = 10);
            Assert.AreEqual(exp.Evaluate(), 4 * valA + 2 * valB * (3 * valC + 4));
            Assert.AreEqual(exp.Evaluate(), 280);



            // Change a variable's contents to an expression based on another variable.
            varB.SetContents("4a-7");
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
                Assert.AreEqual(cdex.ExistingSource, varA);
                Assert.AreEqual(cdex.ExistingListener, varB);                
            }

            // Test for exceptions from self-referencing circularity.
            Variable2 varD = context.Declare("d");
            Assert.AreEqual(varD.Evaluate(), Variable2.Null);
            try
            {
                varD.Contents = "d";
                Assert.Fail();
            }
            catch (CircularDependencyException cdex)
            {
                Assert.AreEqual(varD.Evaluate(), Variable2.Null);
                Assert.AreEqual(cdex.ExistingListener, varD);
                //Assert.AreEqual(cdex.Dependee, d);
            }

        }



        [TestMethod]
        public void TestParsing_Variables_Deleting()
        {

            // TEST THE BASIC CONTEXT
            // Variables which are not pointed to should NOT be deleted from this context.
            {
                // Create a variable that is independent of everything else.
                Context2 root = Context2.FromRoot();                
                Variable2 varA = root.Declare("a", "5+3");
                Assert.AreEqual(varA.Value, 8);
                Variable2 nonexistingEmpty = null;
                try
                {
                    nonexistingEmpty = root.Declare("empty");
                    Assert.Fail();
                }
                catch (KeyNotFoundException) { }
                Assert.IsNull(nonexistingEmpty);
                

                // Create the variable which will eventually be deleted, and have 'a' point at it.
                Variable2 varTBD = root.Declare("subcontext.tbd");
                Context2 subctxt = root.GetSubcontext("subcontext");                
                varA.Contents = "subcontext.tbd + 4";
                Assert.AreEqual(varA.Value, 4);
                Variable2 varTBDPrime = subctxt.GetVariable("tbd");
                Assert.AreEqual(varTBD, varTBDPrime);
                varTBDPrime = null;                

                // Create a weak reference to varTBD and to subctxt and assure that they work just as well as the standard strong 
                // reference.
                WeakReference<Variable2> weakVar = new WeakReference<Variable2>(varTBD);
                Assert.IsTrue(weakVar.TryGetTarget(out Variable2 weakVarTemp));
                Assert.AreEqual(weakVarTemp, varTBD);
                weakVarTemp = null;
                WeakReference<Context2> weakCtxt = new WeakReference<Context2>(subctxt);
                Assert.IsTrue(weakCtxt.TryGetTarget(out Context2 weakCtxtTemp));
                Assert.AreEqual(weakCtxtTemp, subctxt);
                weakCtxtTemp = null;

                // Get 'a' to point at something else.  Since nothing is now listening to 'tbd', it should automatically 
                // delete itself.  Since there's nothing left in subctxt, it should also automatically delete itself.
                varA.Contents = "9-2";
                Assert.AreEqual(varA.Value, 7);
                GC.Collect();
                subctxt.GetVariable("tbd");
                root.GetSubcontext("subcontext");                
            }


            // TEST THE SOFT CONTEXT
            // Variables which are not pointed to SHOULD be deleted from this context.
            {
                Context2 root = Context2.FromRoot();                
                Variable2 tbd = root.Declare("tbd", "0");
                root.GetVariable("tbd");                
                tbd = null;
                GC.Collect();
                try
                {
                    // Since there are no other pointers to the weak variable, it should go bye-bye with the GC
                    root.GetVariable("tbd");
                    Assert.Fail();
                }
                catch (Exception ex)
                {

                }                
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
            // Line test performed 99,160,000 updates in 600034 ms, or 165.257302086215 per ms
            // Ladder test performed 116,012,792 updates in 600000 ms, or 193.354653333333 per ms
            // Spiral test performed 25,777,620 updates in 600033 ms, or 42.960337181455 per ms
            // Pancake test performed 601,510,000 updates in 600006 ms, or 1002.50664160025 per ms
            // Diamond test performed 421,369,780 updates in 600001 ms, or 702.281796197006 per ms

            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            stopwatch.Stop();
            stopwatch.Reset();
            Context2 ctxt;

            // The diamond test.  Goes exponentially wide before going exponentially narrow again.
            {
                ctxt = CreateDiamondTopology(1024, out long edges, out Variable2 diamondStart, out Variable2 diamondEnd);

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

            // The ladder test.  This tests variable operational updating at depth.
            {
                ctxt = CreateLadderTopology(50, out long edges, out Variable2 startA, out Variable2 startB, out Variable2 endA, out Variable2 endB);

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
                stopwatch.Stop();
                Console.WriteLine("Ladder test performed " + totalEdges + " updates in " + stopwatch.ElapsedMilliseconds + " ms, or " + ((double)totalEdges / stopwatch.ElapsedMilliseconds) + " per ms");
            }

            // The line test.  This tests variable value propogation through a simple line.
            {
                ctxt = CreateLineTopology(10000, out long edges, out Variable2 startLine, out Variable2 endLine);

                long totalEdges = 0;
                stopwatch.Restart();
                while (stopwatch.ElapsedMilliseconds < millisPerTest)
                {
                    startLine.Contents = "1";
                    startLine.Contents = "2";
                    totalEdges += (edges + edges);
                }
                stopwatch.Stop();
                Console.WriteLine("Line test performed " + totalEdges + " updates in " + stopwatch.ElapsedMilliseconds + " ms, or " + ((double)totalEdges / stopwatch.ElapsedMilliseconds) + " per ms");
            }

            // The pancake test immediately goes wide before updating a single variable again.
            {
                int vars = 2500;
                CreatePancakeTopology(vars, out long edges, out Variable2 pancakeStart, out Variable2 pancakeEnd);

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

            // The spiral conch shell test.  This tests a balance of deep vs wide dependency structure.
            {
                int variables = 30;
                ctxt = CreateSpiralTopology(variables, out long edges, out Variable2 varCore, out List<Variable2> vars);

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
        }


        [DependencyClass(SubcontextPattern ="context.*", VariablePattern = "var.*")]
        public class DummyDrawing
        {

        }
        public class DummyLine
        {
            [DependencyProperty(Contents = "2", IsWeak = false)]
            public double X;

            [DependencyProperty(Contents = "3", IsWeak = false)]
            public double Y;

            [DependencyProperty(Contents = "R, G, B", IsWeak = false)]
            [DependencyContext]
            DummyColor Color;
        }
        public class DummyColor
        {
            [DependencyProperty(Contents = "1", IsWeak = true)]
            public double R;

            [DependencyProperty(Contents = "0.5", IsWeak = true)]
            public double G;

            [DependencyProperty(Contents = "0.25", IsWeak = true)]
            public double B;
        }



   
    }
}
