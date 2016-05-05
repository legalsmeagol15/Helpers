using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Arithmetic
{
    public static class Geometry
    {
        /// <summary>
        /// Returns the distance between two two-dimensional points.
        /// </summary>
        public static double GetDistance(Point a, Point b)
        {
            double xDiff = a.X - b.X;
            double yDiff = a.Y - b.Y;
            return Math.Sqrt((xDiff * xDiff) + (yDiff * yDiff));
        }
    }
}
