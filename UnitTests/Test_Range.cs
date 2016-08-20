using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DataStructures;

namespace UnitTests
{
    [TestClass]
    public class Test_Range
    {
        Range5<int> focus, distantPrecedes, precedes, distantFollows, follows, containedByFocus, containsFocus, lapsBefore, lapsAfter, 
                   lapsBeforeEdge, lapsAfterEdge;

        [TestInitialize]
        public void Setup()
        {
            focus = new Range5<int>(10, 15);
            distantPrecedes = new Range5<int>(1, 3);
            precedes = new Range5<int>(1, 9);
            distantFollows = new Range5<int>(20, 25);
            follows = new Range5<int>(16, 20);
            containedByFocus = new Range5<int>(11, 14);
            containsFocus = new Range5<int>(9, 16);
            lapsBefore = new Range5<int>(5, 12);
            lapsAfter = new Range5<int>(14, 20);
            lapsBeforeEdge = new Range5<int>(1, 10);
            lapsAfterEdge = new Range5<int>(15, 20);
        }

        [TestMethod]
        public void Range_Statuses()
        {
            Range5<int> test = new Range5<int>(-10, 10);
            Assert.IsFalse(test.IsEmpty);
            Assert.IsFalse(test.IsInfinity);
            Assert.IsFalse(test.IsSingleton);

            test = Range5<int>.NewSingleton(5);
            Assert.IsTrue(test.IsSingleton);
            Assert.IsFalse(test.IsInfinity);
            Assert.IsFalse(test.IsEmpty);

            test = Range5<int>.NewInfinity();
            Assert.IsFalse(test.IsEmpty);
            Assert.IsTrue(test.IsInfinity);
            Assert.IsFalse(test.IsSingleton);

            test = Range5<int>.NewEmpty();
            Assert.IsTrue(test.IsEmpty);
            Assert.IsFalse(test.IsInfinity);
            Assert.IsFalse(test.IsSingleton);
        }

        [TestMethod]
        public void Range_Equality0()
        {
            Range5<int> emptyA = Range5<int>.NewEmpty();
            Range5<int> emptyB = Range5<int>.NewEmpty();
            Assert.IsTrue(emptyA == emptyB);

            Range5<int> infiniteA = Range5<int>.NewInfinity();
            Range5<int> infiniteB = Range5<int>.NewInfinity();
            Assert.IsTrue(infiniteA == infiniteB);
            Assert.IsFalse(infiniteA == emptyA);

            Range5<int> singleA = Range5<int>.NewSingleton(5);
            Range5<int> singleB = Range5<int>.NewSingleton(5);
            Assert.IsTrue(singleA == singleB);
            Assert.IsFalse(emptyA == singleA);
            Assert.IsFalse(infiniteA == singleA);
            Range5<int> singleC = Range5<int>.NewSingleton(6);
            Assert.IsFalse(singleA == singleC);

            Range5<int> test = new Range5<int>(10, 15);
            Assert.IsFalse(test == emptyA);
            Assert.IsFalse(test == infiniteA);
            Assert.IsFalse(test == singleA);
            Assert.IsFalse(test == singleC);
        }

        [TestMethod]
        public void Range_Equality1()
        {
            Range5<int> a = new Range5<int>(10, 15);
            Range5<int> b = new Range5<int>(5, 10);
            Range5<int> c = new Range5<int>(15, 20);
            Range5<int> d = new Range5<int>(10, 20);
            Range5<int> e = new Range5<int>(5, 15);
            Range5<int> f = new Range5<int>(11, 14);
            Range5<int> g = new Range5<int>(12, 13);
            Range5<int> h = new Range5<int>(9, 16);
            Range5<int> i = new Range5<int>(10, 15);

            i.ToString();

            Assert.IsFalse(a == b);
            Assert.IsFalse(a == c);
            Assert.IsFalse(a == d);
            Assert.IsFalse(a == e);
            Assert.IsFalse(a == f);
            Assert.IsFalse(a == g);
            Assert.IsFalse(a == h);
            Assert.IsTrue(a == i);
            Assert.IsTrue(a.Equals(a));
            Assert.IsFalse(a.Equals(b));
            Assert.IsFalse(a.Equals(c));
            Assert.IsFalse(a.Equals(d));
            Assert.IsFalse(a.Equals(e));
            Assert.IsFalse(a.Equals(f));
            Assert.IsFalse(a.Equals(g));
            Assert.IsFalse(a.Equals(h));            
            Assert.IsTrue(a.Equals(i));

            Assert.IsTrue(a != b);
            Assert.IsTrue(a != c);
            Assert.IsTrue(a != d);
            Assert.IsTrue(a != e);
            Assert.IsTrue(a != f);
            Assert.IsTrue(a != g);
            Assert.IsTrue(a != h);
            Assert.IsFalse(a != i);

            
        }



