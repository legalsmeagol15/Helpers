using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mathematics.Calculus;
using System.Numerics;

namespace UnitTests
{
    [TestClass]
    public class Test_Polynomial
    {
        [TestMethod]
        public void Test_Polynomial_GetRoots()
        {

            //LINEAR
            Complex[] lineRoots0 = Polynomial.GetRoots(2.5, 3.5);
            Assert.AreEqual(1, lineRoots0.Length);
            Assert.AreEqual(lineRoots0[0], -1.4);

            Complex[] lineRoots1 = Polynomial.GetRoots(-4, 8);
            Assert.AreEqual(1, lineRoots0.Length);
            Assert.AreEqual(lineRoots1[0], 2);

            //QUADRATIC
            //Test all real roots.
            Complex[] quadraticRoots0 = Polynomial.GetRoots(2, -4, -6);
            Assert.AreEqual(2, quadraticRoots0.Length);
            Assert.AreEqual(quadraticRoots0[0], -1);
            Assert.AreEqual(quadraticRoots0[1], 3);

            //Test imaginary roots.
            Complex[] quadraticRoots1 = Polynomial.GetRoots(2, -4, 6);
            Assert.AreEqual(2, quadraticRoots1.Length);
            Assert.IsTrue(AreClose(quadraticRoots1[0], new Complex(1, -Math.Sqrt(2))));
            Assert.IsTrue(AreClose(quadraticRoots1[1], new Complex(1, Math.Sqrt(2))));

            //Test multi-root
            Complex[] quadraticRoots2 = Polynomial.GetRoots(4, 0, 0);
            Assert.AreEqual(2, quadraticRoots2.Length);
            Assert.AreEqual(quadraticRoots2[0], quadraticRoots2[1]);
            Assert.AreEqual(0, quadraticRoots2[0]);

            //CUBIC
            //Test all real roots
            Complex[] cubicRoots0 = Polynomial.GetRoots(3, -27, 15, 60);
            Assert.AreEqual(1, cubicRoots0.Count((c) => AreClose(c, -1.1775)));
            Assert.AreEqual(1, cubicRoots0.Count((c) => AreClose(c, 2.1036)));
            Assert.AreEqual(1, cubicRoots0.Count((c) => AreClose(c, 8.0739)));

            //Test with 1 real and 2 imaginary roots
            Complex[] cubicRoots1 = Polynomial.GetRoots(5, -24, 10, 75);
            Assert.AreEqual(1, cubicRoots1.Count((c) => AreClose(c, -1.4022)));
            Assert.AreEqual(1, cubicRoots1.Count((c) => AreClose(c, new Complex(3.1011, 1.0393))));
            Assert.AreEqual(1, cubicRoots1.Count((c) => AreClose(c, new Complex(3.1011, -1.0393))));            
        }


        private static bool AreClose(double a, double b)
        {
            return Math.Abs(a - b) < 0.0001;
        }
        private static bool AreClose(Complex a, Complex b)
        {
            return AreClose(a.Real, b.Real) && AreClose(a.Imaginary, b.Imaginary);
        }

        [TestMethod]
        public void Test_Polynomial_ToString()
        {
            Polynomial p1a = Polynomial.FromConstant(10.0);
            Assert.AreEqual("10", p1a.ToString());

            Polynomial p1b = Polynomial.FromConstant(-10.5);
            Assert.AreEqual("-10.5", p1b.ToString());

            Polynomial p1c = Polynomial.FromConstant(0.0);
            Assert.AreEqual("0", p1c.ToString());

            Polynomial p2a = Polynomial.FromLinear(5.5,  1.0);
            Assert.AreEqual("5.5x + 1", p2a.ToString());

            Polynomial P2b = Polynomial.FromLinear(-5.5, -1.0);
            Assert.AreEqual("-5.5x - 1", P2b.ToString());

            Polynomial P2c = Polynomial.FromLinear(1.0, 0.0);
            Assert.AreEqual("x", P2c.ToString());

            Polynomial P2d = Polynomial.FromLinear(1.0, 2.0);
            Assert.AreEqual("x + 2", P2d.ToString());

            Polynomial p2e = Polynomial.FromLinear(1.0, -2.5);
            Assert.AreEqual("x - 2.5", p2e.ToString());

            Polynomial p3a = Polynomial.FromQuadratic(1.0, 1.0, 1.0);
            Assert.AreEqual("x^2 + x + 1", p3a.ToString());

            Polynomial p3b = Polynomial.FromQuadratic(-1.0, -1.0, 0.0);
            Assert.AreEqual("-x^2 - x", p3b.ToString());

            Polynomial p3c = Polynomial.FromQuadratic(5.0, 0.0, 0.0);
            Assert.AreEqual("5x^2", p3c.ToString());

            Polynomial p3d = Polynomial.FromQuadratic(-3.0, 0.0, 10.0);
            Assert.AreEqual("-3x^2 + 10", p3d.ToString());

            Polynomial p3e = Polynomial.FromQuadratic(0.0, 1.0, 2.0);
            Assert.AreEqual("x + 2", p3e.ToString());

            Polynomial p3f = Polynomial.FromQuadratic(0.0, 0.0, 10.0);
            Assert.AreEqual("10", p3f.ToString());

            Polynomial p3g = Polynomial.FromQuadratic(0.0, 0.0, 0.0);
            Assert.AreEqual("0", p3g.ToString());

            Polynomial p4a = Polynomial.FromCubic(1.0, 1.0, 1.0,  1.0);
            Assert.AreEqual("x^3 + x^2 + x + 1", p4a.ToString());

            Polynomial p4b = Polynomial.FromCubic(-1.0, 0.0, 0.0, 0.0);
            Assert.AreEqual("-x^3", p4b.ToString());

            Polynomial p4c = Polynomial.FromCubic(5.5, -1.0, -1.0, 10.0);
            Assert.AreEqual("5.5x^3 - x^2 - x + 10", p4c.ToString());

            Polynomial p4d = Polynomial.FromCubic(1.0, 1.0, 1.0, 0.0);
            Assert.AreEqual("x^3 + x^2 + x", p4d.ToString());

            Polynomial p4e = Polynomial.FromCubic(-5.5, 0.0, 0.0, 0.0);
            Assert.AreEqual("-5.5x^3", p4e.ToString());
        }
    }
}
