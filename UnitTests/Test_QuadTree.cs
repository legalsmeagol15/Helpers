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
            
        }
    }
}