        [TestMethod]
        public void Range_And_Simple_Finite_Inclusive()
        {
            throw new NotImplementedException();
            //Range3<int> test = focus & precedes;
            //Assert.IsTrue(test.IsEmpty);

            //test = focus & distantPrecedes;
            //Assert.IsTrue(test.IsEmpty);

            //test = focus & follows;
            //Assert.IsTrue(test.IsEmpty);

            //test = focus & distantFollows;
            //Assert.IsTrue(test.IsEmpty);

            //test = focus & containedByFocus;
            //Assert.IsTrue(test.Equals(containedByFocus));

            //test = focus & containsFocus;
            //Assert.IsTrue(test.Equals(focus));

            //test = lapsBefore & focus;
            //Assert.IsFalse(test.Contains(9));
            //Assert.IsTrue(test.Contains(10));
            //Assert.IsTrue(test.Contains(11));
            //Assert.IsTrue(test.Contains(12));
            //Assert.IsFalse(test.Contains(13));

            //test = lapsAfter & focus;
            //Assert.IsFalse(test.Contains(13));
            //Assert.IsTrue(test.Contains(14));
            //Assert.IsTrue(test.Contains(15));
            //Assert.IsFalse(test.Contains(16));

            //test = lapsBeforeEdge & focus;
            //Assert.IsFalse(test.Contains(9));
            //Assert.IsTrue(test.Contains(10));
            //Assert.IsFalse(test.Contains(11));
            //Assert.IsTrue(test.IsSingleton);

            //test = lapsAfterEdge & focus;
            //Assert.IsFalse(test.Contains(14));
            //Assert.IsTrue(test.Contains(15));
            //Assert.IsFalse(test.Contains(16));
            //Assert.IsTrue(test.IsSingleton);
        }

        [TestMethod]
        public void Range_Or_Singleton_Simple()
        {


            Range5<int> test = focus | 0;
            Assert.IsFalse(test.Equals(focus));
            Assert.IsFalse(test.Contains(-1));
            Assert.IsTrue(test.Contains(0));
            for (int i = 1; i <= 9; i++) Assert.IsFalse(test.Contains(i));
            for (int i = 10; i <= 15; i++) Assert.IsTrue(test.Contains(i));
            Assert.IsFalse(test.Contains(16));

            test = focus | 9;
            Assert.IsFalse(test.Equals(focus));
            Assert.IsFalse(test.Contains(8));
            for (int i = 9; i < 15; i++) Assert.IsTrue(test.Contains(i));
            Assert.IsFalse(test.Contains(16));

            test = focus | 10;
            Assert.IsTrue(test.Equals(focus));

            test = focus | 12;
            Assert.IsTrue(test.Equals(focus));

            test = focus | 15;
            Assert.IsTrue(test.Equals(focus));

            test = focus | 16;
            Assert.IsFalse(test.Equals(focus));
            Assert.IsFalse(test.Contains(9));
            for (int i = 10; i < 16; i++) Assert.IsTrue(test.Contains(i));
            Assert.IsFalse(test.Contains(17));

            test = focus | 20;
            Assert.IsFalse(test.Equals(focus));
            Assert.IsFalse(test.Contains(9));
            for (int i = 10; i <= 15; i++) Assert.IsTrue(test.Contains(i));
            for (int i = 16; i <= 19; i++) Assert.IsFalse(test.Contains(i));
            Assert.IsTrue(test.Contains(20));
            Assert.IsFalse(test.Contains(21));

        }

