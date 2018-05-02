using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using DataStructures;
using System.Linq;
using System.Diagnostics;

namespace UnitTests
{

   

    [TestClass]
    public class Test_SkipList
    {
        private List<int> randomIntA = new List<int>();
        //private List<int> randomIntB = new List<int>();

        [TestInitialize]
        public void SkipList_Initialize()
        {
            Random rnd = new Random(10);            
            for (int i = 0; i < 200; i++)
                randomIntA.Add(rnd.Next(0, 100));
            //for (int i = 0; i < 200; i++)
            //    randomIntB.Add(rnd.Next(int.MinValue, int.MaxValue));

        }

        [TestMethod]
        public void SkipList_Constructors()
        {
            List<int> newList = new List<int>();
            newList.Add(1);
            newList.Add(10);
            newList.Add(100);
            newList.Add(50);
            newList.Add(-1);
            newList.Add(0);

            SkipList<int> skipA = new SkipList<int>(newList);
            Assert.IsTrue(skipA.Count == 6);
            Assert.IsTrue(skipA.First() == -1);
            Assert.IsTrue(skipA.Last() == 100);

            foreach (int i in newList)
                Assert.IsFalse(skipA.Add(i));


            SkipList<int> skipB = new SkipList<int>();
            foreach (int i in newList)
                Assert.IsTrue(skipB.Add(i));

            
            SkipList<int> skipC = new SkipList<int>(randomIntA);
            for (int i = 0; i < 200; i++)
                Assert.IsTrue(skipC.Contains(randomIntA[i]));
            
        }

