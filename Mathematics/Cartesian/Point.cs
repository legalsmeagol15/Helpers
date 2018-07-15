using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Mathematics.Cartesian
{
    public class Point : AbstractShape
    {
        public readonly double X, Y;
        public Point(double x, double y) { this.X = x; this.Y = y; }

        public override Rect ToRect() => new Rect(X, Y, 0, 0);
    }
}
