using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Windows;
using System.Collections.Generic;

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
        public void Test_QuadTree_DeepRetrieval()
        {
            qt0.Add(new Rect(0.0, 0.0, 100.0, 100.0));
            for (double y = 0.0; y < 100.0; y += 1.0)
            {
                for (double x = 0.0; x < 100.0; x += 1.0)
                {
                    Rect newRect = new Rect(x, y, 1.0, 1.0);
                    qt0.Add(newRect, newRect);
                }
            }
            Assert.AreEqual(10001, qt0.Count);

            IEnumerable<Rect> searchSetA = qt0.GetIntersection(new Rect(0.5, 0.5, 1.0, 1.0));
            Assert.AreEqual(searchSetA.Count(), 5);
            IEnumerable<Rect> searchSetB = qt0.GetIntersection(new Rect(0, 0, 100, 100));
            Assert.AreEqual(searchSetB.Count(), 10001);
            IEnumerable<Rect> searchSetC = qt0.GetIntersection(new Rect(50.1, 50.1, 49.8, 49.8));
            Assert.AreEqual(searchSetC.Count(), 2501);
        }

        [TestMethod]
        public void Test_QuadTree_Misc()
        {
            //Test BoundsOf
            qt0.Add(new Rect(0.0, 0.0, 1.0, 1.0));
            qt0.Add(new Rect(1.0, 1.0, 100.0, 100.0));
            Assert.AreEqual(qt0.BoundsOf(new Rect(0.0, 0.0, 1.0, 1.0)), new Rect(0.0, 0.0, 1.0, 1.0));
            try
            {
                qt0.BoundsOf(new Rect(-1.0, -1.0, 1.0, 1.0));
                Assert.Fail();
            }
            catch { }

            //Test ICollection.Count
            Assert.AreEqual(2, ((ICollection<Rect>)qt0).Count());

            //Test Contains
            Assert.IsTrue(qt0.Contains(new Rect(1.0, 1.0, 100.0, 100.0)));
            Assert.IsFalse(qt0.Contains(new Rect(2.0, 2.0, 1.0, 1.0)));

            //Test ICollection.IsReadOnly
            Assert.IsFalse(((ICollection<Rect>)qt0).IsReadOnly);

            //Test IEnumerator.
            ((ICollection<Rect>)qt0).Add(new Rect(0.0, 0.0, 1.0, 1.0));  //A dupe of the first one.
            int c=0, d=0;
            foreach (Rect item in qt0) c++;
            foreach (object o in (ICollection<Rect>)qt0) d++;
            Assert.AreEqual(3, qt0.Count);
            Assert.AreEqual(2, c);
            Assert.AreEqual(2, d);

        }

        [TestMethod]
        public void Test_QuadTree_Retrieval()
        {
            Assert.AreEqual(1, qt0.Add(new Rect(0, 0, 1, 1)));
            Assert.AreEqual(1, qt0.Add(new Rect(0.2, 0.2, 0.1, 0.1)));
            Assert.AreEqual(2, qt0.Add(new Rect(0, 0, 1, 1)));
            
            Assert.AreEqual(0, qt0.GetIntersection(new Rect(-1, -1, .1, .1)).Count());
            Assert.AreEqual(2, qt0.GetIntersection(new Rect(0, 0, 1, 1)).Count());
            Assert.AreEqual(2, qt0.GetIntersection(new Rect(0, 0, 0.2, 0.2)).Count());
            Assert.AreEqual(1, qt0.GetIntersection(new Rect(0.5, 0.5, 1, 1)).Count());

            //Now remove them and test the intersection again == 0.
            Assert.IsTrue(qt0.Remove(new Rect(0, 0, 1, 1)));
            Assert.IsTrue(qt0.Remove(new Rect(0.2, 0.2, 0.1, 0.1)));
            Assert.IsTrue(qt0.Remove(new Rect(0, 0, 1, 1)));
            Assert.IsFalse(qt0.Remove(new Rect(0.2, 0.2, 0.1, 0.1)));
            Assert.IsFalse(qt0.Remove(new Rect(0, 0, 1, 1)));

            Assert.AreEqual(0, qt0.GetIntersection(new Rect(-1, -1, .1, .1)).Count());
            Assert.AreEqual(0, qt0.GetIntersection(new Rect(0, 0, 1, 1)).Count());
            Assert.AreEqual(0, qt0.GetIntersection(new Rect(0, 0, 0.2, 0.2)).Count());
            Assert.AreEqual(0, qt0.GetIntersection(new Rect(0.5, 0.5, 1, 1)).Count());

            //Now add and clear and test the intersections again.
            Assert.AreEqual(1, qt0.Add(new Rect(0, 0, 1, 1)));
            Assert.AreEqual(1, qt0.Add(new Rect(0.2, 0.2, 0.1, 0.1)));
            Assert.AreEqual(2, qt0.Add(new Rect(0, 0, 1, 1)));
            qt0.Clear();
            Assert.AreEqual(0, qt0.GetIntersection(new Rect(-1, -1, .1, .1)).Count());
            Assert.AreEqual(0, qt0.GetIntersection(new Rect(0, 0, 1, 1)).Count());
            Assert.AreEqual(0, qt0.GetIntersection(new Rect(0, 0, 0.2, 0.2)).Count());
            Assert.AreEqual(0, qt0.GetIntersection(new Rect(0.5, 0.5, 1, 1)).Count());
        }

        [TestMethod]
        public void Test_QuadTree_Structure()
        {
            qt0.Add(new Rect(0.0, 0.0, 100.0, 100.0));
            Assert.AreEqual(1, qt0.Count);
            for (double y = 0.0; y< 100.0; y+=1.0)
            {
                for (double x = 0.0; x<100.0; x += 1.0)
                {
                    Rect newRect = new Rect(x, y, 1.0, 1.0);
                    qt0.Add(newRect, newRect);
                    qt0.Add(newRect, newRect);
                    Assert.AreEqual(2, qt0.CountOf(newRect));
                }
            }
            Assert.AreEqual(20001, qt0.Count);
        }

    }
}
