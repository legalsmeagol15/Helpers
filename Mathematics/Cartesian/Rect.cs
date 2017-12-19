using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics.Cartesian
{
    public struct Rect
    {
        /// <summary>The top (meaning highest y-value) of the Rect. </summary>
        public double Top { get; private set; }
        /// <summary>The bottom (meaning lowest y-value) of the Rect. </summary>
        public double Bottom { get; private set; }
        /// <summary>The left (meaning lowest x-value) of the Rect. </summary>
        public double Left { get; private set; }
        /// <summary>The right (meaning highest x-value) of the Rect. </summary>
        public double Right { get; private set; }
        public Rect(double left, double right, double top, double bottom) { Left = left;  Right = right;  Top = top; Bottom = bottom; }
        public Rect(Point a, Point b)
        {
            Left = (a.X <= b.X) ? a.X : b.X;
            Right = (a.X >= b.X) ? a.X : b.X;
            Top = (a.Y >= b.Y) ? a.Y : b.Y;
            Bottom = (a.Y <= b.Y) ? a.Y : b.Y;            
        }
        public static Rect Empty { get { return new Rect(double.PositiveInfinity, double.NegativeInfinity, double.NegativeInfinity, double.PositiveInfinity); } }


        public Point BottomLeft { get { return new Point(Left, Bottom); } }
        public Point BottomRight { get { return new Point(Right, Bottom); } }
        public bool Contains(Point p) { return (p.X >= Left) && (p.X <= Right) && (p.Y >= Bottom) && (p.Y <= Top); }
        public bool Contains(Rect other) { return other.Left >= Left && other.Right <= Right && other.Top <= Top && other.Bottom >= Bottom; }
        public double Height { get { return Math.Max(0.0d, Top - Bottom); } }
        public bool IntersectsWith(Rect other) { throw new NotImplementedException(); }
        public bool IsEmpty { get { return Top < Bottom || Right < Left; } }
        public Point TopLeft { get { return new Point(Left, Top); } }
        public Point TopRight { get { return new Point(Right, Top); } }        
        public double Width { get { return Math.Max(0.0d, Right - Left); } }
        public double X { get { return Left; } }
        public double Y { get { return Right; } }
       
        

        public static Rect Union(Rect a, Rect b)
        {
            if (a.IsEmpty) return new Rect(b.Left, b.Right, b.Top, b.Bottom);
            if (b.IsEmpty) return new Rect(a.Left, a.Right, a.Top, a.Bottom);

            double left = (a.Left <= b.Left) ? a.Left : b.Left;
            double right = (a.Right >= b.Right) ? a.Right : b.Right;
            double top = (a.Top >= b.Top) ? a.Top : b.Top;
            double bottom = (a.Bottom <= b.Bottom) ? a.Bottom : b.Bottom;
            return new Rect(left, right, top, bottom);
        }


        public static bool operator ==(Rect a, Rect b) { return a.Left == b.Left && a.Right == b.Right && a.Top == b.Top && a.Bottom == b.Bottom; }
        public static bool operator !=(Rect a, Rect b) { return !(a == b); }

        public override bool Equals(object obj) { return (obj is Rect) ? this == (Rect)obj : false; }
        public override int GetHashCode() { return Top.GetHashCode() + Bottom.GetHashCode() + Right.GetHashCode() + Left.GetHashCode(); }
    }
}
