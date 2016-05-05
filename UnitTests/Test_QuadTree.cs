using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;

namespace UnitTests
{
    [TestClass]
    public class Test_QuadTree
    {

        DataStructures.QuadTree<Rect> qt0;

        [TestInitialize]
        public void Test_QuadTree_Initialize()
        {
            qt0 = new DataStructures.QuadTree<Rect>((r) => r);            
        }

        [TestMethod]
        public void Test_QuadTree_Contents()
        {
            qt0.Add(new Rect(0, 0, 1, 1));
            Assert.AreEqual(new Rect(0, 0, 1, 1), qt0.Extent);

            qt0.Clear();
            Assert.IsTrue(qt0.Extent.IsEmpty);

            qt0.Add(new Rect(0, 0, 0.5, 0.5));
            qt0.Add(new Rect(0.5, 0.5, 0.5, 0.5));
            Assert.AreEqual(new Rect(0, 0, 1, 1), qt0.Extent);
            qt0.Remove(new Rect(0.5, 0.5, 0.5, 0.5));
            Assert.AreEqual(new Rect(0, 0, 0.5, 0.5), qt0.Extent);
            qt0.Remove(new Rect(0, 0, 0.5, 0.5));
            Assert.IsTrue(qt0.Extent.IsEmpty);
        }

        [TestMethod]
        public void Test_QuadTree_Retrieval()
        {
            qt0.Add(new Rect(0, 0, 1, 1));
            for  (int i = 0; i<50; i++)            
                qt0.Add(new Rect(0, 0, 0.5 + (i/1000.0), 0.5 + (1/1000.0)));    //The +i/1000.0 is to avoid adding the same rect multiple times.
            qt0.Add(new Rect(0.25, 0.25, 0.1, 0.1));

            var containedBy = qt0.GetContainedBy(new Rect(0, 0, 1, 1));
            Assert.AreEqual(52, containedBy.Count);

            var contain = qt0.GetContainers(new Rect(0.6, 0.6, 0.1, 0.1));
            Assert.AreEqual(1, contain.Count);

            var intersect = qt0.GetIntersection(new Rect(1, 1, 0, 0));
            Assert.AreEqual(1, intersect.Count);
            intersect = qt0.GetIntersection(new Rect(0, 0, 1, 1));
            Assert.AreEqual(52, intersect.Count);
            intersect = qt0.GetIntersection(new Rect(2, 2, 1, 1));
            Assert.AreEqual(0, intersect.Count);
        }
    }
}
