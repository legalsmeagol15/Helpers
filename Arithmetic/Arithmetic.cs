using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Arithmetic
{
    public static class Arithmetic
    {

        internal const double SqRt3 = 1.7320508075688772935;
        internal const double OneThird = 0.33333333333333333333;
        internal const double PiTimes2 = Math.PI * 2d;


        /// <summary>
        /// Returns the simple, real cube root of the given double.
        /// </summary>
        public static double CubeRt(double d)
        {
            return Math.Pow(Math.Abs(d), (1.0 / 3.0)) * Math.Sign(d);            
        }

        /// <summary>
        /// Returns the roots of the given complex number.
        /// </summary>
        /// <param name="c">The value whose roots are sought.</param>
        /// <param name="n">The order of roots to return.  For example, providing '2' will return the square roots, providing '3' will return the cubic roots, etc.</param>        
        public static Complex[] NthRoots(this Complex c, int n)
        {
            double r = Math.Pow(c.Magnitude, ((double)1/ n));

            Complex[] result = new Complex[n];
            double phaseInterval = c.Phase / n;            
            for (int m = 0; m<n; m++)
            {
                double newAngle = (PiTimes2 * (double)m / n) + phaseInterval;
                double newReal = r * Math.Cos(newAngle);
                double newImaginary = r * Math.Sin(newAngle);
                result[m] = new Complex(newReal, newImaginary);
            }            
            return result;
        }
    }
}
