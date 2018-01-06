using System;
using DataStructures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class Test_Heap
    {
        [TestMethod]
        public void Test_Heap_Add()
        {
            //Ordered on, ordered off.
            Heap<int> heap = new Heap<int>();
            for (int i = 0; i < 100; i++) heap.Add(i);
            Assert.AreEqual(100, heap.Count);
            for (int i = 0; i < 100; i++) Assert.AreEqual(i, heap.Pop());
            Assert.AreEqual(0, heap.Count);

            //Allow for duplicate items.
            heap = new Heap<int>();
            Assert.AreEqual(0, heap.Count);
            heap.Add(1);
            Assert.AreEqual(1, heap.Count);
            heap.Add(1);
            Assert.AreEqual(2, heap.Count);
            heap.Clear();
            Assert.AreEqual(0, heap.Count);
            heap.Add(1);
            heap.Add(1);
            Assert.AreEqual(1, heap.Pop());
            Assert.AreEqual(1, heap.Pop());

            //Test in permutation.
            int[] items = new int[100];
            for (int i = 0; i < items.Length; i++) items[i] = i;
            items = Mathematics.Random.Permute(items);
            foreach (int item in items) heap.Add(item);
            Assert.AreEqual(100, heap.Count);
            for (int i = 0; i < items.Length; i++) Assert.AreEqual(i, heap.Pop());
            Assert.AreEqual(0, heap.Count);
        }
    }
}
