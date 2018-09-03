using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Mathematics.Cartesian
{
    public sealed class Line : IAbstractShape
    {
        public static readonly Line Empty = new Line(double.NaN, double.NaN, double.NaN, double.NaN);

        public readonly double X0, Y0, X1, Y1;
        public Line(double x0, double y0, double x1, double y1) { this.X0 = x0; this.Y0 = y0; this.X1 = x1; this.Y1 = y1; }

        public bool IsEmpty() => double.IsNaN(X0) || double.IsNaN(X1) || double.IsNaN(Y0) || double.IsNaN(Y1);

        public double GetLength() => Mathematics.Operations.GetDistance(X0, Y0, X1, Y1);



        public System.Windows.Point GetNearest(System.Windows.Point pt, bool clamp, out double t, out double distance)
        {
            double xDiff = (X1 - X0), yDiff = (Y1 - Y0), l2 = ((xDiff * xDiff) + (yDiff * yDiff));
            System.Windows.Point v = new System.Windows.Point(X0, Y0), w = new System.Windows.Point(X1, Y1);
            if (l2 == 0.0) { t = 0; distance = Operations.GetDistance(pt, v); return pt; }
            t = System.Windows.Vector.Multiply(pt - v, new System.Windows.Vector(xDiff, yDiff)) / l2;
            if (clamp) { if (t < 0) t = 0; else if (t > 1) t = 1; }
            System.Windows.Point result = (System.Windows.Point)(v + ((w - v) * t));
            distance = Operations.GetDistance(pt, result);
            return result;
        }

        /// <summary>Returns whether any part of this line exists in the given range.</summary>
        public bool GetIntersects(Rect range)
        {
            System.Windows.Point a = new System.Windows.Point(X0, Y0), b = new System.Windows.Point(X1, Y1);
            if (range.Contains(a) || range.Contains(b)) return true;
            if (Operations.GetDoIntersect(a, b, range.TopLeft, range.TopRight)) return true;
            if (Operations.GetDoIntersect(a, b, range.BottomRight, range.TopRight)) return true;
            if (Operations.GetDoIntersect(a, b, range.BottomRight, range.BottomLeft)) return true;
            if (Operations.GetDoIntersect(a, b, range.TopLeft, range.BottomLeft)) return true;
            return false;
        }

        /// <summary>Returns whether this line intersects with the line segment described by the given two points.</summary>
        public bool GetIntersects(System.Windows.Point otherPointA, System.Windows.Point otherPointB)
            => Operations.GetDoIntersect(new System.Windows.Point(X0, Y0), new System.Windows.Point(X1, Y1), otherPointA, otherPointB);


        public Rect GetBounds() => new Rect(new System.Windows.Point(X0, Y0), new System.Windows.Point(X1, Y1));
    }


}
