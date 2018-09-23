using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Windows;

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


        public static double GetDistance(Point a, Point b) => GetDistance(a.X, a.Y, b.X, b.Y);

        public static double GetDistance(double x0, double y0, double x1, double y1)
        {
            double a = (x0 - x1);
            double b = (y0 - y1);
            return Math.Sqrt((a * a) + (b * b));
        }

        // The main function that returns true if line segment 'p1q1'
        // and 'p2q2' intersect.
        public static bool GetDoIntersect(Point p1, Point q1, Point p2, Point q2)
        {
            // Find the four orientations needed for general and
            // special cases
            int o1 = orientation(p1, q1, p2);
            int o2 = orientation(p1, q1, q2);
            int o3 = orientation(p2, q2, p1);
            int o4 = orientation(p2, q2, q1);

            // General case
            if (o1 != o2 && o3 != o4)
                return true;

            // Special Cases
            // p1, q1 and p2 are colinear and p2 lies on segment p1q1
            if (o1 == 0 && onSegment(p1, p2, q1)) return true;

            // p1, q1 and q2 are colinear and q2 lies on segment p1q1
            if (o2 == 0 && onSegment(p1, q2, q1)) return true;

            // p2, q2 and p1 are colinear and p1 lies on segment p2q2
            if (o3 == 0 && onSegment(p2, p1, q2)) return true;

            // p2, q2 and q1 are colinear and q1 lies on segment p2q2
            if (o4 == 0 && onSegment(p2, q1, q2)) return true;

            return false; // Doesn't fall in any of the above cases

            int orientation(Point p, Point q, Point r)
            {
                // See https://www.geeksforgeeks.org/orientation-3-ordered-points/
                // for details of below formula.
                double val = (q.Y - p.Y) * (r.X - q.X) -
                          (q.X - p.X) * (r.Y - q.Y);
                if (val == 0) return 0;  // colinear
                return Math.Sign(val);
                //return (val > 0) ? 1 : 2; // clock or counterclock wise
            }

            // Given three colinear points p, q, r, the function checks if
            // point q lies on line segment 'pr'
            bool onSegment(Point p, Point q, Point r)
            {
                if (q.X <= Math.Max(p.X, r.X) && q.X >= Math.Min(p.X, r.X) &&
                    q.Y <= Math.Max(p.Y, r.Y) && q.Y >= Math.Min(p.Y, r.Y))
                    return true;

                return false;
            }
        }


        public static int NCR(int n, int r)
        {
            int result = 1;
            for (int i = n; i > r; i--) result *= i;

            int divisor = 1;
            for (int i = n - r; i > 0; i--) divisor *= i;

            return result / divisor;  
        }

        internal static decimal Sqrt(decimal v)
        {
            throw new NotImplementedException();
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

        private static List<int> _Fibs = new List<int>() { 1, 1, 2, 3, 5, 8, 13, 21 };
        public static int Fibonacci(int n)
        {            
            while (_Fibs.Count <= n)
            {
                int i = _Fibs.Count;
                _Fibs.Add(_Fibs[i - 1] + _Fibs[i - 2]);
            }
            return _Fibs[n];
        }
        
    }
}
