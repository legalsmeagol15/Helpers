using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DataStructures;
using System.Collections.Generic;

namespace UnitTests
{
    [TestClass]
    public class Test_WeakReferenceSet
    {
        [TestMethod]
        public void TestAdd()
        {
            List<Integer> list = new List<Integer>();
            for (int i = 0; i < 20; i++)
            {
                list.Add(new Integer(i));
            }

            WeakReferenceSet<Integer> wrsA = new WeakReferenceSet<Integer>(0);
            for (int i = 0; i < 20; i++)
            {
                Assert.IsTrue(wrsA.Add(list[i]));
            }
            Assert.AreEqual(20, wrsA.Count);
            for (int i = 0; i < 20; i++)
            {
                Assert.IsFalse(wrsA.Add(list[i]));
            }
            Assert.AreEqual(20, wrsA.Count);
            int idx = 0;
            foreach (Integer val in wrsA)
            {
                Assert.AreEqual(val, list[idx++]);
            }

            Common.Permute(list);
            WeakReferenceSet<Integer> wrsB = new WeakReferenceSet<Integer>(0);
            foreach (Integer i in list)
                Assert.IsTrue(wrsB.Add(i));

            // Since an Integer's hash is its value, this should put them in order.
            Integer[] sorted = list.ToArray();
            Array.Sort(sorted, (a, b) => a.Value.CompareTo(b.Value));
            idx = 0;
            foreach (Integer val in wrsB)
            {
                Assert.AreEqual(val, sorted[idx++]);
            }
        }

        private class Integer
        {
            public readonly int Value;
            public Integer(int value) { this.Value = value; }
            public override string ToString() => Value.ToString();
            public override int GetHashCode() => Value;
            public override bool Equals(object obj) => Value == ((Integer)obj).Value;
        }
    }
}
