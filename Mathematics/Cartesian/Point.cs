using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics.Cartesian
{
    public struct Point
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        public Point(double x, double y) { X = x;  Y = y; }

        public static Point operator +(Point p, Vector v) { return new Point(p.X + v.X, p.Y + v.Y); }
        public static Point operator -(Point p, Vector v) { return new Point(p.X - v.X, p.Y - v.Y); }
        public static Vector operator -(Point destination, Point origin) { return new Vector(destination.X - origin.X, destination.Y - origin.Y); }
    }
}
