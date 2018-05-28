﻿using System;
using Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using static Parsing.DataContext;

namespace UnitTests
{
    [TestClass]
    public class Test_Parsing
    {
        private Function.Factory factory;
        [TestInitialize]
        public void TestParsing__Initialize()
        {
            factory = Function.Factory.StandardFactory();
        }


        [TestMethod]
        public void TestParsing_Constant_Functions()
        {
            Function f1 = factory["PI"];
            Function f2 = factory["PI"];
            Assert.IsTrue(ReferenceEquals(f1, f2));
            Assert.AreEqual(factory["PI"].Evaluate(), (decimal)Math.PI);
            Assert.AreEqual(factory["E"].Evaluate(), (decimal)Math.E);

            IEvaluatable pi = Expression.FromString("PI", factory, null);
            Assert.AreEqual("PI", pi.ToString());
            Assert.AreEqual(pi.Evaluate(), (decimal)Math.PI);
            pi = Expression.FromString("PI()", factory, null);
            Assert.AreEqual("PI", pi.ToString());
            Assert.AreEqual(pi.Evaluate(), (decimal)Math.PI);

            IEvaluatable e = Expression.FromString("E", factory, null);
            Assert.AreEqual("E", e.ToString());
            Assert.AreEqual(e.Evaluate(), (decimal)Math.E);
            e = Expression.FromString("E()", factory,  null);
            Assert.AreEqual("E", e.ToString());
            Assert.AreEqual(e.Evaluate(), (decimal)Math.E);
            
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
            Assert.IsTrue(factory.Contains("Range"));
            Assert.IsTrue(factory.Contains("And"));
            Assert.IsTrue(factory.Contains("Or"));
            Assert.IsTrue(factory.Contains("PI"));
            Assert.IsTrue(factory.Contains("E"));  
            
            
        }



        [TestMethod]
        public void TestParsing_Nesting()
        {
            IEvaluatable e = Expression.FromString("2+1", factory, null);
            Assert.AreEqual("2 + 1", e.ToString());
            Assert.AreEqual(e.Evaluate(), 3);

            e = Expression.FromString("3+(2+1)", factory, null);
            Assert.AreEqual("3 + ( 2 + 1 )", e.ToString());
            Assert.AreEqual(e.Evaluate(), 6);

            e = Expression.FromString("(3+2)+1", factory,  null);
            Assert.AreEqual("( 3 + 2 ) + 1", e.ToString());
            Assert.AreEqual(e.Evaluate(), 6);

            e = Expression.FromString("(4+3)", factory, null);
            Assert.AreEqual("( 4 + 3 )", e.ToString());
            Assert.AreEqual(e.Evaluate(), 7);

            e = Expression.FromString("((4+3))", factory,  null);
            Assert.AreEqual("( ( 4 + 3 ) )", e.ToString());
            Assert.AreEqual(e.Evaluate(), 7);

            e = Expression.FromString("((3+2))+1", factory,  null);
            Assert.AreEqual("( ( 3 + 2 ) ) + 1", e.ToString());
            Assert.AreEqual(e.Evaluate(), 6);

            e = Expression.FromString("3+((2+1))", factory, null);
            Assert.AreEqual("3 + ( ( 2 + 1 ) )", e.ToString());
            Assert.AreEqual(e.Evaluate(), 6);
        }



        [TestMethod]
        public void TestParsing_Number_Equalities()
        {
            Function adder = factory["Addition"];
            // Test that functions are correctly doing calculations.
            Assert.IsTrue(adder.Evaluate(1, 1).Equals(2m));
            Assert.IsTrue(adder.Evaluate(1, 1).Equals(2));
            Assert.IsTrue(adder.Evaluate(1, 1).Equals(2d));
            Assert.IsTrue(adder.Evaluate(1m, 1m).Equals(2m));
            Assert.IsTrue(adder.Evaluate(1d, 1d).Equals(2m));
            Assert.AreEqual(adder.Evaluate(1, 1), 2);

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
            IEvaluatable e = Expression.FromString("5+4", factory, null);
            Assert.AreEqual(e.Evaluate(), 9m);
            e = Expression.FromString("5+-4", factory, null);
            Assert.AreEqual(e.Evaluate(), 1);
            e = Expression.FromString("-5+4", factory, null);
            Assert.AreEqual(e.Evaluate(), -1);
            e = Expression.FromString("-5+-4", factory, null);
            Assert.AreEqual(e.Evaluate(), -9);

            // Subtraction
            e = Expression.FromString("5-4", factory, null);
            Assert.AreEqual(e.Evaluate(), 1);
            e = Expression.FromString("5--4", factory, null);
            Assert.AreEqual(e.Evaluate(), 9);
            e = Expression.FromString("-5-4", factory, null);
            Assert.AreEqual(e.Evaluate(), -9);
            e = Expression.FromString("-5--4", factory, null);
            Assert.AreEqual(e.Evaluate(), -1);

            // Multiplication
            e = Expression.FromString("5*4", factory, null);
            Assert.AreEqual(e.Evaluate(), 20);
            e = Expression.FromString("5*-4", factory, null);
            Assert.AreEqual(e.Evaluate(), -20);
            e = Expression.FromString("-5*4", factory,  null);
            Assert.AreEqual(e.Evaluate(), -20);
            e = Expression.FromString("-5*-4", factory,  null);
            Assert.AreEqual(e.Evaluate(), 20);

            // Division
            e = Expression.FromString("5/4", factory, null);
            Assert.AreEqual(e.Evaluate(), 1.25);
            e = Expression.FromString("5/-4", factory, null);
            Assert.AreEqual(e.Evaluate(), -1.25);
            e = Expression.FromString("-5/4", factory, null);
            Assert.AreEqual(e.Evaluate(), -1.25);
            e = Expression.FromString("-5/-4", factory, null);
            Assert.AreEqual(e.Evaluate(), 1.25);

            // Exponentiation
            e = Expression.FromString("2^4", factory, null);
            Assert.AreEqual(e.Evaluate(), 16);
        }

