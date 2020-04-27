using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics.Geometry
{
    public interface IRect<T> where T : struct, IComparable<T> // TODO:  does it have to be struct?
    {
        T Left { get; }
        T Right { get; }
        T Top { get; }
        T Bottom { get; }

        bool Contains(IPoint<T> point);
        bool Contains(IRect<T> other);
        bool Overlaps(IRect<T> other);
        bool IsEmpty { get; }

        IRect<T> GetUnion(IRect<T> other);
        IRect<T> GetIntersection(IRect<T> other);
    }

    public interface IBounded<T> where T : struct, IComparable<T>
    {
        IRect<T> Bounds { get; }
    }

    public static class Helpers
    {
        public static bool SharesEdge<T>(this IRect<T> a, IRect<T> other) where T:struct, IComparable<T>
        {
            if (a.IsEmpty || other.IsEmpty) return false;
            return (a.Left.CompareTo(other.Left) == 0) 
                || (a.Right.CompareTo(other.Right) == 0) 
                || (a.Top.CompareTo(other.Top) == 0) 
                || (a.Bottom.CompareTo(other.Bottom) == 0);
        }

        /// <summary>
        /// Returns the union of all the given items.  The union will equal the 
        /// <see cref="IRect{T}"/> exactly large enough to include all points in all items.  Note 
        /// that the two <see cref="IRect{T}"/> items do not need to intersect.
        /// </summary>
        public static IRect<T> GetUnion<T>(this IEnumerable<IRect<T>> items) where T:struct, IComparable<T>
        {
            // TODO:  getting the union of a large set of items can be made more efficient by 
            IRect<T> result = Rect<T>.Empty;
            foreach (var item in items) result = result.GetUnion(item);
            return result;
        }

        /// <summary>
        /// Returns the union of all the given items.  The union will equal the 
        /// <see cref="IRect{T}"/> exactly large enough to include all points in all items.  Note 
        /// that the two <see cref="IRect{T}"/> items do not need to intersect.
        /// </summary>
        public static IRect<T> GetUnion<T>(this IEnumerable<IBounded<T>> items) where T : struct, IComparable<T>
            => GetUnion(items.Select(item => item.Bounds));
    }

    /// <summary>
    /// Represents a rectangle.  Duh.
    /// <para/>There exist other versions of this in standard libraries.  This implementation is 
    /// so I don't have to reference those libraries here.  This might be a bad idea, we'll see.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct Rect<T> : IRect<T> where T : struct, IComparable<T>
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
        private Rect(bool isEmpty) { IsEmpty = isEmpty; Left = Right = Bottom = Top = default; }

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

        public bool Contains(IPoint<T> point)
        {
            if (IsEmpty) return false;
            return Left.CompareTo(point.X) <= 0
                    && point.X.CompareTo(Right) <= 0
                    && Bottom.CompareTo(point.Y) <= 0
                    && point.Y.CompareTo(Top) <= 0;
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

        public Rect<T> GetIntersection(IRect<T> other)
        {
            if (!Overlaps(other)) return Empty;
            T left = (Left.CompareTo(other.Left) > 0) ? Left : other.Left;
            T right = (Right.CompareTo(other.Right) < 0) ? Right : other.Right;
            T bottom = (Bottom.CompareTo(other.Bottom) > 0) ? Bottom : other.Bottom;
            T top = (Top.CompareTo(other.Top) < 0) ? Top : other.Top;
            return new Rect<T>(left, right, bottom, top);
        }
        IRect<T> IRect<T>.GetIntersection(IRect<T> other) => GetIntersection(other);

        /// <summary>
        /// Returns the union of this and the given item.  The union will equal the 
        /// <see cref="IRect{T}"/> exactly large enough to include all points in both items.  Note 
        /// that the two <see cref="IRect{T}"/> items do not need to intersect.
        /// </summary>     
        public Rect<T> GetUnion(IRect<T> other)
        {
            if (other.IsEmpty) return this;
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
