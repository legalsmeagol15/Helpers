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
using Parsing.Contexts;

namespace UnitTests
{
    [TestClass]
    public class Test_Parsing
    {
        
        #region Topologies

        public static IContext CreateDiamondTopology(int width, out long edges, out Variable start, out Variable end)
        {
            IContext ctxt = new DummyContext(null, "dummy_context");

            List<Variable> middle = new List<Variable>();
            int i;
            edges = 0;
            for (i = 0; i < width; i++)
            {
                Variable v = Variable.Declare(ctxt, "diamond" + i.ToString("D8"));
                middle.Add(v);
            }
            LinkedList<Variable> diamond = new LinkedList<Variable>(middle);
            while (diamond.Count > 1)  // The left side of the diamong.
            {
                Variable vLeft = diamond.First();
                diamond.RemoveFirst();
                Variable vRight = diamond.First();
                diamond.RemoveFirst();
                Variable v = Variable.Declare(ctxt, "diamond" + (i++).ToString("D8"));
                vLeft.Contents = v.Name;
                vRight.Contents = v.Name;
                diamond.AddLast(v);
                edges += 2;
            }
            start = diamond.First();
            diamond = new LinkedList<Variable>(middle);
            while (diamond.Count > 1)  // The right side of the diamond.
            {
                Variable vLeft = diamond.First();
                diamond.RemoveFirst();
                Variable vRight = diamond.First();
                diamond.RemoveFirst();
                Variable v = Variable.Declare( ctxt, "diamond" + (i++).ToString("D8"));
                v.Contents = vLeft.Name + " - " + vRight.Name;
                diamond.AddLast(v);
                //edges += 2;
            }

            edges += middle.Count;
            end = diamond.First();

            return ctxt;
        }

        public static IContext CreateLadderTopology(int vars, out long edges, out Variable startA, out Variable startB, out Variable endA, out Variable endB)
        {
            DummyContext ctxt = new DummyContext(null, "dummy_context");
            string newNameA = "ladderA" + 0.ToString("D5");
            string newNameB = "ladderB" + 0.ToString("D5");
            startA = Variable.Declare(ctxt,newNameA);
            startB = Variable.Declare(ctxt, newNameB);
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
                endA = Variable.Declare(ctxt, newNameA);
                endB = Variable.Declare(ctxt, newNameB);
                endA.Contents = oldNameA + " + " + oldNameB;
                endB.Contents = oldNameB + " + " + oldNameA;
                edges += 4;
            }
            return ctxt;
        }

        public static IContext CreateLineTopology(int vars, out long edges, out Variable start, out Variable end)
        {
            DummyContext ctxt = new DummyContext(null, "dummy_context");
            string name = "line" + 0.ToString("D5");
            Expression.FromString(name, ctxt);
            start = ctxt[name];
            start.Contents = "-1";
            end = null;
            edges = 0;
            for (int i = 1; i <= vars; i++)
            {
                edges++;
                string newName = "line" + i.ToString("D5");
                Expression.FromString(newName, ctxt);
                end = ctxt[newName];
                end.Contents = name;
                name = newName;
            }
            return ctxt;
        }

