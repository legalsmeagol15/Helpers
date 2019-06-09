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

namespace UnitTests
{
    [TestClass]
    public class Test_Dependency
    {
        [TestMethod]
        public void TestDependency_Parsing()
        {
            IEvaluateable exp1 = Parse.FromString("(3-5+2^3/4*-7)");  // -16
            IEvaluateable exp2 = Parse.FromString("(3-5+(2^3)/4*-7"); // Still -16
            
            Assert.AreEqual(exp1.UpdateValue(), -16);
            Assert.AreEqual(exp1.Value, -16);
            Assert.AreEqual(exp2.UpdateValue(), -16);
            Assert.AreEqual(exp2.Value, -16);

            var timings = DoTiming(() => exp1.UpdateValue(), 512, 6);
            PrintTimings(timings);
        }

        private static void PrintTimings(double[] timings)
        {
            for (int i = 0; i < timings.Length; i++)
            {
                Console.Write(i + ":\t" + timings[i].ToString("0.00000"));
                if (i > 0)
                    Console.Write("\t" + ( timings[i] / timings[i - 1]));
                Console.Write("\n");
            }
        }
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
