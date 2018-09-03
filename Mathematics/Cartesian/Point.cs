using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Mathematics.Cartesian
{
    public class Point : IAbstractShape
    {
        public readonly double X, Y;
        public Point(double x, double y) { this.X = x; this.Y = y; }

        public Rect GetBounds() => new Rect(X, Y, 0, 0);
    }
}
