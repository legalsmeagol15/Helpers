using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics.Geometry
{
    public interface IRect<T> where T : struct
    {
        T Left { get; }
        T Right { get; }
        T Top { get; }
        T Bottom { get; }

        bool Contains(IRect<T> other);
        bool Overlaps(IRect<T> other);
        bool IsEmpty { get; }
    }

    public struct RectD : IRect<double>
    {
        public static readonly RectD Empty = new RectD(double.NaN, double.NaN, double.NaN, double.NaN);
        public static readonly RectD Universal = new RectD(double.NegativeInfinity, double.PositiveInfinity, double.NegativeInfinity, double.PositiveInfinity);

        public readonly double Left, Right, Top, Bottom;

        double IRect<double>.Left => Left;

        double IRect<double>.Right => Right;

        double IRect<double>.Top => Top;

        double IRect<double>.Bottom => Bottom;

        public double Width => Right - Left;

        public double Height => Top - Bottom;

        public double Area => Height * Width;

        public bool IsEmpty => double.IsNaN(Top) || double.IsNaN(Bottom) || double.IsNaN(Right) || double.IsNaN(Left);

        public bool IsPoint => Left == Right && Top == Bottom;

        public bool IsLineSegment => (Left == Right) ^ (Top == Bottom);

        public bool IsHorizontalInfinite => double.IsNegativeInfinity(Left) || double.IsPositiveInfinity(Right);

        public bool IsVerticalInfinite => double.IsNegativeInfinity(Bottom) || double.IsPositiveInfinity(Top);
        
        public RectD (double left, double right, double bottom,  double top)
        {
            this.Left = left; this.Right = right; this.Top = top; this.Bottom = bottom;
        }

        public bool Contains(IRect<double> other)
        {
            if (IsEmpty || other.IsEmpty) return false;
            if (other.Left < Left) return false;
            if (other.Right > Right) return false;
            if (other.Bottom < Bottom) return false;
            if (other.Top > Top) return false;
            return true;
        }
        public bool Overlaps(IRect<double> other)
        {
            if (this.IsEmpty || other.IsEmpty) return false;
            if (Right < other.Left || Left > other.Right) return false;
            if (Top < other.Bottom || Bottom > other.Top) return false;
            return true;
        }

        public RectD GetIntersection(IRect<double> other)
        {
            if (!Overlaps(other)) return Empty;
            double[] horizontal = { Left, Right, other.Left, other.Right };
            double[] vertical = { Top, Bottom, other.Top, other.Bottom };
            Array.Sort(horizontal);
            Array.Sort(vertical);
            return new RectD(horizontal[1], horizontal[2], vertical[1], vertical[2]);
        }

        public RectD GetUnion(IRect<double> other) 
        {
            if (this.IsEmpty) return new RectD(other.Left, other.Right, other.Bottom, other.Top);
            else if (other.IsEmpty) return this;

            double[] horizontal = { Left, Right, other.Left, other.Right };
            double[] vertical = { Top, Bottom, other.Top, other.Bottom };
            Array.Sort(horizontal);
            Array.Sort(vertical);
            return new RectD(horizontal[0], horizontal[3], vertical[0], vertical[3]);
        }
    }
    
}