        [TestMethod]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public void SkipList_Constructor_AutoComparators()
        {
            //This method excluded from code coverage analysis due to assert.fails that are not reached.
            SkipList<TestJunk> skipJunk = new SkipList<TestJunk>();
            
            try
            {
                SkipList<System.Windows.Point> skipA = new SkipList<System.Windows.Point>();
                Assert.Fail();
            }
            catch(Exception ex)
            {
                if (ex is AssertFailedException) Assert.Fail();
            }
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        private struct TestJunk : IComparable
        {
            //This class is only used to illustrate the IComparable interface constructor.
            public TestJunk(int value) { this.Value = value; }
            int Value;

            public int CompareTo(object obj)
            {
                return ((TestJunk)obj).Value.CompareTo(Value);
            }
        }

        [TestMethod]
        public void SkipList_Contents_Modification()
        {
            Random rnd = new Random(10);
            List<int> rndList = new List<int>();
            for (int i = 0; i < 200; i++)
                rndList.Add(rnd.Next(int.MinValue, int.MaxValue));
            SkipList<int> skipA = new SkipList<int>(rndList);
            for (int i = 0; i < 200; i++)
                Assert.IsTrue(skipA.Contains(rndList[i]));

            for (int i = 0; i < 200; i++)
                Assert.IsTrue(skipA.Remove(rndList[i]));

            for (int i = 0; i < 200; i++)
                Assert.IsFalse(skipA.Remove(rndList[i]));


            for (int i = 0; i < 200; i++) skipA.Add(rndList[i]);
            skipA.Clear();
            for (int i = 0; i < 200; i++)
                Assert.IsFalse(skipA.Contains(rndList[i]));

            skipA.Add(10);
            Assert.IsFalse(skipA.Remove(0));
            Assert.IsFalse(skipA.Remove(11));

            skipA.Clear();
            Assert.IsFalse(skipA.Remove(0));

        }

        [TestMethod]
        public void SkipList_Contents_Adjacencies()
        {
            Random rnd = new Random(10);
            List<int> rndList = new List<int>();
            for (int i = 0; i < 200; i++)
                rndList.Add(rnd.Next(int.MinValue, int.MaxValue));

            SkipList<int> skipA = new SkipList<int>(Comparer<int>.Default, 1.0/3.0);
            for (int i = 0; i < 200; i++) skipA.Add(rndList[i]);
            for (int i = 0; i < 200; i++)
                Assert.IsTrue(skipA.Contains(rndList[i]));
        }

        [TestMethod]
        public void SkipList_RemoveMin()
        {
            SkipList<int> skipA = new SkipList<int>(randomIntA);
            int uniqueCount = randomIntA.Distinct().Count();
            int min = randomIntA.Min();

            Assert.IsTrue(skipA.Count == uniqueCount);            
            Assert.IsTrue(skipA.Min == min);
            Assert.IsTrue(skipA.Contains(min));
            Assert.IsTrue(skipA.RemoveMin()==0);
            Assert.IsFalse(skipA.Min == min);
            Assert.IsFalse(skipA.Contains(min));
            Assert.IsFalse(skipA.Remove(min));            
            Assert.IsTrue(skipA.Count == uniqueCount - 1);

            SkipList<int> skipB = new SkipList<int>();
            Assert.IsFalse(skipB.RemoveMin() != 0);
        }

        [TestMethod]
        public void SkipList_RemoveMax()
        {
            SkipList<int> skipA = new SkipList<int>(randomIntA);
            int uniqueCount = randomIntA.Distinct().Count();
            int max = randomIntA.Max();
            Assert.IsTrue(skipA.Count == uniqueCount);
            Assert.IsTrue(skipA.Max == max);
            Assert.IsTrue(skipA.Contains(max));
            Assert.IsTrue(skipA.RemoveMax()==max);
            Assert.IsFalse(skipA.Max == max);
            Assert.IsFalse(skipA.Contains(max));
            Assert.IsFalse(skipA.Remove(max));
            Assert.IsTrue(skipA.Count == uniqueCount - 1);

            SkipList<int> skipB = new SkipList<int>();
            Assert.IsFalse(skipB.RemoveMax() != default(int));

        }

        [TestMethod]
        public void SkipList_TryGetBefore()
        {
            SkipList<int> skipA = new SkipList<int>(randomIntA);
            int result;
            Assert.IsFalse(skipA.TryGetBefore(-1, out result));
            Assert.IsTrue(result == 0);
            Assert.IsFalse(skipA.TryGetBefore(0, out result));
            Assert.IsTrue(result == 0);

            int min = randomIntA.Min();
            Assert.IsFalse(skipA.TryGetBefore(min, out result));
            Assert.IsTrue(result == 0);

            randomIntA.Sort();
            randomIntA = randomIntA.Distinct().ToList();
            Assert.IsTrue(skipA.TryGetBefore(randomIntA[1], out result));
            Assert.IsTrue(result == randomIntA[0]);
            Assert.IsTrue(skipA.TryGetBefore(2, out result));
            Assert.IsTrue(result == 1);

            for (int i = 1; i < randomIntA.Count; i++)
            {
                Assert.IsTrue(skipA.TryGetBefore(randomIntA[i], out result));
                Assert.IsTrue(result == randomIntA[i - 1]);
            }
            

            SkipList<int> skipEmpty = new SkipList<int>();
            Assert.IsFalse(skipEmpty.TryGetBefore(1, out result));
            Assert.IsFalse(skipEmpty.TryGetBefore(0, out result));
            Assert.IsFalse(skipEmpty.TryGetBefore(-1, out result));
        }

        [TestMethod]
        public void SkipList_TryGetAfter()
        {
            SkipList<int> skipA = new SkipList<int>(randomIntA);
            int result;
            Assert.IsFalse(skipA.TryGetAfter(100, out result));
            Assert.IsTrue(result == 0);
            Assert.IsFalse(skipA.TryGetAfter(99, out result));
            Assert.IsTrue(result == 0);

            int max = randomIntA.Max();
            Assert.IsFalse(skipA.TryGetAfter(max, out result));
            Assert.IsTrue(result == 0);

            randomIntA.Sort();
            randomIntA = randomIntA.Distinct().ToList();
            Assert.IsTrue(skipA.TryGetAfter(randomIntA[randomIntA.Count - 2], out result));
            Assert.IsTrue(result == randomIntA.Last());
            Assert.IsTrue(result == max);

            for (int i = 0; i < randomIntA.Count-1; i++)
            {
                Assert.IsTrue(skipA.TryGetAfter(randomIntA[i], out result));
                Assert.IsTrue(result == randomIntA[i + 1]);
            }

            SkipList<int> skipEmpty = new SkipList<int>();
            Assert.IsFalse(skipEmpty.TryGetAfter(1, out result));
            Assert.IsFalse(skipEmpty.TryGetAfter(0, out result));
            Assert.IsFalse(skipEmpty.TryGetAfter(-1, out result));

            
        }

        [TestMethod]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        public void SkipList_AdjacencyFraction()
        {

            //This method excludes from coverage cuz some Assert.Fail() methods should never be reached by code analysis.

            SkipList<int> skipA = new SkipList<int>();


            skipA.AdjacencyFraction = (1.0d / 3.0d);
            randomIntA = randomIntA.Distinct().ToList();
            foreach (int i in randomIntA)            
                Assert.IsTrue(skipA.Add(i));
            Assert.IsTrue(skipA.Count == randomIntA.Count);
            foreach (int  i in randomIntA)            
                Assert.IsTrue(skipA.Remove(i));
            Assert.IsTrue(skipA.Count == 0);

            skipA.AdjacencyFraction = 0.5;
            try
            {
                //cannot be 0.0 or less.
                skipA.AdjacencyFraction = 0.0;
                Assert.Fail();            
            }
            catch (Exception ex)
            {
                if (ex is AssertFailedException) Assert.Fail();
            }

            try
            {
                //Cannot be over 1.0
                skipA.AdjacencyFraction = 1.01;
                Assert.Fail();
            }
            catch (Exception ex)
            {
                if (ex is AssertFailedException) Assert.Fail();
            }

            try
            {
                //Cannot be a number impossible to fractionalize as a double and then invert.
                skipA.AdjacencyFraction = (1.0d / double.MaxValue);
                Assert.Fail();
            }
            catch (Exception ex)
            {
                if (ex is AssertFailedException) Assert.Fail();
            }

            Assert.IsTrue(skipA.AdjacencyFraction == 0.5);
        }
    }
}
