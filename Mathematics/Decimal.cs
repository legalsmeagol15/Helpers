using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics
{
    public static class Decimal
    {

        public static decimal Atan2(decimal y, decimal x)
        {
            return (decimal)Math.Atan2((double)y, (double)x);
        }

        public const decimal PI = 3.14159265358979323846m;

        public static decimal Sign(decimal a) { return (a >= 0.0m) ? 1 : -1; }
        

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



    }
}
