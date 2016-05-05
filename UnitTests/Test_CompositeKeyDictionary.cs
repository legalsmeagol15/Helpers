using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace UnitTests
{
    [TestClass]
    public class Test_CompositeKeyDictionary
    {
        private DataStructures.CompositeKeyDictionary<int, string> ckd0;

        [TestInitialize]
        public void Test_CompositeKeyDictionary_Initialize()
        {
            ckd0 = new DataStructures.CompositeKeyDictionary<int, string>();
            Assert.AreEqual(ckd0.Count, 0);
            for (int i = 0; i < 34; i++)
            {
                HashSet<int> key = new HashSet<int>();
                key.Add(i);
                key.Add(i + 1);
                ckd0.Add(key, "Item" + i);
            }

            Assert.AreEqual(ckd0.Count, 34);
        }

        [TestMethod]
        public void Test_CompositeKeyDictionary_Add()
        {
            
            for (int i = 0; i < 34; i++)
            {
                HashSet<int> key = new HashSet<int>();
                key.Add(i);
                key.Add(i + 1);
                Assert.IsTrue(ckd0.ContainsKey(key));
                HashSet<int> reversedKey = new HashSet<int>();
                key.Add(i + 1);
                key.Add(i);               
                Assert.IsTrue(ckd0.ContainsKey(key));
                HashSet<int> singletonKey = new HashSet<int>();
                singletonKey.Add(i + i + 1);
                Assert.IsFalse(ckd0.ContainsKey(singletonKey));
            }

            

            
        }

        [TestMethod]
        public void Test_CompositeKeyDictionary_Remove()
        {
            HashSet<int> removeSet = new HashSet<int>();
            removeSet.Add(2);
            removeSet.Add(3);
            Assert.IsTrue(ckd0.Remove(removeSet));
            Assert.AreEqual(33, ckd0.Count);
            Assert.IsFalse(ckd0.ContainsKey(removeSet));
            Assert.IsFalse(ckd0.Remove(removeSet));
            Assert.AreEqual(33, ckd0.Count);

            HashSet<int> revRemoveSet = new HashSet<int>();
            revRemoveSet.Add(7);
            revRemoveSet.Add(6);
            Assert.IsTrue(ckd0.Remove(revRemoveSet));
            Assert.AreEqual(32, ckd0.Count);
            Assert.IsFalse(ckd0.ContainsKey(revRemoveSet));
            Assert.IsFalse(ckd0.Remove(revRemoveSet));
            Assert.AreEqual(32, ckd0.Count);

            HashSet<int> clearedSet = new HashSet<int>();
            clearedSet.Add(1);
            clearedSet.Add(2);
            Assert.IsTrue(ckd0.ContainsKey(clearedSet));
            ckd0.Clear();
            Assert.IsFalse(ckd0.ContainsKey(clearedSet));
            Assert.AreEqual(0, ckd0.Count);
            
        }
    }
}
