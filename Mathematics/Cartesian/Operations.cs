using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics.Cartesian
{
    public static class Operations
    {
        public static double GetDistance(Point a, Point b) { double xDiff = a.X - b.X, yDiff = a.Y - b.Y; return Math.Sqrt((xDiff * xDiff) + (yDiff * yDiff)); }
    }
}
