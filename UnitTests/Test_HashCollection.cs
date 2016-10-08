using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DataStructures;
using System.Collections.Generic;

namespace UnitTests
{
    [TestClass]
    public class Test_HashCollection
    {
        [TestMethod]
        public void Test_HashCollection_Contents()
        {
            HashCollection<int> hc0 = new HashCollection<int>();
            for (int i = 0; i < 100; i++)
                hc0.Add(i);
            for (int i = 0; i < 100; i++)
                Assert.AreEqual(1, hc0.CountOf(1));
            Assert.AreEqual(0, hc0.CountOf(-1));
            Assert.AreEqual(0, hc0.CountOf(100));
            Assert.IsTrue(hc0.Contains(0));
            Assert.IsFalse(hc0.Contains(-1));

            hc0.Add(1);
            Assert.AreEqual(2, hc0.CountOf(1));
            hc0.Remove(0);
            Assert.IsFalse(hc0.Contains(0));
            hc0.Add(1);
            Assert.AreEqual(3, hc0.CountOf(1));
            hc0.Add(0);
            Assert.IsTrue(hc0.Contains(0));
            Assert.AreEqual(1, hc0.CountOf(0));

            hc0.Clear();
            for (int i = 0; i < 100; i++) Assert.IsFalse(hc0.Contains(i));

            HashCollection<int> hc1 = new HashCollection<int>();
            for (int i = 0; i < 100; i++) hc1.Add(i);
            Assert.IsFalse(hc1.Remove(-1)==0);

            HashCollection<int> hc2 = new HashCollection<int>();
            hc2.Add(0);
            Assert.IsFalse(hc2.Remove(1)==0);

        }

        [TestMethod]
        public void Test_HashCollection_ChangeCapacity()
        {
            HashCollection<int> hc0 = new HashCollection<int>();
            for (int i = 0; i < 20000; i++) hc0.Add(i);
            for (int i = 0; i < 20000; i++) Assert.AreEqual(1, hc0.CountOf(i));
            

            HashCollection<int> hc1 = new HashCollection<int>();
            for (int i = 0; i < 20000; i += 17) hc1.Add(i);
            for (int i = 0; i < 20000; i += 17) Assert.AreEqual(1, hc1.CountOf(i));
        }

        [TestMethod]
        public void Test_HashCollection_GetCopiesOf()
        {
            HashCollection<int> hc0 = new HashCollection<int>();
            hc0.Add(1);
            hc0.Add(1);
            hc0.Add(1);
            int[] positiveResult = hc0.GetCopiesOf(1);
            int[] negativeResult = hc0.GetCopiesOf(0);
            Assert.AreEqual(3, positiveResult.Length);
            Assert.AreEqual(0, negativeResult.Length);
            negativeResult = hc0.GetCopiesOf(18);
            Assert.AreEqual(0, negativeResult.Length);
            for (int i = 0; i < 3; i++) Assert.AreEqual(1, positiveResult[i]);
        }

        [TestMethod]
        public void Test_HashCollection_Enumeration()
        {
            HashCollection<int> hc0 = new HashCollection<int>();
            HashSet<int> compare = new HashSet<int>();

            for (int i = 0; i < 100; i++)
            {
                hc0.Add(i);
                Assert.IsTrue(compare.Add(i));                
            }
            Assert.IsTrue(compare.Count == 100);
                        
            foreach (int  i in hc0)
            {                
                Assert.IsTrue(compare.Remove(i));
            }
            Assert.IsTrue(compare.Count == 0);
            compare.Clear();

            HashCollection<int> hc1 = new HashCollection<int>();
            hc1.Add(1);
            hc1.Add(18);
            hc1.Add(35);
            foreach (int i in hc1) compare.Add(i);
            Assert.AreEqual(3, compare.Count);
            Assert.IsTrue(compare.Contains(1));
            Assert.IsTrue(compare.Contains(18));
            Assert.IsTrue(compare.Contains(35));

            HashCollection<int> hc2 = new HashCollection<int>(compare);
            Assert.IsTrue(compare.Contains(1));
            Assert.IsTrue(compare.Contains(18));
            Assert.IsTrue(compare.Contains(35));
            Assert.IsFalse(compare.Contains(0));

        }
    }
}
