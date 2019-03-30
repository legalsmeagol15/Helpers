using DataStructures;
using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static UnitTests.Common;


namespace UnitTests
{
    [TestClass]
    public class TestDeque
    {
        [TestMethod]
        public void Test_Deque_Ctor()
        {
            Deque<int> ml = new Deque<int>();
            Assert.AreEqual(0, ml.Count);
            Assert.AreEqual(16, ml.Capacity);

            int[] items = { 1, 17, 42, 14, 3, 5, -2, 6, 18, 4, 3, 53, 4, 0, 2, 2, 54, 18 };
            ml = new Deque<int>(items);
            Assert.AreEqual(items.Length, ml.Count);
            Assert.AreEqual(Mathematics.Int32.RoundUpPow2(items.Length), ml.Capacity);

            foreach (int item in items)
                Assert.AreEqual(items.TakeWhile(i => i != item).Count(), ml.IndexOf(item));
            Assert.AreEqual(4, ml.IndexOf(3));
        }

        [TestMethod]
        public void Test_Deque_Add()
        {
            Deque<int> ml = new Deque<int>(4);
            Assert.AreEqual(0, ml.Count);
            Assert.AreEqual(4, ml.Capacity);

            ml.AddLast(5);
            ml.AddLast(6);
            ml.AddLast(7);
            ml.AddLast(8);
            ml.AddLast(9);
            Assert.AreEqual(5, ml.Count);
            Assert.AreEqual(8, ml.Capacity);

            ml.AddFirst(4);
            ml.AddFirst(3);
            ml.AddFirst(2);
            ml.AddFirst(1);
            Assert.AreEqual(9, ml.Count);
            Assert.AreEqual(16, ml.Capacity);

            for (int j = 1; j <= 9; j++)
                Assert.IsTrue(ml.Contains(j));
            Assert.IsFalse(ml.Contains(0));
            Assert.IsFalse(ml.Contains(10));

            int i = 1;
            foreach (int item in ml)
                Assert.AreEqual(i++, item);
        }

        [TestMethod]
        public void Test_Deque_Clear()
        {
            int[] items = new int[] { 1, 2, 3, 4 };
            Deque<int> ml = new Deque<int>(items);
            Assert.AreEqual(4, ml.Count);
            Assert.AreEqual(4, ml.Capacity);

            for (int i = 1; i < ml.Count; i++)
                Assert.IsTrue(ml.Contains(items[i]));

            ml.Clear();
            Assert.AreEqual(0, ml.Count);
            Assert.AreEqual(4, ml.Capacity);
            foreach (int item in ml)
                Assert.Fail();
            for (int i = 1; i < ml.Count; i++)
                Assert.IsFalse(ml.Contains(items[i]));
        }

        [TestMethod]
        public void Test_Deque_Indexing()
        {
            int[] items = { 0, 1, 2, 3, 4, 5, 6, 7 };
            Deque<int> ml = new Deque<int>(items);
            for (int i = 0; i < 7; i++)
                ml[i] = ml[i];
            for (int i = 0; i < 7; i++)
                Assert.IsTrue(ml.Contains(i));
            int idx = 0;
            foreach (int item in ml)
                Assert.AreEqual(idx++, item);
        }

