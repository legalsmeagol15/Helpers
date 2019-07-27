using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics
{
    public static class Int64
    {
        /// <summary>
        /// Returns the square root of the given long (System.Int64).
        /// </summary>
        /// <remarks>From http://www.codecodex.com/wiki/Calculate_an_integer_square_root#C.23 .</remarks>
        public static long Sqrt(long num)
        {
            if (0 == num) { return 0; }  // Avoid zero divide  
            long n = (num / 2) + 1;       // Initial estimate, never low  
            long n1 = (n + (num / n)) / 2;
            while (n1 < n)
            {
                n = n1;
                n1 = (n + (num / n)) / 2;
            }
            return n;
        }

        public static long Pow(long number, int exponent)
        {
            if (exponent == 0) return 1;
            long result = number;
            for (int i = 2; i <= exponent; i++)
            {
                result *= number;
                if (result < 0) throw new OverflowException("Int64 " + number + " to the power " + exponent + " exceeds available bits for an Int64.");
            }
            return result;
        }

        public static int Log_2(int number)
        {
            if (number < 1) throw new ArgumentException("The log of a number less than 1 is undefined.");

            // Uses a binary search to find the log_2 of the number.
            int result = 0;
            int step = 32;
            while (step > 0)
            {
                int stepped = number >> step;
                if (stepped != 0) { result += step; number = stepped; }
                step >>= 1;
            }
            return result;
        }
    }
}
