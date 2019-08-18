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
    }

    public struct RectD : IRect<double>
    {
        public static readonly RectD Empty = new RectD(double.NaN, double.NaN, double.NaN, double.NaN);

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


        public bool Intersects(IRect<double> other)
        {
            if (Right < other.Left || Left > other.Right) return false;
            if (Top < other.Bottom || Bottom > other.Top) return false;
            return true;
        }

        public RectD GetIntersection(IRect<double> other)
        {
            if (!Intersects(other)) return Empty;
            double[] horizontal = { Left, Right, other.Left, other.Right };
            double[] vertical = { Top, Bottom, other.Top, other.Bottom };
            Array.Sort(horizontal);
            Array.Sort(vertical);
            return new RectD(horizontal[1], horizontal[2], vertical[1], vertical[2]);
        }

        public RectD GetUnion(IRect<double> other) 
        {
            double[] horizontal = { Left, Right, other.Left, other.Right };
            double[] vertical = { Top, Bottom, other.Top, other.Bottom };
            Array.Sort(horizontal);
            Array.Sort(vertical);
            return new RectD(horizontal[0], horizontal[3], vertical[0], vertical[3]);
        }
    }

    public struct RectI : IRect<int>
    {
        public static readonly RectI Empty = new RectI(int.MinValue, int.MinValue, int.MinValue, int.MinValue);

        public readonly int Left, Right, Top, Bottom;

        int IRect<int>.Left => Left;

        int IRect<int>.Right => Right;

        int IRect<int>.Top => Top;

        int IRect<int>.Bottom => Bottom;

        public int Width => Right - Left;

        public int Height => Top - Bottom;

        public int Area => Height * Width;

        public bool IsEmpty => Top == int.MinValue || Bottom == int.MinValue || Right == int.MinValue || Left == int.MinValue;

        public bool IsPoint => Left == Right && Top == Bottom;

        public bool IsLineSegment => (Left == Right) ^ (Top == Bottom);

        public RectI(int left, int right, int bottom, int top)
        {
            this.Left = left; this.Right = right; this.Top = top; this.Bottom = bottom;
        }


        public bool Intersects(IRect<int> other)
        {
            if (Right < other.Left || Left > other.Right) return false;
            if (Top < other.Bottom || Bottom > other.Top) return false;
            return true;
        }

        public RectI GetIntersection(IRect<int> other)
        {
            if (!Intersects(other)) return Empty;
            int[] horizontal = { Left, Right, other.Left, other.Right };
            int[] vertical = { Top, Bottom, other.Top, other.Bottom };
            Array.Sort(horizontal);
            Array.Sort(vertical);
            return new RectI(horizontal[1], horizontal[2], vertical[1], vertical[2]);
        }

        public RectI GetUnion(IRect<int> other)
        {
            int[] horizontal = { Left, Right, other.Left, other.Right };
            int[] vertical = { Top, Bottom, other.Top, other.Bottom };
            Array.Sort(horizontal);
            Array.Sort(vertical);
            return new RectI(horizontal[0], horizontal[3], vertical[0], vertical[3]);
        }
    }

    public struct RectM : IRect<decimal>
    {
        public static readonly RectM Empty = new RectM(decimal.MinValue, decimal.MinValue, decimal.MinValue, decimal.MinValue);

        public readonly decimal Left, Right, Top, Bottom;

        decimal IRect<decimal>.Left => Left;

        decimal IRect<decimal>.Right => Right;

        decimal IRect<decimal>.Top => Top;

        decimal IRect<decimal>.Bottom => Bottom;

        public decimal Width => Right - Left;

        public decimal Height => Top - Bottom;

        public decimal Area => Height * Width;

        public bool IsEmpty => Top == decimal.MinValue || Bottom == decimal.MinValue || Right == decimal.MinValue || Left == decimal.MinValue;

        public bool IsPoint => Left == Right && Top == Bottom;

        public bool IsLineSegment => (Left == Right) ^ (Top == Bottom);

        public RectM(decimal left, decimal right, decimal bottom, decimal top)
        {
            this.Left = left; this.Right = right; this.Top = top; this.Bottom = bottom;
        }


        public bool Intersects(IRect<decimal> other)
        {
            if (Right < other.Left || Left > other.Right) return false;
            if (Top < other.Bottom || Bottom > other.Top) return false;
            return true;
        }

        public RectM GetIntersection(IRect<decimal> other)
        {
            if (!Intersects(other)) return Empty;
            decimal[] horizontal = { Left, Right, other.Left, other.Right };
            decimal[] vertical = { Top, Bottom, other.Top, other.Bottom };
            Array.Sort(horizontal);
            Array.Sort(vertical);
            return new RectM(horizontal[1], horizontal[2], vertical[1], vertical[2]);
        }

        public RectM GetUnion(IRect<decimal> other)
        {
            decimal[] horizontal = { Left, Right, other.Left, other.Right };
            decimal[] vertical = { Top, Bottom, other.Top, other.Bottom };
            Array.Sort(horizontal);
            Array.Sort(vertical);
            return new RectM(horizontal[0], horizontal[3], vertical[0], vertical[3]);
        }
    }
}