        [TestMethod]
        public void Test_Deque_Insert()
        {
            int[] items = { 0,1,2,3,4,5,6,7 };
            Deque<int> ml = new Deque<int>(16);
            ml.Insert(0, 10);
            Assert.IsTrue(ml.Contains(10));
            Assert.AreEqual(1, ml.Count);
            Assert.AreEqual(16, ml.Capacity);

            AssertThrows(() => ml.Insert(2, 20));
            AssertThrows(() => ml.Insert(-1, 20));
            Assert.IsFalse(ml.Contains(20));
            ml.Insert(1, 20);
            Assert.IsTrue(ml.Contains(20));
            Assert.AreEqual(2, ml.Count);
            Assert.AreEqual(16, ml.Capacity);

            // Try inserting where Count = Capacity = 8
            ml = new Deque<int>(items);
            Assert.IsFalse(ml.Contains(20));
            Assert.IsFalse(ml.Contains(10));
            Assert.AreEqual(8, ml.Count);
            Assert.AreEqual(8, ml.Capacity);
            ml.Insert(8, 30);
            Assert.IsTrue(ml.Contains(30));
            Assert.AreEqual(9, ml.Count);
            Assert.AreEqual(16, ml.Capacity);

            // Try inserting where Count=6, Capacity = 8
            ml = new Deque<int>(items);
            Assert.AreEqual(0, ml.RemoveFirst());
            Assert.AreEqual(1, ml.RemoveFirst());
            Assert.IsFalse(ml.Contains(0));
            Assert.IsFalse(ml.Contains(1));
            Assert.AreEqual(2, ml[0]);
            ml.Insert(6, 30);
            Assert.IsTrue(ml.Contains(30));
            ml.Insert(7, 40);
            Assert.IsTrue(ml.Contains(40));
            for (int i = 2; i <= 7; i++)
                Assert.IsTrue(ml.Contains(i));
            Assert.AreEqual(8, ml.Count);
            Assert.AreEqual(8, ml.Capacity);
            ml.Insert(8, 50);
            Assert.IsTrue(ml.Contains(50));
            Assert.AreEqual(9, ml.Count);
            Assert.AreEqual(16, ml.Capacity);

            ml = new Deque<int>(items);
            Assert.AreEqual(0, ml.RemoveFirst());
            Assert.AreEqual(1, ml.RemoveFirst());
            ml.Insert(0, 10);
            Assert.IsTrue(ml.Contains(10));
            Assert.AreEqual(7, ml.Count);
            Assert.AreEqual(8, ml.Capacity);
            ml.Insert(3, 20);
            Assert.IsTrue(ml.Contains(20));
            Assert.AreEqual(8, ml.Count);
            Assert.AreEqual(8, ml.Capacity);
            ml.Insert(3, 30);
            Assert.AreEqual(9, ml.Count);
            Assert.AreEqual(16, ml.Capacity);
            for (int i = 2; i <= 7; i++) Assert.IsTrue(ml.Contains(i));
            Assert.IsTrue(ml.Contains(10));
            Assert.IsTrue(ml.Contains(20));
            Assert.IsTrue(ml.Contains(30));
            Assert.AreEqual(10, ml[0]);
            Assert.AreEqual(2, ml[1]);
            Assert.AreEqual(3, ml[2]);
            Assert.AreEqual(30, ml[3]);
            Assert.AreEqual(20, ml[4]);
            Assert.AreEqual(4, ml[5]);
            Assert.AreEqual(5, ml[6]);
            Assert.AreEqual(6, ml[7]);
            Assert.AreEqual(7, ml[8]);
            ml.Insert(5, 40);
            Assert.AreEqual(10, ml[0]);
            Assert.AreEqual(2, ml[1]);
            Assert.AreEqual(3, ml[2]);
            Assert.AreEqual(30, ml[3]);
            Assert.AreEqual(20, ml[4]);
            Assert.AreEqual(40, ml[5]);
            Assert.AreEqual(4, ml[6]);
            Assert.AreEqual(5, ml[7]);
            Assert.AreEqual(6, ml[8]);
            Assert.AreEqual(7, ml[9]);


        }

        [TestMethod]
        public void Test_Deque_Remove()
        {
            int[] items = { 0, 1, 2, 3, 4, 5, 6, 7 };
            Deque<int> ml = new Deque<int>(items);
            Assert.AreEqual(7, ml.RemoveLast());
            Assert.AreEqual(7, ml.Count);
            Assert.IsFalse(ml.Contains(7));
            for (int i = 0; i <= 6; i++) Assert.IsTrue(ml.Contains(i));
            Assert.AreEqual(0, ml.RemoveFirst());
            Assert.AreEqual(6, ml.Count);
            Assert.IsFalse(ml.Contains(0));
            for (int i = 1; i <= 6; i++) Assert.IsTrue(ml.Contains(i));

            ml = new Deque<int>(items);
            Assert.AreEqual(0, ml.RemoveFirst());
            Assert.AreEqual(1, ml.RemoveFirst());
            ml.AddLast(10);
            ml.AddLast(20);
            Assert.AreEqual(8, ml.Count);
            Assert.AreEqual(8, ml.Capacity);
            Assert.IsFalse(ml.Remove(100));
            Assert.IsTrue(ml.Remove(2));
            Assert.IsFalse(ml.Remove(2));
            Assert.AreEqual(7, ml.Count);
            for (int i = 3; i <= 7; i++) Assert.IsTrue(ml.Contains(i));
            Assert.IsTrue(ml.Contains(10));
            Assert.IsTrue(ml.Contains(20));
            ml.RemoveAt(2);
            Assert.IsTrue(ml.Contains(3));
            Assert.IsTrue(ml.Contains(4));
            Assert.IsTrue(ml.Contains(6));
            Assert.IsTrue(ml.Contains(7));
            Assert.IsTrue(ml.Contains(10));
            Assert.IsTrue(ml.Contains(20));
            Assert.AreEqual(6, ml.Count);

            for (int i = 0; i < 6; i++) ml.RemoveFirst();
            Assert.AreEqual(0, ml.Count);
            for (int i = 0; i <= 7; i++) Assert.IsFalse(ml.Contains(i));
            Assert.IsFalse(ml.Contains(10));
            Assert.IsFalse(ml.Contains(20));

            

        }
    }
}
