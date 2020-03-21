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

        IRect<T> GetUnion(IRect<T> other);
        IRect<T> GetIntersection(IRect<T> other);
    }

    public interface IBounded<T> where T: struct
    {
        IRect<T> Bounds { get; }
    }

    /// <summary>
    /// Represents a rectangle.  Duh.
    /// <para/>There exist other versions of this in standard libraries.  This implementation is 
    /// so I don't have to reference those libraries here.  This might be a bad idea, we'll see.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct Rect<T> : IRect<T> where T: struct, IComparable<T>
    {
        public static readonly Rect<T> Empty = new Rect<T>(true);

        public bool IsEmpty { get; }
        public T Left { get; }
        public T Right { get; }
        public T Bottom { get; }
        public T Top { get; }

        public bool IsPoint => Left.Equals(Right) && Bottom.Equals(Top);
        public bool IsEdge => Left.Equals(Right) ^ Bottom.Equals(Top);

        /// <summary>Creates an empty <see cref="Rect{T}"/>.</summary>
        private Rect(bool isEmpty) { IsEmpty = isEmpty; Left = Right = Bottom = Top = default(T); }

        public Rect(T left, T right, T bottom, T top)
        {
            if (left.CompareTo(right) > 0) throw new ArgumentException("Left must be equal to or less than right.");
            if (bottom.CompareTo(top) > 0) throw new ArgumentException("Bottom must be equal to or less than top.");
            this.Left = left;
            this.Right = right;
            this.Bottom = bottom;
            this.Top = top;
            this.IsEmpty = false;
        }

        public bool Contains(IRect<T> other)
        {
            if (IsEmpty || other.IsEmpty) return false;
            return Left.CompareTo(other.Left) <= 0
                    && other.Right.CompareTo(Right) <= 0
                    && Bottom.CompareTo(other.Bottom) <= 0
                    && other.Top.CompareTo(Top) <= 0;
        }

        public bool Overlaps(IRect<T> other)
        {
            if (IsEmpty || other.IsEmpty) return false;
            return !(Right.CompareTo(other.Left) < 0
                    || Left.CompareTo(other.Right) > 0
                    || Bottom.CompareTo(other.Top) > 0
                    || Top.CompareTo(other.Bottom) < 0);
        }

        public IRect<T> GetIntersection(IRect<T> other)
        {
            if (!Overlaps(other)) return Empty;
            T left = (Left.CompareTo(other.Left) > 0) ? Left : other.Left;
            T right = (Right.CompareTo(other.Right) < 0) ? Right : other.Right;
            T bottom = (Bottom.CompareTo(other.Bottom) > 0) ? Bottom : other.Bottom;
            T top = (Top.CompareTo(other.Top) < 0) ? Top: other.Top;
            return new Rect<T>(left, right, bottom, top);
        }

        public Rect<T> GetUnion(IRect<T> other)
        {
            if (!Overlaps(other)) return Empty;
            T left = (Left.CompareTo(other.Left) < 0) ? Left : other.Left;
            T right = (Right.CompareTo(other.Right) > 0) ? Right : other.Right;
            T bottom = (Bottom.CompareTo(other.Bottom) < 0) ? Bottom : other.Bottom;
            T top = (Top.CompareTo(other.Top) > 0) ? Top : other.Top;
            return new Rect<T>(left, right, bottom, top);
        }
        IRect<T> IRect<T>.GetUnion(IRect<T> other) => GetUnion(other);

        public override bool Equals(object obj)
            => obj is Rect<T> other && Left.Equals(other.Left) 
                                    && Right.Equals(other.Right)
                                    && Top.Equals(other.Top) 
                                    && Bottom.Equals(other.Bottom);
        public override int GetHashCode()
        {
            unchecked
            {
                return Math.Abs(Left.GetHashCode() + Right.GetHashCode() + Top.GetHashCode() + Bottom.GetHashCode());
            }
        }
    }
    
}
