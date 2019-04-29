using System;
using DataStructures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UnitTests
{
    [TestClass]
    public class Test_IntervalSet
    {
        private static Random _Rng = new Random(0);
        private static List<int> _GetRandomInt32s(int size = 1000)
        {
            List<int> list = new List<int>();
            for (int i = 0; i < size; i++) if (_Rng.Next(2) == 0) list.Add(i);
            return list;
        }


        

        [TestMethod]
        public void Test_IntervalSet_And()
        {
            var intSetA = new Int32IntervalSet(new int[] { 1, 2, 3, 7, 8, 13, 14, 15, 18, 24, 25, 26 });
            var intSetB = new Int32IntervalSet(new int[] { 0,3,4,5,12,13,14,25,26,27});
            var copy1 = intSetA & intSetB;
            for (int i = -3; i <= 50; i++)
            {
                bool inBoth = intSetA.Contains(i) && intSetB.Contains(i);
                Assert.IsTrue(inBoth == copy1.Contains(i), (" at i = " + i));
            }

            intSetA = new Int32IntervalSet(1, 2, 3, 7, 8, 13, 14, 15, 18, 24, 25, 26 );
            intSetB = new Int32IntervalSet(0, 3, 4, 5, 12, 13, 14, 25, 26, 27 );
            var copy2 = intSetB & intSetA;
            for (int i = -3; i <= 50; i++)
            {
                Assert.IsTrue((intSetA.Contains(i) && intSetB.Contains(i)) == copy2.Contains(i), (" at i = " + i));
            }


            int listSize = 1000;
            Random rng = new Random(0);
            Int32IntervalSet a, b, oper;
            a = new Int32IntervalSet(_GetRandomInt32s(listSize));
            b = new Int32IntervalSet(_GetRandomInt32s(listSize));
            oper = a & b;
            for (int i = -10; i < listSize + 10; i++)
            {
                bool inOrig = a.Contains(i) && b.Contains(i);
                bool inOper = oper.Contains(i);
                Assert.AreEqual(inOper, inOrig, "For item " + i + ", inOrig=" + inOrig.ToString() + ", inOper=" + inOper.ToString());
            }


            oper = a & ~b;
            for (int i = -10; i < listSize + 10; i++)
            {
                bool inOrig = a.Contains(i) && !b.Contains(i);
                bool inOper = oper.Contains(i);
                Assert.AreEqual(inOper, inOrig, "For item " + i + ", inOrig=" + inOrig.ToString() + ", inOper=" + inOper.ToString());
            }

            oper = ~a & b;
            for (int i = -10; i < listSize + 10; i++)
            {
                bool inOrig = !a.Contains(i) && b.Contains(i);
                bool inOper = oper.Contains(i);
                Assert.AreEqual(inOper, inOrig, "For item " + i + ", inOrig=" + inOrig.ToString() + ", inOper=" + inOper.ToString());
            }

            oper = ~a & ~b;
            for (int i = -10; i < listSize + 10; i++)
            {
                bool inOrig = !a.Contains(i) && !b.Contains(i);
                bool inOper = oper.Contains(i);
                Assert.AreEqual(inOper, inOrig, "For item " + i + ", inOrig=" + inOrig.ToString() + ", inOper=" + inOper.ToString());
            }
        }



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
                bool inSet = intSet.Contains(i);
                Assert.AreEqual(intSet.Contains(i), copy.Contains(i), (" at i = " + i));
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

            Assert.IsFalse(intSet.Contains(0));
            Assert.IsFalse(intSet.Contains(10));
            Assert.IsFalse(intSet.Contains(-1));

            int[] contents = { 1, 2, 3, 7, 8, 13, 14, 15, 18, 24, 25, 26 };
            intSet = new Int32IntervalSet(contents);

            for (int i = -3; i <= 50; i++)
            {
                bool inContents = Array.FindIndex(contents, c => c == i) >= 0;
                bool inSet = intSet.Contains(i);
                Assert.AreEqual(inContents, inSet, (" at i = " + i));
            }
            
        }

        [TestMethod]
        [ExcludeFromCodeCoverage]
        public void Test_IntervalSet_Iteration()
        {
            List<int> list = _GetRandomInt32s(1000);
            
            Int32IntervalSet set = new Int32IntervalSet(list);

            int idx = 0;
            foreach (int item in set)
            {
                int inList = list[idx++];
                Assert.AreEqual(inList, item);
            }


            set.MakeEmpty();
            foreach (int item in set)
            {
                Assert.Fail("There should be no items in the set.");
            }

            set = new Int32IntervalSet(7);
            idx = 0;
            foreach (int item in set)
            {
                Assert.AreEqual(7, item); idx++;
            }
            Assert.AreEqual(idx, 1);

            set.MakeNegativeInfinite();
            try
            {
                foreach (int item in set)
                    Assert.Fail();
            } catch (InvalidOperationException)
            {
            }

            set.MakeUniversal();
            Assert.IsTrue(set.IsUniversal);
            try
            {
                foreach (int item in set)
                    Assert.Fail();
            }
            catch (InvalidOperationException)
            {
            }

            set = new Int32IntervalSet(15);
            set.MakePositiveInfinite();
            idx = 15;
            foreach (int item in set)
            {
                Assert.AreEqual(idx++, item);
                if (idx > 30) break;
            }


        }


        [TestMethod]
        public void Test_IntervalSet_MakeX()
        {
            Int32IntervalSet set = new Int32IntervalSet((IEnumerable<int>)null);
            Assert.IsTrue(set.IsEmpty);
            set = new Int32IntervalSet(new int[0]);
            Assert.IsTrue(set.IsEmpty);
            Assert.IsFalse(set.IsUniversal);

            set = new Int32IntervalSet(1, 10);
            Assert.IsFalse(set.IsEmpty);
            Assert.IsFalse(set.IsUniversal);
            Assert.IsFalse(set.Contains(11));
            Assert.IsTrue(set.Contains(10));
            Assert.IsFalse(set.Contains(9));
            set.MakePositiveInfinite();
            Assert.IsTrue(set.Contains(10));
            Assert.IsFalse(set.Contains(9));
            Assert.IsTrue(set.Contains(11));

            Assert.IsFalse(set.Contains(0));
            Assert.IsTrue(set.Contains(1));
            Assert.IsFalse(set.Contains(2));
            set.MakeNegativeInfinite();
            Assert.IsTrue(set.Contains(0));
            Assert.IsTrue(set.Contains(1));
            Assert.IsFalse(set.Contains(2));

            set.MakeEmpty();
            Assert.IsTrue(set.IsEmpty);
            Assert.IsFalse(set.IsUniversal);
            Assert.IsFalse(set.Contains(0));
            Assert.IsFalse(set.Contains(1));
            Assert.IsFalse(set.Contains(2));
            Assert.IsFalse(set.Contains(9));
            Assert.IsFalse(set.Contains(10));
            Assert.IsFalse(set.Contains(11));

            set.MakeUniversal();
            Assert.IsFalse(set.IsEmpty);
            Assert.IsTrue(set.IsUniversal);
            Assert.IsTrue(set.Contains(0));
            Assert.IsTrue(set.Contains(1));
            Assert.IsTrue(set.Contains(2));
            Assert.IsTrue(set.Contains(9));
            Assert.IsTrue(set.Contains(10));
            Assert.IsTrue(set.Contains(11));

            IEnumerable<int> ienum = new int[] { 1, 3, 5, 7 };
            set = new Int32IntervalSet(ienum);
            Assert.IsFalse(set.IsEmpty);

            set.MakeEmpty();
            Assert.IsTrue(set.IsEmpty);
            set.MakePositiveInfinite();
            Assert.IsTrue(set.IsUniversal);

            set.MakeEmpty();
            Assert.IsTrue(set.IsEmpty);
            set.MakeNegativeInfinite();
            Assert.IsTrue(set.IsUniversal);
        }



        [TestMethod]
        public void Test_IntervalSet_Not()
        {
            var orig = new Int32IntervalSet(1, 2, 3, 7, 8, 13, 14, 15, 18, 24, 25, 26, 28, 29, 30, 32, 34);
            var negSet = !orig;
            for (int i = -3; i <= 50; i++)
            {
                Assert.IsTrue(orig.Contains(i) != negSet.Contains(i), (" at i = " + i));
            }
            Assert.IsTrue(negSet.IsPositiveInfinite);
            Assert.IsTrue(negSet.IsNegativeInfinite);
            Assert.IsFalse(negSet.IsEmpty);
            Assert.IsFalse(negSet.IsUniversal);

            var negNegSet = !(!orig);
            Assert.IsTrue(negNegSet == orig);
            for (int i = -3; i <= 50; i++)
            {
                Assert.IsTrue(orig.Contains(i) == negNegSet.Contains(i), (" at i = " + i ));
            }

            orig = new Int32IntervalSet();
            Assert.IsTrue(orig.IsEmpty);
            negSet = !orig;
            Assert.IsTrue(negSet.IsUniversal);
            negNegSet = !negSet;
            Assert.IsTrue(negNegSet.IsEmpty);

        }



        [TestMethod]
        public void Test_IntervalSet_Or()
        {
            var intSetA = new Int32IntervalSet(new int[] { 1, 2, 3, 7, 8, 13, 14, 15, 18, 24, 25, 26 });
            var intSetB = new Int32IntervalSet(new int[] { 0, 3, 4, 5, 12, 13, 14, 25, 26, 27 });
            var copy1 = intSetA | intSetB;
            for (int i = -3; i <= 50; i++)
            {
                bool includedInOne = intSetA.Contains(i) || intSetB.Contains(i);
                Assert.IsTrue(includedInOne == copy1.Contains(i), (" at i = " + i));
            }

            intSetA = new Int32IntervalSet(1, 2, 3, 7, 8, 13, 14, 15, 18, 24, 25, 26);
            intSetB = new Int32IntervalSet(0, 3, 4, 5, 12, 13, 14, 25, 26, 27);
            var copy2 = intSetB | intSetA;
            for (int i = -3; i <= 50; i++)
            {
                Assert.IsTrue((intSetA.Contains(i) || intSetB.Contains(i)) == copy2.Contains(i), (" at i = " + i));
            }

            var emptySet = new Int32IntervalSet();
            var universalSet = new Int32IntervalSet();
            universalSet.MakeUniversal();

            for (int i = -3; i <= 50; i++)
            {
                Assert.IsTrue(!emptySet.Contains(i), (" at i = " + i));
                Assert.IsTrue(universalSet.Contains(i), (" at i = " + i));
            }

            var copy3 = intSetA | emptySet;
            for (int i = -3; i <= 50; i++)
            {
                Assert.IsTrue(copy3.Contains(i) == intSetA.Contains(i), (" at i = " + i));
            }
            copy3 |= universalSet;
            for (int i = -3; i <= 50; i++)
            {
                Assert.IsTrue(copy3.Contains(i), (" at i = " + i));
            }

            int listSize = 1000;
            Random rng = new Random(0);
            Int32IntervalSet a, b, oper;
            SortedSet<int> list = new SortedSet<int>();
            a = new Int32IntervalSet(_GetRandomInt32s(listSize));
            b = new Int32IntervalSet(_GetRandomInt32s(listSize));
            oper = a | b;
            for (int i = -10; i < listSize + 10; i++)
            {
                bool inOrig = a.Contains(i) || b.Contains(i);
                bool inOper = oper.Contains(i);
                Assert.AreEqual(inOper, inOrig, "For item " + i + ", inOrig=" + inOrig.ToString() + ", inOper=" + inOper.ToString());
            }


            oper = a | ~b;
            for (int i = -10; i < listSize+10; i++)
            {
                bool inOrig = a.Contains(i) || !b.Contains(i);
                bool inOper = oper.Contains(i);
                Assert.AreEqual(inOper, inOrig, "For item " + i + ", inOrig=" + inOrig.ToString() + ", inOper=" + inOper.ToString());
            }

            oper = ~a | b;
            for (int i = -10; i < listSize + 10; i++)
            {
                bool inOrig = !a.Contains(i) || b.Contains(i);
                bool inOper = oper.Contains(i);
                Assert.AreEqual(inOper, inOrig, "For item " + i + ", inOrig=" + inOrig.ToString() + ", inOper=" + inOper.ToString());
            }

            oper = ~a | ~b;
            for (int i = -10; i < listSize + 10; i++)
            {
                bool inOrig = !a.Contains(i) || !b.Contains(i);
                bool inOper = oper.Contains(i);
                Assert.AreEqual(inOper, inOrig, "For item " + i + ", inOrig=" + inOrig.ToString() + ", inOper=" + inOper.ToString());
            }
        }


        [TestMethod]
        public void Test_IntervalSet_Subtract()
        {

            int listSize = 1000;
            Random rng = new Random(0);
            Int32IntervalSet a, b, oper;
            SortedSet<int> list = new SortedSet<int>();
            a = new Int32IntervalSet(_GetRandomInt32s(listSize));
            b = new Int32IntervalSet(_GetRandomInt32s(listSize));
            oper = a - b;
            for (int i = -10; i < listSize + 10; i++)
            {
                bool inOrig = a.Contains(i) && !b.Contains(i);
                bool inOper = oper.Contains(i);
                Assert.AreEqual(inOper, inOrig, "For item " + i + ", inOrig=" + inOrig.ToString() + ", inOper=" + inOper.ToString());
            }

            var notB = ~b;
            oper = a - ~b;
            for (int i = -10; i < listSize + 10; i++)
            {
                bool inOrig = a.Contains(i) && b.Contains(i);
                bool inOper = oper.Contains(i);
                Assert.AreEqual(inOper, inOrig, "For item " + i + ", inOrig=" + inOrig.ToString() + ", inOper=" + inOper.ToString());
            }

            oper = ~a - b;
            for (int i = -10; i < listSize + 10; i++)
            {
                bool inOrig = !a.Contains(i) && !b.Contains(i);
                bool inOper = oper.Contains(i);
                Assert.AreEqual(inOper, inOrig, "For item " + i + ", inOrig=" + inOrig.ToString() + ", inOper=" + inOper.ToString());
            }

            oper = ~a - ~b;
            for (int i = -10; i < listSize + 10; i++)
            {
                bool inOrig = !a.Contains(i) && b.Contains(i);
                bool inOper = oper.Contains(i);
                Assert.AreEqual(inOper, inOrig, "For item " + i + ", inOrig=" + inOrig.ToString() + ", inOper=" + inOper.ToString());
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
            var negSet = !orig;
            Assert.AreEqual("1..3,7..8,13..15,18,24..26,28..30,32,34", orig.ToString());
            Assert.AreEqual("<..0,4..6,9..12,16..17,19..23,27,31,33,35..>", negSet.ToString());
        }


        [TestMethod]
        public void Test_IntervalSet_Xor()
        {

            int listSize = 1000;
            Random rng = new Random(0);
            Int32IntervalSet a, b, oper;
            SortedSet<int> list = new SortedSet<int>();
            a = new Int32IntervalSet(_GetRandomInt32s(listSize));
            b = new Int32IntervalSet(_GetRandomInt32s(listSize));
            oper = a ^ b;
            for (int i = -10; i < listSize + 10; i++)
            {
                bool inOrig = a.Contains(i) ^ b.Contains(i);
                bool inOper = oper.Contains(i);
                Assert.AreEqual(inOper, inOrig, "For item " + i + ", inOrig=" + inOrig.ToString() + ", inOper=" + inOper.ToString());
            }


            oper = a ^ ~b;
            for (int i = -10; i < listSize + 10; i++)
            {
                bool inOrig = a.Contains(i) ^ !b.Contains(i);
                bool inOper = oper.Contains(i);
                Assert.AreEqual(inOper, inOrig, "For item " + i + ", inOrig=" + inOrig.ToString() + ", inOper=" + inOper.ToString());
            }

            oper = ~a ^ b;
            for (int i = -10; i < listSize + 10; i++)
            {
                bool inOrig = !a.Contains(i) ^ b.Contains(i);
                bool inOper = oper.Contains(i);
                Assert.AreEqual(inOper, inOrig, "For item " + i + ", inOrig=" + inOrig.ToString() + ", inOper=" + inOper.ToString());
            }

            oper = ~a ^ ~b;
            for (int i = -10; i < listSize + 10; i++)
            {
                bool inOrig = !a.Contains(i) ^ !b.Contains(i);
                bool inOper = oper.Contains(i);
                Assert.AreEqual(inOper, inOrig, "For item " + i + ", inOrig=" + inOrig.ToString() + ", inOper=" + inOper.ToString());
            }
        }

    }
}
