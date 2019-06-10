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
using System.Runtime.CompilerServices;

namespace UnitTests
{
    [TestClass]
    public class Test_Dependency
    {
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
            IEvaluateable exp2 = Parse.FromString("(3-5+(2^3)/4*-7-1"); // Still -17
            IEvaluateable exp3 = Parse.FromString("(3-5+(((2^3)/4)*-7-1)"); // Still -17

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
        public void TestDependency_ToString()
        {
            IEvaluateable exp0 = Parse.FromString("3-5+2^3/4*-7-1");
            IEvaluateable exp1 = Parse.FromString("(3-5+2^3/4*-7-1)"); 
            IEvaluateable exp2 = Parse.FromString("(3-5+(2^3)/4*-7-1");
            IEvaluateable exp3 = Parse.FromString("(3-5+(((2^3)/4)*-7-1)");

            Assert.AreEqual("3 - 5 + 2 ^ 3 / 4 * -7 - 1", exp0.ToString());
            Assert.AreEqual("( 3 - 5 + 2 ^ 3 / 4 * -7 - 1 )", exp1.ToString());
            Assert.AreEqual("( 3 - 5 + ( 2 ^ 3 ) / 4 * -7 - 1 )", exp2.ToString());
            Assert.AreEqual("( 3 - 5 + ( ( ( 2 ^ 3 ) / 4 ) * -7 - 1 ) )", exp3.ToString());
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

}
