using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Mathematics
{
    public static class Operations
    {

      


        /// <summary>
        /// Returns the simple, real cube root of the given double.
        /// </summary>
        public static double CubeRt(double d)
        {
            return Math.Pow(Math.Abs(d), (1.0 / 3.0)) * Math.Sign(d);            
        }

        /// <summary>
        /// Returns the square root of the given decimal, by using the Babylonian method.
        /// </summary>
        /// <param name="d"></param>
        /// <remarks>Based on code by Bobson, posted, 11/8/12, at http://stackoverflow.com/questions/4124189/performing-math-operations-on-decimal-datatype-in-c .  Retrieved 6/1/16.</remarks>
        public static decimal Sqrt(decimal d)
        {
            decimal guess = d / 2m, result, avg;
            while (true)
            {                
                result = d / guess;
                avg = (guess + result) / 2m;
                if (avg == guess) return avg;
                guess = avg;
            }            
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
                double newAngle = (CommonValues.PiTimes2 * (double)m / n) + phaseInterval;
                double newReal = r * Math.Cos(newAngle);
                double newImaginary = r * Math.Sin(newAngle);
                result[m] = new Complex(newReal, newImaginary);
            }            
            return result;
        }

        public static int NCR(int n, int r)
        {
            int result = 1;
            for (int i = n; i > r; i--) result *= i;

            int divisor = 1;
            for (int i = n - r; i > 0; i--) divisor *= i;

            return result / divisor;  
        }

        public static int NPR(int n, int r)
        {
            int num = 1;
            for (int i = n; i > 0; i--) num *= i;

            int div = 1;
            for (int i = r; i > 0; i--) div *= i;

            return num / div;
        }

        /// <summary>
        /// Returns the factorial of the given number.  Note than numbers higher than 12 will overflow a 32-bit int.
        /// </summary> 
        public static int Factorial (int n)
        {
            if (n < 0) throw new ArgumentException("Cannot find the factorial of a negative number.");
            for (int i = n;  i>=1; i--)
            {
                n *= i;
            }
            return n;
        }

        
    }
}