        [TestMethod]
        public void Range_Or_Finite_Inclusive()
        {
            Range5<int> test;

            focus.ToString();
            test = focus | precedes;
            
            for (int i = 1; i <= 15; i++)
                Assert.IsTrue(test.Contains(i));
            Assert.IsFalse(test.Contains(0));
            Assert.IsFalse(test.Contains(16));

            test = focus | distantPrecedes;
            for (int i = 1; i <= 3; i++) Assert.IsTrue(test.Contains(i));
            for (int i = 4; i <= 9; i++) Assert.IsFalse(test.Contains(i));
            for (int i = 10; i <= 15; i++) Assert.IsTrue(test.Contains(i));
            Assert.IsFalse(test.Contains(0));
            Assert.IsFalse(test.Contains(16));

            test = focus | follows;
            for (int i = 10; i <= 20; i++) Assert.IsTrue(test.Contains(i));
            Assert.IsFalse(test.Contains(9));
            Assert.IsFalse(test.Contains(21));

            test = focus | distantFollows;
            for (int i = 10; i <= 15; i++) Assert.IsTrue(test.Contains(i));
            for (int i = 16; i <= 19; i++) Assert.IsFalse(test.Contains(i));
            for (int i = 20; i <= 25; i++) Assert.IsTrue(test.Contains(i));

            test = focus | containedByFocus;
            Assert.IsTrue(test.Equals(focus));

            test = focus | containsFocus;
            Assert.IsTrue(test.Equals(containsFocus));

            test = lapsBefore | focus;
            for (int i = 5; i < 15; i++) Assert.IsTrue(test.Contains(i));
            Assert.IsFalse(test.Contains(4));
            Assert.IsFalse(test.Contains(16));
            

            test = lapsAfter | focus;
            for (int i = 10; i < 20; i++) Assert.IsTrue(test.Contains(i));
            Assert.IsFalse(test.Contains(9));
            Assert.IsFalse(test.Contains(21));

            test = lapsBeforeEdge | focus;
            for (int i = 1; i <= 15; i++) Assert.IsTrue(test.Contains(i));
            Assert.IsFalse(test.Contains(0));
            Assert.IsFalse(test.Contains(16));
            
            test = lapsAfterEdge | focus;
            for (int i = 10; i <= 20; i++) Assert.IsTrue(test.Contains(i));
            Assert.IsFalse(test.Contains(9));
            Assert.IsFalse(test.Contains(21));
            
        }

        [TestMethod]
        public void Range_Subtract_Singleton_Simple()
        {
            throw new NotImplementedException();
            //Range3<int> test = null;

            //test = focus - 9;
            //Assert.IsTrue(test.Equals(focus));

            //test = focus - 16;
            //Assert.IsTrue(test.Equals(focus));

            //test = focus - 10;
            //Assert.IsFalse(test.Equals(focus));
            //Assert.IsFalse(test.Contains(10));
            //for (int i = 11; i <= 15; i++) Assert.IsTrue(test.Contains(i));
            //Assert.IsFalse(test.Contains(16));

            //test = focus - 15;
            //Assert.IsFalse(test.Equals(focus));
            //Assert.IsTrue(test.Contains(10));
            //for (int i = 10; i <= 14; i++) Assert.IsTrue(test.Contains(i));
            //Assert.IsFalse(test.Contains(15));

            ////Now, for something a little more complex.
            //Range3<int> complexA = new Range3<int>(51, 55);
            //complexA |= new Range3<int>(57, 60);
            //complexA |= new Range3<int>(65, 100);
            //complexA |= new Range3<int>(0, 40);
            //Assert.IsFalse(complexA.Contains(-1));
            //for (int i = 0; i <= 40; i++) Assert.IsTrue(complexA.Contains(i));
            //for (int i = 51; i <= 55; i++) Assert.IsTrue(complexA.Contains(i));
            //for (int i = 57; i <= 60; i++) Assert.IsTrue(complexA.Contains(i));
            //for (int i = 65; i <= 100; i++) Assert.IsTrue(complexA.Contains(i));
            //for (int e = 41; e <= 50; e++) Assert.IsFalse(complexA.Contains(e));
            //for (int e = 56; e <= 56; e++) Assert.IsFalse(complexA.Contains(e));
            //for (int e = 61; e <= 64; e++) Assert.IsFalse(complexA.Contains(e));
            //Assert.IsFalse(complexA.Contains(101));

            //Range3<int> complexB = new Range3<int>(0, 100);
            //complexB -= new Range3<int>(41, 50);
            //complexB -= new Range3<int>(61, 64);
            //complexB -= 56;
            //Assert.IsFalse(complexB.Contains(-1));
            //for (int i = 0; i <= 40; i++) Assert.IsTrue(complexB.Contains(i));
            //for (int i = 51; i <= 55; i++) Assert.IsTrue(complexB.Contains(i));
            //for (int i = 57; i <= 60; i++) Assert.IsTrue(complexB.Contains(i));
            //for (int i = 65; i <= 100; i++) Assert.IsTrue(complexB.Contains(i));
            //for (int e = 41; e <= 50; e++) Assert.IsFalse(complexB.Contains(e));
            //for (int e = 56; e <= 56; e++) Assert.IsFalse(complexB.Contains(e));
            //for (int e = 61; e <= 64; e++) Assert.IsFalse(complexB.Contains(e));
            //Assert.IsFalse(complexB.Contains(101));

            //Assert.IsTrue(complexA.Equals(complexB));
            //Assert.IsTrue(complexA == complexB);

        }
    }
}
