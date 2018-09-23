using System;
using Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using static Parsing.Context;

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

            IEvaluateable pi = Expression.FromString("PI", context).Release();
            Assert.AreEqual("PI", pi.ToString());
            Assert.AreEqual(pi.Evaluate(), (decimal)Math.PI);
            pi = Expression.FromString("PI()", context).Release();
            Assert.AreEqual("PI", pi.ToString());
            Assert.AreEqual(pi.Evaluate(), (decimal)Math.PI);

            IEvaluateable e = Expression.FromString("E", context).Release();
            Assert.AreEqual("E", e.ToString());
            Assert.AreEqual(e.Evaluate(), (decimal)Math.E);
            e = Expression.FromString("E()", context).Release();
            Assert.AreEqual("E", e.ToString());
            Assert.AreEqual(e.Evaluate(), (decimal)Math.E);
            
        }

        [TestMethod]
        public void TestParsing_Contexts()
        {
            DummyContext dummyA = new DummyContext(null, "dummy_context_a");
            Assert.IsTrue(dummyA.TryAdd("dummy_context_b", out Context dummyB));
            Assert.IsTrue(dummyB.TryAdd("dummy_context_c", out Context dummyC));


            Variable varA, varB, varC;
            Assert.IsTrue(dummyA.TryAdd("a", out varA));
            Assert.IsTrue(dummyB.TryAdd("b", out varB));
            Assert.IsTrue(dummyC.TryAdd("c", out varC));

            Assert.IsTrue(dummyA.TryGet("a", out varA));
            Assert.IsTrue(dummyB.TryGet("b", out varB));
            Assert.IsTrue(dummyC.TryGet("c", out varC));

            varA.Contents = "10";
            varB.Contents = "5";
            varC.Contents = "2";
            Assert.IsFalse(dummyB.TryGet("a", out Variable testA));
            Assert.IsNull(testA);
            varB.Contents = "a + 9"; // Context "context-b" has no reference to "a", but the parent does.
            Assert.IsFalse(dummyB.TryGet("a", out testA));
            Assert.IsNull(testA);
            Assert.AreEqual(varB.Evaluate(), 19);


            Assert.IsTrue(dummyA.TryGet("dummy_context_b", out Context testContext));
            Assert.AreEqual(dummyB, testContext);
            
            Assert.IsTrue(dummyB.TryGet("dummy_context_c", out testContext));
            Assert.AreEqual(dummyC, testContext);

            IEvaluateable exp = Expression.FromString("dummy_context_b.dummy_context_c.c", dummyA).Release();
            Assert.AreEqual(exp.Evaluate(), 2);

            exp = Expression.FromString("dummy_context_b.dummy_context_c.c + dummy_context_b.b", dummyA).Release();
            Assert.AreEqual(exp.Evaluate(), 21);

            // This will only work if variables are scoped for all subcontexts:
            //                                                         vvv                                 vvv
            exp = Expression.FromString("dummy_context_b.dummy_context_c.c + dummy_context_b.dummy_context_c.b", dummyA).Release();
            Assert.AreEqual(exp.Evaluate(), 21);

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
            IEvaluateable e = Expression.FromString("2+1", null).Release();
            Assert.AreEqual("2 + 1", e.ToString());
            Assert.AreEqual(e.Evaluate(), 3);

            e = Expression.FromString("3+(2+1)", null).Release();
            Assert.AreEqual("3 + ( 2 + 1 )", e.ToString());
            Assert.AreEqual(e.Evaluate(), 6);

            e = Expression.FromString("(3+2)+1",  null).Release();
            Assert.AreEqual("( 3 + 2 ) + 1", e.ToString());
            Assert.AreEqual(e.Evaluate(), 6);

            e = Expression.FromString("(4+3)", null).Release();
            Assert.AreEqual("( 4 + 3 )", e.ToString());
            Assert.AreEqual(e.Evaluate(), 7);

            e = Expression.FromString("((4+3))",  null).Release();
            Assert.AreEqual("( ( 4 + 3 ) )", e.ToString());
            Assert.AreEqual(e.Evaluate(), 7);

            e = Expression.FromString("((3+2))+1",  null).Release();
            Assert.AreEqual("( ( 3 + 2 ) ) + 1", e.ToString());
            Assert.AreEqual(e.Evaluate(), 6);

            e = Expression.FromString("3+((2+1))", null).Release();
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
            IEvaluateable e = Expression.FromString("5+4", null).Release();
            Assert.AreEqual(e.Evaluate(), 9m);
            e = Expression.FromString("5+-4", null).Release();
            Assert.AreEqual(e.Evaluate(), 1);
            e = Expression.FromString("-5+4", null).Release();
            Assert.AreEqual(e.Evaluate(), -1);
            e = Expression.FromString("-5+-4", null).Release();
            Assert.AreEqual(e.Evaluate(), -9);

            // Subtraction
            e = Expression.FromString("5-4", null).Release();
            Assert.AreEqual(e.Evaluate(), 1);
            e = Expression.FromString("5--4", null).Release();
            Assert.AreEqual(e.Evaluate(), 9);
            e = Expression.FromString("-5-4", null).Release();
            Assert.AreEqual(e.Evaluate(), -9);
            e = Expression.FromString("-5--4", null).Release();
            Assert.AreEqual(e.Evaluate(), -1);

            // Multiplication
            e = Expression.FromString("5*4", null).Release();
            Assert.AreEqual(e.Evaluate(), 20);
            e = Expression.FromString("5*-4", null).Release();
            Assert.AreEqual(e.Evaluate(), -20);
            e = Expression.FromString("-5*4",  null).Release();
            Assert.AreEqual(e.Evaluate(), -20);
            e = Expression.FromString("-5*-4",  null).Release();
            Assert.AreEqual(e.Evaluate(), 20);

            // Division
            e = Expression.FromString("5/4", null).Release();
            Assert.AreEqual(e.Evaluate(), 1.25);
            e = Expression.FromString("5/-4", null).Release();
            Assert.AreEqual(e.Evaluate(), -1.25);
            e = Expression.FromString("-5/4", null).Release();
            Assert.AreEqual(e.Evaluate(), -1.25);
            e = Expression.FromString("-5/-4", null).Release();
            Assert.AreEqual(e.Evaluate(), 1.25);

            // Exponentiation
            e = Expression.FromString("2^4", null).Release();
            Assert.AreEqual(e.Evaluate(), 16);
        }


        [TestMethod]
        public void TestParsing_Serialization()
        {
            Context context = new DummyContext(null, "root");
            //IEvaluateable exp1 = Expression.FromString("3", context);
            Assert.IsTrue(context.TryAdd("a", out Variable aVar));
            IEvaluateable exp1 = Expression.FromString("3 + 5 * a ^ 2 / 4 - -1", context).Release();


            MemoryStream outStream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(outStream, context);
            

            outStream.Seek(0, SeekOrigin.Begin);
            formatter = new BinaryFormatter();
            DummyContext deser = (DummyContext)formatter.Deserialize(outStream);
            
            Assert.IsTrue(deser.TryGet("a", out Variable aPrime));
            Assert.AreEqual(aVar.Name, aPrime.Name);
            Assert.AreEqual(aVar.Evaluate(), aPrime.Evaluate());



        }


        [TestMethod]
        public void TestParsing_Variables()
        {
            Context context = new DummyContext(null, "dummyContext Test_Parsing_Variables");
            Variable a, b, c;
            Assert.IsTrue(context.TryAdd("a", out a));
            Assert.IsFalse(context.TryAdd("a", out Variable _));
            Assert.IsTrue(context.TryAdd("b", out b));
            Assert.IsTrue(context.TryAdd("c", out c));
            Assert.IsTrue(context.TryGet("c", out Variable c2));
            Assert.IsTrue(ReferenceEquals(c, c2));
            
            Assert.AreEqual(a.Context, context);
            Assert.AreEqual(b.Context, context);
            Assert.AreEqual(c.Context, context);
            Assert.AreEqual(context["a"], a);
            Assert.AreEqual(context["b"], b);
            Assert.AreEqual(context["c"], c);
            int valA = 1, valB = 2, valC = 3;
            context["a"].Contents = "" + valA;
            context["b"].Contents = "" + valB;
            context["c"].Contents = "" + valC;

            // Do a simple evaluation of an expression containing a variable.
            IEvaluateable exp = Expression.FromString("5a+3", context).Release();
            a.Update(out ISet<Variable> changed);
            Assert.AreEqual(0, changed.Count);
            Assert.AreEqual(exp.Evaluate(), (5*valA + 3));
            Assert.AreEqual(exp.Evaluate(), 8);
            Assert.IsTrue(exp is Clause clause);
            
           

            // Do a more-complex evaluation of an expression containing multiple variables.
            exp = Expression.FromString("4a + 2b*(3c+4)", context).Release();
            b.Update(out changed);
            Assert.AreEqual(0, changed.Count);
            c.Update(out changed);
            Assert.AreEqual(0, changed.Count);
            Assert.AreEqual(exp.Evaluate(), (4 * valA) + ((2 * valB) * (3 * valC + 4)));
            Assert.AreEqual(exp.Evaluate(), 56);


            // Change a variable's stored value, and test.
            valA = 5;
            a.Contents = "" + new Number(valA);
            Assert.AreEqual(exp.Evaluate(), 4 * valA + 2 * valB * (3 * valC + 4));
            Assert.AreEqual(exp.Evaluate(), 72);


            // Change a variable's contents to another expression.
            b.Contents = "4+(2*3)";
            Assert.AreEqual(b.Evaluate(), valB = 10);
            Assert.AreEqual(exp.Evaluate(), 4 * valA + 2 * valB * (3 * valC + 4));
            Assert.AreEqual(exp.Evaluate(), 280);



            // Change a variable's contents to an expression based on another variable.
            b.Contents = "4a-7";            
            Assert.AreEqual(exp.Evaluate(), 4 * valA + 2 * (valB = 4 * valA - 7) * (3 * valC + 4));
            Assert.AreEqual(exp.Evaluate(), 358);
            Assert.AreEqual(b.Evaluate(), valB);


            // Now, change the value of the 'a' and see if the change propogates up through the two expressions.
            a.Contents = "-1";
            valA = -1;
            valB = 4 * valA - 7;
            int compare = 4 * valA + 2 * valB * (3 * valC + 4);
            // If 'b' equals 334, the value assigned to 'a' did not propogate.
            Assert.AreEqual(exp.Evaluate(), compare);
            Assert.AreEqual(exp.Evaluate(),-290);

            // Now, create a circular dependency and test for an exception.
            try
            {
                a.Contents = "2b-14";                
                Assert.Fail();
            }
            catch(CircularDependencyException cdex)
            {
                // Make sure state wasn't actually changed.
                Assert.AreEqual(exp.Evaluate(), compare);
                Assert.AreEqual(exp.Evaluate(), -290);
                Assert.AreEqual(cdex.Tested, a);
               // Assert.AreEqual(cdex.Dependee, b);
            }

            // Test for exceptions from self-referencing circularity.
            Assert.IsTrue(context.TryAdd("d", out Variable d));
            Assert.AreEqual(d.Evaluate(), null);
            try
            {
                d.Contents = "d";
                Assert.Fail();
            } catch (CircularDependencyException cdex)
            {
                Assert.AreEqual(d.Evaluate(), null);
                Assert.AreEqual(cdex.Tested, d);
                //Assert.AreEqual(cdex.Dependee, d);
            }
            
        }

        /// <summary>
        /// Used to test context feature.
        /// </summary>
        [Serializable]
        internal class DummyContext : Parsing.Context
        {
            

            public DummyContext(Context parent, string name) : base(parent, name)
            {
                Variables = new Dictionary<string, Variable>();
                Subcontexts = new Dictionary<string, Context>();
            }

            public override bool TryAdd(string name, out Context sub_ctxt)
            {
                if (!name.StartsWith("dummy")  || Subcontexts.ContainsKey(name) || Variables.ContainsKey(name)) {sub_ctxt = null; return false; }
                sub_ctxt = new DummyContext(this, name);
                Subcontexts.Add(name, sub_ctxt);
                return true;                
            }

        }
    }
}