        public static IContext CreatePancakeTopology(int pancakeVars, out long edges, out Variable pancakeStart, out Variable pancakeEnd)
        {
            IContext ctxt = new DummyContext(null, "dummy_context");
            pancakeStart = Variable.Declare(ctxt, "pancakeStart");
            edges = 0;
            List<string> flatNames = new List<string>();
            for (int i = 0; i < pancakeVars; i++)
            {
                Variable newVar = Variable.Declare(ctxt, "pancake" + i.ToString("D5"));
                flatNames.Add(newVar.Name);
                newVar.Contents = "pancakeStart";
            }
            pancakeEnd = Variable.Declare(ctxt, "pancakeEnd");
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

        public static IContext CreateSpiralTopology(int spiralVars, out long edges, out Variable varCore, out List<Variable> vars)
        {
            //int spiralVars = 30;
            DummyContext ctxt = new DummyContext(null, "dummy_context");
            varCore = Variable.Declare(ctxt, "core");
            vars = new List<Variable>() { varCore };
            varCore.Contents = "0";
            edges = 0;
            for (int i = 0; i < spiralVars; i++)
            {
                string varName = "spiral" + i.ToString("D5");
                Variable newVar = Variable.Declare(ctxt, varName);
                string varContents = string.Join(" + ", vars.Select(v => v.Name));
                newVar.Contents = varContents;
                vars.Add(newVar);
                edges += vars.Count;
            }
            return ctxt;
        }

        [TestMethod]
        public void TestParsing_Validate_Topologies()
        {
            // Diamond topology
            {
                IContext ctxt = CreateDiamondTopology(1024, out long edges, out Variable start, out Variable end);
                Assert.AreEqual(end.Evaluate(), 0);
                start.Contents = "2";
                Assert.AreEqual(start.Evaluate(), 2);
                Assert.AreEqual(end.Evaluate(), 0);
            }

            // Ladder topology
            {
                int vars = 50;
                IContext ctxt = CreateLadderTopology(vars, out long edges, out Variable startA, out Variable startB, out Variable endA, out Variable endB);
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
                IContext ctxt = CreateLineTopology(10000, out long edges, out Variable startLine, out Variable endLine);
                startLine.Contents = "-1";
                Assert.AreEqual(endLine.Value, -1);
                startLine.Contents = "2";
                Assert.AreEqual(endLine.Value, 2);
            }

            // Pancake topology
            {
                int vars = 2500;
                IContext ctxt = CreatePancakeTopology(vars, out long edges, out Variable pancakeStart, out Variable pancakeEnd);
                pancakeStart.Contents = 1.ToString();
                Assert.AreEqual(pancakeEnd.Evaluate(), 1 * vars);
                pancakeStart.Contents = 2.ToString();
                Assert.AreEqual(pancakeStart.Evaluate(), 2);
                Assert.AreEqual(pancakeEnd.Evaluate(), 2 * vars);
            }

            // Spiral topology
            {
                IContext ctxt = CreateSpiralTopology(30, out long edges, out Variable varCore, out List<Variable> vars);
                varCore.Contents = "1";
                for (int i = 1; i < vars.Count; i++)
                {
                    Variable var = vars[i];
                    int shouldEqual = 1 << (i - 1);
                    Assert.AreEqual(var.Evaluate(), shouldEqual);
                }
                int val = 1;
                for (int i = 1; i < vars.Count; i++)
                {
                    // Name, inbound, listening structure, and unchanged value checked here.
                    Variable var = vars[i];
                    int inbound = (int)var.GetType().GetField("UnresolvedInbound", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(var);
                    Assert.AreEqual(inbound, 0);
                    Assert.AreEqual(var.Name, "spiral" + (i - 1).ToString("D5"));
                    for (int j = 0; j < i; j++)
                        Assert.IsTrue(var.ListensTo(vars[j]));
                    Assert.IsTrue(var.ListensTo(varCore));
                    Assert.AreEqual(var.Evaluate(), val << (i - 1), var.Name);

                }
            }
        }


        #endregion



        [TestMethod]
        public void TestParsing_Constant_Functions()
        {
            DummyContext context = new DummyContext(null, "dummy ctxt");
            Function.Factory factory = Function.Factory.StandardFactory;

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
            Variable varA = Variable.Declare(dummyA, "a");
            Variable varB = ((Reference)(Expression.FromString("dummy_context_b.b", dummyA).Commit())).Variable;
            IContext dummyB = ((DummyContext)dummyA).Subcontexts["dummy_context_b"];
            Variable varC = ((Reference)(Expression.FromString("dummy_context_b.dummy_context_c.c", dummyA).Commit())).Variable;
            IContext dummyC = ((DummyContext)dummyB).Subcontexts["dummy_context_c"];
            Expression.FromString("dummy_context_c", dummyB).Commit();

            varA.Contents = "10";
            varB.Contents = "5";
            varC.Contents = "2";


            IEvaluateable exp = Expression.FromString("dummy_context_b.dummy_context_c.c", dummyA).Commit();
            Assert.AreEqual(exp.Evaluate(), 2);

            exp = Expression.FromString("a + dummy_context_b.b + dummy_context_b.dummy_context_c.c", dummyA).Commit();
            Assert.AreEqual(exp.Evaluate(), 17);

            // This will only work if variables are scoped for all subcontexts:
            //                                                         vvv                                 vvv
            exp = Expression.FromString("c + b", dummyC).Commit();
            Assert.AreEqual(exp.Evaluate(), 7, "If it equals 2, it means that variable getting isn't looking at super-contexts.");

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
            IContext context = new DummyContext(null, "root");
            Variable aOrigin = Variable.Declare(context, "a");
            aOrigin.Contents = "2";
            IEvaluateable exp1 = Expression.FromString("3 + 5 * a ^ 2 / 4 - -1", context).Commit();


            MemoryStream outStream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(outStream, context);


            outStream.Seek(0, SeekOrigin.Begin);
            formatter = new BinaryFormatter();
            DummyContext deser = (DummyContext)formatter.Deserialize(outStream);

            Variable aPrime = deser["a"];
            Assert.AreEqual(aPrime.Name, "a");
            Assert.AreEqual(aPrime.Evaluate(), 2);

        }


        [TestMethod]
        public void TestParsing_Variables()
        {
            IContext context = new DummyContext(null, "dummyContext Test_Parsing_Variables");
            Variable varA = Variable.Declare(context, "a");
            Variable varB = Variable.Declare(context, "b");
            Variable varC = Variable.Declare(context, "c");
            Variable varA2 = context["a"];
            Variable varB2 = context["b"];
            Variable varC2 = context["c"];
            try
            {
                Variable.Declare(context,"a");
                Assert.Fail();
            }
            catch (DuplicateVariableException dvex)
            {
                Assert.AreEqual(dvex.ContextName, context.Name);
                Assert.AreEqual(dvex.VariableName, context["a"].Name);
                Assert.AreEqual(dvex.VariableName, "a");
            }
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
            varA.Update(out IDictionary<Variable, DataStructures.ChangedEventArgs<Variable, IEvaluateable>> changed);
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
            Variable varD = Variable.Declare(context, "d");
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
        public void TestParsing_Variables_Deleting()
        {

            // TEST THE BASIC CONTEXT
            // Variables which are not pointed to should NOT be deleted from this context.
            {
                // Create a variable that is independent of everything else.
                DummyContext root = new DummyContext(null, "root_context");
                Variable varA = Variable.Declare(root, "a", "5+3", Expression.DeletionStatus.NO_DELETION);
                Assert.AreEqual(varA.Value, 8);
                Variable nonexistingEmpty = null;
                try
                {
                    nonexistingEmpty = root["empty"];
                    Assert.Fail();
                }
                catch (KeyNotFoundException) { }
                Assert.IsNull(nonexistingEmpty);
                Assert.AreEqual(Expression.DeletionStatus.NO_DELETION, varA.DeletionStatus);

                // Create the variable which will eventually be deleted, and have 'a' point at it.
                Variable varTBD = Variable.Declare(root, "subcontext.tbd");
                DummyContext subctxt = (DummyContext)root.Subcontexts["subcontext"];
                varA.Contents = "subcontext.tbd + 4";
                Assert.AreEqual(varA.Value, 4);
                Variable varTBDPrime = subctxt["tbd"];
                Assert.AreEqual(varTBD, varTBDPrime);
                varTBDPrime = null;
                Assert.AreEqual(Expression.DeletionStatus.ALLOW_DELETION, varTBD.DeletionStatus);

                // Create a weak reference to varTBD and to subctxt and assure that they work just as well as the standard strong 
                // reference.
                WeakReference<Variable> weakVar = new WeakReference<Variable>(varTBD);
                Assert.IsTrue(weakVar.TryGetTarget(out Variable weakVarTemp));
                Assert.AreEqual(weakVarTemp, varTBD);
                weakVarTemp = null;
                WeakReference<IContext> weakCtxt = new WeakReference<IContext>(subctxt);
                Assert.IsTrue(weakCtxt.TryGetTarget(out IContext weakCtxtTemp));
                Assert.AreEqual(weakCtxtTemp, subctxt);
                weakCtxtTemp = null;

                // Get 'a' to point at something else.  Since nothing is now listening to 'tbd', it should automatically 
                // delete itself.  Since there's nothing left in subctxt, it should also automatically delete itself.
                varA.Contents = "9-2";
                Assert.AreEqual(varA.Value, 7);
                GC.Collect();
                Assert.AreEqual(Expression.DeletionStatus.ALLOW_DELETION, varTBD.DeletionStatus);
                Assert.AreEqual(Expression.DeletionStatus.ALLOW_DELETION, subctxt.DeletionStatus);
                Assert.IsTrue(subctxt.Variables.Contains("tbd"));
                Assert.IsTrue(root.Subcontexts.Contains("subcontext"));
            }


            // TEST THE SOFT CONTEXT
            // Variables which are not pointed to SHOULD be deleted from this context.
            {
                DummySoftContext root = new DummySoftContext(null, "dummy");
                Variable tbd = Variable.Declare(root, "tbd", "0", Expression.DeletionStatus.ALLOW_DELETION);
                Assert.IsTrue(root.Variables.Contains("tbd"));
                tbd = null;
                GC.Collect();
                Assert.IsFalse(root.Variables.Contains("tbd"));
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
            IContext ctxt;

            // The diamond test.  Goes exponentially wide before going exponentially narrow again.
            {
                ctxt = CreateDiamondTopology(1024, out long edges, out Variable diamondStart, out Variable diamondEnd);

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
                ctxt = CreateLadderTopology(50, out long edges, out Variable startA, out Variable startB, out Variable endA, out Variable endB);

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
                ctxt = CreateLineTopology(10000, out long edges, out Variable startLine, out Variable endLine);

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
                CreatePancakeTopology(vars, out long edges, out Variable pancakeStart, out Variable pancakeEnd);

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
                ctxt = CreateSpiralTopology(variables, out long edges, out Variable varCore, out List<Variable> vars);

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


        /// <summary>
        /// Used to test context feature.
        /// </summary>
        [Serializable]
        internal class DummyContext : Parsing.Contexts.BasicContext
        {


            public DummyContext(IContext parent, string name) : base(parent, name) { }

            protected override bool TryCreateSubcontext(string name, out IContext sub_ctxt)
            {
                name = name.ToLower();
                if (name.Contains("context"))
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
        }


        internal class DummySoftContext : SoftContext
        {
            public DummySoftContext(IContext parent, string name) : base(parent, name) { }

            protected override bool TryCreateSubcontext(string name, out IContext subcontext)
            {
                if (name.Contains("context"))
                {
                    subcontext = new DummySoftContext(this, name);
                    return true;
                }
                subcontext = null;
                return false;
            }

            protected override bool TryCreateVariable(string name, out Variable v)
            {
                v = new Variable(this, name);
                return true;
            }
        }

    }
}