        [TestMethod]
        public void Test_Variables()
        {
            DataContext context = new DataContext();
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
            context["a"].Contents = new Number(valA);
            context["b"].Contents = new Number(valB);
            context["c"].Contents = new Number(valC);

            // Do a simple evaluation of an expression containing a variable.
            IEvaluatable exp = Expression.FromString("5a+3", factory, context);
            Assert.AreEqual(exp.Evaluate(), (5*valA + 3));
            Assert.AreEqual(exp.Evaluate(), 8);
            Assert.IsTrue(exp is Clause clause);
            
           

            // Do a more-complex evaluation of an expression containing multiple variables.
            exp = Expression.FromString("4a + 2b*(3c+4)", factory, context);
            Assert.AreEqual(exp.Evaluate(), (4 * valA) + ((2 * valB) * (3 * valC + 4)));
            Assert.AreEqual(exp.Evaluate(), 56);
           

            // Change a variable's stored value, and test.
            a.Contents = new Number(valA = 5);
            Assert.AreEqual(exp.Evaluate(), 4 * valA + 2 * valB * (3 * valC + 4));
            Assert.AreEqual(exp.Evaluate(), 72);
            

            // Change a variable's contents to another expression.
            b.Contents = Expression.FromString("4+(2*3)", factory, context);
            Assert.AreEqual(b.Evaluate(), valB = 10);
            Assert.AreEqual(exp.Evaluate(), 4 * valA + 2 * valB * (3 * valC + 4));
            Assert.AreEqual(exp.Evaluate(), 280);
            
            

            // Change a variable's contents to an expression based on another variable.
            b.Contents = Expression.FromString("4a-7", factory, context);
            Assert.AreEqual(exp.Evaluate(), 4 * valA + 2 * (valB = 4 * valA - 7) * (3 * valC + 4));
            Assert.AreEqual(exp.Evaluate(), 358);
            Assert.AreEqual(b.Evaluate(), valB);
            

            // Now, change the value of the 'a' and see if the change propogates up through the two expressions.
            a.Contents = new Number(-1);
            valA = -1;
            valB = 4 * valA - 7;
            int compare = 4 * valA + 2 * valB * (3 * valC + 4);
            Assert.AreEqual(exp.Evaluate(), compare);
            Assert.AreEqual(exp.Evaluate(),-290);

            // Now, create a circular dependency and test for an exception.
            try
            {
                a.Contents = Expression.FromString("2b-14", factory, context);
                
                Assert.Fail();
            }
            catch(CircularDependencyException cdex)
            {
                // Make sure state wasn't actually changed.
                Assert.AreEqual(exp.Evaluate(), compare);
                Assert.AreEqual(exp.Evaluate(), -290);
                Assert.AreEqual(cdex.V0, a);
                Assert.AreEqual(cdex.V1, b);
            }            

            // Test for exceptions from self-referencing circularity.
            exp = Expression.FromString("d", factory, context);
            Assert.IsTrue(context.TryGet("d", out Variable d));
            Assert.AreEqual(d.Evaluate(), 0);
            try
            {
                d.Contents = Expression.FromString("d", factory, context);
                Assert.Fail();
            } catch (CircularDependencyException cdex)
            {
                Assert.AreEqual(d.Evaluate(), 0);
                Assert.AreEqual(cdex.V0, d);
                Assert.AreEqual(cdex.V1, d);
            }
            
        }
    }
}
