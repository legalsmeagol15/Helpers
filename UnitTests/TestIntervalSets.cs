using System;
using DataStructures;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HelpersTest
{
    [TestClass]
    public class TestIntervalSets
    {
        [TestMethod]
        public void Test_IntervalSet_Comparison()
        {
            var setA = new Int32IntervalSet(1, 2, 3, 6, 7, 8, 12, 14, 16);
            var setB = new Int32IntervalSet(1, 2, 3, 6, 7, 8, 12, 14, 16);

            Assert.IsTrue(setA == setB);
            Assert.IsTrue(setA.Equals(setB));
            Assert.IsFalse(setA != setB);

            var setC = new Int32IntervalSet();
            Assert.IsFalse(setA == setC);
            Assert.IsTrue(setA != setC);

            var setD = new Int32IntervalSet(1, 2, 3, 6, 7, 8, 12, 14, 17);
            Assert.IsFalse(setA == setD);
            Assert.IsTrue(setA != setD);
        }



        [TestMethod]
        public void Test_IntervalSet_Copy()
        {
            int[] contents = { 1, 2, 3, 7, 8, 13, 14, 15, 18, 24, 25, 26 };
            var intSet = new Int32IntervalSet(contents);

            var copy = intSet.Copy();

            for (int i = -3; i <= 50; i++)
            {
                bool inSet = intSet.Includes(i);
                Assert.AreEqual(intSet.Includes(i), copy.Includes(i), (" at i = " + i));
            }
        }



        [TestMethod]
        public void Test_IntervalSet_Ctor()
        {
            var intSet = new Int32IntervalSet();

            Assert.IsTrue(intSet.IsEmpty);
            Assert.IsFalse(intSet.IsUniversal);
            Assert.IsFalse(intSet.IsNegativeInfinite);
            Assert.IsFalse(intSet.IsPositiveInfinite);

            Assert.IsFalse(intSet.Includes(0));
            Assert.IsFalse(intSet.Includes(10));
            Assert.IsFalse(intSet.Includes(-1));
            
            int[] contents = { 1, 2, 3, 7, 8, 13, 14, 15, 18, 24,25,26 };
            intSet = new Int32IntervalSet(contents);

            for (int i = -3; i <= 50; i++)
            {
                bool inContents = Array.FindIndex(contents, c => c == i) >= 0;
                bool inSet = intSet.Includes(i);
                Assert.AreEqual(inContents, inSet, (" at i = " + i));
            }
            
        }

        

        [TestMethod]
        public void Test_IntervalSet_And()
        {
            var intSetA = new Int32IntervalSet(new int[] { 1, 2, 3, 7, 8, 13, 14, 15, 18, 24, 25, 26 });
            var intSetB = new Int32IntervalSet(new int[] { 0,3,4,5,12,13,14,25,26,27});
            var copy1 = intSetA.Copy();
            copy1.IntersectWith(intSetB);
            for (int i = -3; i <= 50; i++)
            {
                Assert.IsTrue((intSetA.Includes(i) && intSetB.Includes(i)) == copy1.Includes(i), (" at i = " + i));
            }

            intSetA = new Int32IntervalSet(1, 2, 3, 7, 8, 13, 14, 15, 18, 24, 25, 26 );
            intSetB = new Int32IntervalSet(0, 3, 4, 5, 12, 13, 14, 25, 26, 27 );
            var copy2 = intSetB.Copy();
            copy2.IntersectWith(intSetA);
            for (int i = -3; i <= 50; i++)
            {
                Assert.IsTrue((intSetA.Includes(i) && intSetB.Includes(i)) == copy2.Includes(i), (" at i = " + i));
            }
        }


        [TestMethod]
        public void Test_IntervalSet_Not()
        {
            var orig = new Int32IntervalSet(1, 2, 3, 7, 8, 13, 14, 15, 18, 24, 25, 26, 28, 29, 30, 32, 34);
            var negSet = orig.Copy();
            negSet.Negate();
            for (int i = -3; i <= 50; i++)
            {
                Assert.IsTrue(orig.Includes(i) != negSet.Includes(i), (" at i = " + i));
            }
        }



        [TestMethod]
        public void Test_IntervalSet_Or()
        {
            var intSetA = new Int32IntervalSet(new int[] { 1, 2, 3, 7, 8, 13, 14, 15, 18, 24, 25, 26 });
            var intSetB = new Int32IntervalSet(new int[] { 0, 3, 4, 5, 12, 13, 14, 25, 26, 27 });
            var copy1 = intSetA.Copy();
            copy1.UnionWith(intSetB);
            for (int i = -3; i <= 50; i++)
            {
                Assert.IsTrue((intSetA.Includes(i) || intSetB.Includes(i)) == copy1.Includes(i), (" at i = " + i));
            }

            intSetA = new Int32IntervalSet(1, 2, 3, 7, 8, 13, 14, 15, 18, 24, 25, 26);
            intSetB = new Int32IntervalSet(0, 3, 4, 5, 12, 13, 14, 25, 26, 27);
            var copy2 = intSetB.Copy();
            copy2.UnionWith(intSetA);
            for (int i = -3; i <= 50; i++)
            {
                Assert.IsTrue((intSetA.Includes(i) || intSetB.Includes(i)) == copy2.Includes(i), (" at i = " + i));
            }

            var emptySet = new Int32IntervalSet();
            var universalSet = new Int32IntervalSet();
            universalSet.MakeUniversal();

            for (int i = -3; i <= 50; i++)
            {
                Assert.IsTrue(!emptySet.Includes(i), (" at i = " + i));
                Assert.IsTrue(universalSet.Includes(i), (" at i = " + i));
            }

            var copy3 = intSetA.Copy();
            copy3.UnionWith(emptySet);
            for (int i = -3; i <= 50; i++)
            {
                Assert.IsTrue(copy3.Includes(i) == intSetA.Includes(i), (" at i = " + i));
            }
            copy3.UnionWith(universalSet);
            for (int i = -3; i <= 50; i++)
            {
                Assert.IsTrue(copy3.Includes(i), (" at i = " + i));
            }
        }



        [TestMethod]
        public void Test_IntervalSet_ToString()
        {
            int[] contents = { 1, 2, 3, 7, 8, 13, 14, 15, 18, 24, 25, 26 };
            var intSet = new Int32IntervalSet(contents);
            Assert.AreEqual("1..3,7..8,13..15,18,24..26", intSet.ToString());

            intSet = new Int32IntervalSet(17);
            Assert.AreEqual("17", intSet.ToString());

            intSet.MakeEmpty();
            Assert.AreEqual("..", intSet.ToString());

            intSet.MakeUniversal();
            Assert.AreEqual("<..>", intSet.ToString());

            var orig = new Int32IntervalSet(1, 2, 3, 7, 8, 13, 14, 15, 18, 24, 25, 26, 28, 29, 30, 32, 34);
            var negSet = orig.Copy();
            negSet.Negate();
            Assert.AreEqual("1..3,7..8,13..15,18,24..26,28..30,32,34", orig.ToString());
            Assert.AreEqual("<..0,4..6,9..12,16..17,19..23,27,31,33,35..>", negSet.ToString());
        }


    }
}
