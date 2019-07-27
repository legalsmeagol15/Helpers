using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DataStructures;
using System.Collections.Generic;
using System.Diagnostics;

namespace UnitTests
{
    [TestClass]
    public class Test_WeakReferenceSet
    {
        private List<Integer> _Integers = new List<Integer>();
        [TestInitialize]
        public void Initialize()
        {
            for (int i = 0; i < 100; i++) _Integers.Add(new Integer(i));
        }
        
        [TestMethod]
        public void Test_Add()
        {
            
            WeakReferenceSet<Integer> wrsA = new WeakReferenceSet<Integer>(0);
            for (int i = 0; i < 20; i++)
            {
                Assert.IsTrue(wrsA.Add(_Integers[i]));
            }
            Assert.AreEqual(20, wrsA.Count);
            for (int i = 0; i < 20; i++)
            {
                Assert.IsFalse(wrsA.Add(_Integers[i]));
            }
            Assert.AreEqual(20, wrsA.Count);
            int idx = 0;
            foreach (Integer val in wrsA)
            {
                Assert.AreEqual(val, _Integers[idx++]);
            }

            Common.Permute(_Integers);
            WeakReferenceSet<Integer> wrsB = new WeakReferenceSet<Integer>(0);
            foreach (Integer i in _Integers)
                Assert.IsTrue(wrsB.Add(i));

            // Since an Integer's hash is its value, this should put them in order.
            Integer[] sorted = _Integers.ToArray();
            Array.Sort(sorted, (a, b) => a.Value.CompareTo(b.Value));
            idx = 0;
            foreach (Integer val in wrsB)
            {
                Assert.AreEqual(val, sorted[idx++]);
            }
        }

        [TestMethod]
        public void Test_Compaction()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void Test_Iteration()
        {
            WeakReferenceSet<Integer> wrsA = new WeakReferenceSet<Integer>(0);
            var random = new List<Integer>(_Integers);
            Common.Permute(random);
            for (int i = 0; i < random.Count; i++)
            {
                Assert.IsTrue(wrsA.Add(random[i]));
            }
            for (int i = 0; i < random.Count; i++)
            {
                Assert.IsTrue(wrsA.Contains(random[i]));
            }

            int idx = 0;
            foreach (Integer i in wrsA)
            {
                Assert.AreEqual(_Integers[idx++], i);
            }
        }

        [TestMethod]
        public void Test_Remove()
        {
            WeakReferenceSet<Integer> wrsA = new WeakReferenceSet<Integer>(0);
            for (int i = 0; i < _Integers.Count; i++)
            {
                Assert.IsTrue(wrsA.Add(_Integers[i]));
            }
            for (int i = 0; i < _Integers.Count; i++)
            {
                Assert.IsTrue(wrsA.Contains(_Integers[i]));
            }
            Assert.AreEqual(_Integers.Count, wrsA.Count);

            for  (int  i =0; i < _Integers.Count; i++)
            {
                Assert.IsTrue(wrsA.Remove(_Integers[i]));
            }
            for (int i = 0; i < _Integers.Count; i++)
            {
                Assert.IsFalse(wrsA.Contains(_Integers[i]));
            }
            Assert.AreEqual(0, wrsA.Count);


            // Now do it randomly.
            Common.Permute(_Integers);
            for (int i = 0; i < _Integers.Count; i++)
            {
                Assert.IsTrue(wrsA.Add(_Integers[i]));
            }
            for (int i = 0; i < _Integers.Count; i++)
            {
                Assert.IsTrue(wrsA.Contains(_Integers[i]));
            }
            Common.Permute(_Integers);
            for (int i = 0; i < _Integers.Count; i++)
            {
                Assert.IsTrue(wrsA.Remove(_Integers[i]));
            }
            for (int i = 0; i < _Integers.Count; i++)
            {
                Assert.IsFalse(wrsA.Contains(_Integers[i]));
            }
            Assert.AreEqual(0, wrsA.Count);

        }





        private class Integer
        {
            public readonly int Value;
            [DebuggerStepThrough]
            public Integer(int value) { this.Value = value; }
            public override string ToString() => Value.ToString();
            [DebuggerStepThrough]
            public override int GetHashCode() => Value;
            [DebuggerStepThrough]
            public override bool Equals(object obj) => base.Equals(obj);
        }
    }
}
