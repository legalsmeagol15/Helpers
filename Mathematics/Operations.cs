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
