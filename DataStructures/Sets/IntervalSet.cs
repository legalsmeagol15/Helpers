using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{
    internal static class ConvenienceMethods
    {

        /// <summary>Convenience method to improve readability.</summary>
        public static bool LessThan<T>(this T a, T b) where T:IComparable<T> => a.CompareTo(b) < 0;
        /// <summary>Convenience method to improve readability.</summary>
        public static bool LessThanOrEqualTo<T>(this T a, T b) where T : IComparable<T> => a.CompareTo(b) <= 0;
        /// <summary>Convenience method to improve readability.</summary>
        public static bool EqualTo<T>(this T a, T b) where T : IComparable<T> => a.CompareTo(b) == 0;
        /// <summary>Convenience method to improve readability.</summary>
        public static bool GreaterThanOrEqualTo<T>(this T a, T b) where T : IComparable<T> => a.CompareTo(b) >= 0;
        /// <summary>Convenience method to improve readability.</summary>
        public static bool GreaterThan<T>(this T a, T b) where T : IComparable<T> => a.CompareTo(b) > 0;
    }


    /// <summary>A set that uses inflection to determine inclusion.</summary>
    /// <typeparam name="T"></typeparam>
    public class IntervalSet<T>  where T : IComparable<T>
    {
        // Each set must be immutable

        private Interval[] _Intervals;

        /// <summary>Returns whether this <see cref="IntervalSet{T}"/> is an empty set.</summary>
        public bool IsEmpty { get => _Intervals.Length == 0; }

        /// <summary>Returns whether this <see cref="IntervalSet{T}"/> is the universal set for type T.</summary>
        public bool IsUniversal { get => _Intervals.Length == 1 && _Intervals[0].IsUniversal; }

        /// <summary>Create a new <see cref="IntervalSet{T}"/> with the singleton as the first interval.</summary>
        public IntervalSet(T singleton) : this(new Interval(false, singleton, true, singleton, true, false)) { }
        /// <summary>Create a new <see cref="IntervalSet{T}"/> with the indicated first included interval.</summary>
        public IntervalSet(T start, T end) : this(new Interval(false, start, true, end, true, false)) { }
        /// <summary>Create a new <see cref="IntervalSet{T}"/> with the indicated first included interval.</summary>
        public IntervalSet(T start, bool includeStart, T end, bool includeEnd) : this(new Interval(false, start, includeStart, end, includeEnd, false)) { }
        /// <summary>
        /// Create a new <see cref="IntervalSet{T}"/> with the indicated first included interval, specifying both 
        /// positive and negative infinity.
        /// </summary>
        public IntervalSet(bool negativeInfinite, T start, bool includeStart, T end, bool includeEnd, bool positiveInfinite) : this(new Interval(negativeInfinite, start, includeStart, end, includeEnd, positiveInfinite)) { }
        
        protected IntervalSet(params Interval[] intervals) { this._Intervals = intervals.ToArray(); }



        /// <summary>Replace to determine whether two items are consecutive.</summary>
        protected static Func<T, T, bool> AreConsecutive = (a, b) => false;
        
               
        


        /// <summary>Returns the inflection values of this <see cref="IntervalSet{T}"/>, in order from min to max.</summary>
        public IEnumerable<T> GetInflections()
        {
            if (IsEmpty) yield break;
            Interval interval = _Intervals[0];
            if (!interval.Start.EqualTo(MinValue) || !interval.IncludeStart) // negative-universal means omit the first Start
                yield return interval.Start;
            if (_Intervals.Length == 1)
            {
                if (!interval.End.EqualTo(MaxValue) || !interval.IncludeEnd) // if positive-universal, there's no End inflection
                    yield return interval.End;
                yield break;
            }
            yield return interval.End;
            for (int i = 1; i < _Intervals.Length-1; i++)
            {
                interval = _Intervals[i];
                yield return interval.Start;
                yield return interval.End;
            }
            interval = _Intervals[_Intervals.Length - 1];
            yield return interval.Start;
            if (!interval.End.EqualTo(MaxValue) || !interval.IncludeEnd)
                yield return interval.End;
        }



        #region Interval comparison members


        /// <summary>
        /// Inverts the given interval and returns a 1- or 2-item array.  The 2-item array will be returned if the 
        /// removal splits the interval.  If nothing would be left of the original interval, a 1-item array containing 
        /// the <seealso cref="Interval.Empty"/> interval is returned.  Otherwise, the 1-item array will contain the 
        /// remainder of the original interval.
        /// </summary>
        protected Interval[] Invert(Interval i)
        {
            Interval start, end;
            if (i.Start.EqualTo(MinValue))
                start = i.IncludeStart ? Interval.Empty : CreateInterval(MinValue);
            else
                start = CreateInterval(MinValue, true, i.Start, !i.IncludeStart);

            if (i.End.EqualTo(MaxValue))
                end = i.IncludeEnd ? Interval.Empty : CreateInterval(MaxValue);
            else
                end = CreateInterval(MaxValue, true, i.End, !i.IncludeEnd);

            if (start.IsEmpty) return new Interval[] { end };
            if (end.IsEmpty) return new Interval[] { start };
            Interval union = Union(start, end);
            return union.IsEmpty ? new Interval[] { start, end } : new Interval[] { union };
        }
        /// <summary>
        /// Returns the set result of a v b.  If the two cannot be combined contiguously, returns 
        /// <see cref="Interval.Empty"/>.  Note that this is not perfectly kosher with set theory, but it is necessary 
        /// to allow implementation flexibility.
        /// </summary>
        protected Interval Union(Interval a, Interval b)
        {
            if (a.IsEmpty) return b; // Rules out 'a' having null anywhere.
            if (b.IsEmpty) return a; // Rules out 'b' having null anywhere.

            if (b.Start.LessThan(a.Start)) return Union(b, a); // If 'b' started first, flip 'em around and try it that way.

            if (a.End.LessThan(b.Start)) // 'a' entirely precedes 'b'.
            {
                if (!AreConsecutive(a.End, b.Start)) return Interval.Empty;
                if (!a.IncludeEnd || !b.IncludeStart) return Interval.Empty;
                return CreateInterval(a.Start, a.IncludeStart, b.End, b.IncludeEnd);
            }
            if (a.End.EqualTo(b.Start)) // 'a' overlaps only at its end
            {
                if (a.IncludeEnd || b.IncludeStart)
                    return CreateInterval(a.Start, a.IncludeStart, b.End, b.IncludeEnd);
                return Interval.Empty;
            }

            // From here there is some significant overlap of 'a' on 'b'.
            int c = a.End.CompareTo(b.End);
            if (a.Start.EqualTo(b.Start)) // 'a' and 'b' start at the same spot.  How do they end?
            {
                if (c < 0) return CreateInterval(a.Start,
                                                 a.IncludeStart && b.IncludeStart,
                                                 a.End,
                                                 a.IncludeEnd);
                else if (c == 0) return CreateInterval(a.Start,
                                                       a.IncludeStart && b.IncludeStart,
                                                       a.End,
                                                       a.IncludeEnd && b.IncludeEnd);
                else return CreateInterval(a.Start,
                                           a.IncludeStart && b.IncludeStart,
                                           b.End,
                                           b.IncludeEnd);
            }
            // Otherwise, 'a' starts before 'b', and ends somewhere in 'b' or beyond 'b'
            if (c > 0) return b;
            else if (c == 0) return CreateInterval(b.Start, b.IncludeStart, a.End, a.IncludeEnd && b.IncludeEnd);
            else return CreateInterval(b.Start, b.IncludeStart, a.End, a.IncludeEnd);
        }

        /// <summary>Returns the set result of a ^ b.</summary>
        protected Interval Intersection(Interval a, Interval b)
        {
            if (a.IsEmpty) return Interval.Empty;
            if (b.IsEmpty) return Interval.Empty;

            if (b.Start.LessThan(a.Start)) return Union(b, a); // If 'b' started first, flip 'em around and try it that way.

            if (a.End.LessThan(b.Start)) return Interval.Empty;
            if (a.End.EqualTo(b.Start)) // 'a' overlaps only at its end
            {
                if (a.IncludeEnd && b.IncludeStart) return CreateInterval(a.End);
                return Interval.Empty;
            }

            // From here there is some significant overlap of 'a' on 'b'.
            int c = a.End.CompareTo(b.End);
            if (a.Start.EqualTo(b.Start)) // 'a' and 'b' start at the same spot.  How do they end?
            {
                if (c < 0) return CreateInterval(b.IncludeStart ? b.Start : a.Start,
                                               a.IncludeStart || b.IncludeStart,
                                               a.End,
                                               a.IncludeEnd);
                else if (c == 0) return CreateInterval(b.IncludeStart ? b.Start : a.Start,
                                                     a.IncludeStart || b.IncludeStart,
                                                     b.IncludeEnd ? b.End : a.End,
                                                     a.IncludeEnd || b.IncludeEnd);
                else return CreateInterval(b.IncludeStart ? b.Start : a.Start,
                                         a.IncludeStart || b.IncludeStart,
                                         a.End,
                                         a.IncludeEnd);
            }
            // Otherwise, 'a' starts before 'b', and ends somewhere in 'b' or beyond 'b'
            if (c > 0) return a;
            else if (c == 0) return CreateInterval(a.Start, a.IncludeStart, b.IncludeEnd ? b.End : a.End, b.IncludeEnd || a.IncludeEnd);
            else return CreateInterval(a.Start, a.IncludeStart, b.End, b.IncludeEnd);
        }

        /// <summary>Returns the set result of a - b</summary>
        protected Interval[] Except(Interval a, Interval b)
        {
            Interval[] b_inv = Invert(b);
            Interval split0 = Intersection(a, b_inv[0]);
            Interval split1 = Intersection(a, b_inv[1]);
            if (split0.IsEmpty) return new Interval[] { split1 };
            if (split1.IsEmpty) return new Interval[] { split0 };
            return new Interval[] { split0, split1 };

            //if (a.IsEmpty) return new Interval[]{ Interval.Empty};
            //if (b.IsEmpty) return new Interval[] { a };

            //if (b.Start.LessThan(a.Start))
            //{
            //    if (b.End.LessThan(a.Start))
            //        return new Interval[] { a };
            //    else if (b.End.EqualTo(a.Start))
            //        return new Interval[] { CreateInterval(a.Start, a.IncludeStart && !b.IncludeEnd, a.End, a.IncludeEnd) };
            //    else if (b.End.GreaterThan(a.End))
            //        return new Interval[] { Interval.Empty };
            //    else if (b.End.EqualTo(a.End))
            //    {
            //        if (a.IncludeEnd && !b.IncludeEnd) return new Interval[] { CreateInterval(a.End) };
            //        return new Interval[] { Interval.Empty };
            //    }
            //    else
            //        return new Interval[] { CreateInterval(b.End, !b.IncludeEnd, a.End, a.IncludeEnd) };
            //}
            //if (b.Start.EqualTo(a.Start))
            //{
            //    Interval start = (a.IncludeStart && !b.IncludeStart) ? CreateInterval(a.Start) : Interval.Empty;
            //    if (b.End.GreaterThan(a.End)) return new Interval[] { start };
            //    else if (b.End.EqualTo(a.End))
            //    {
            //        if (a.IncludeEnd && !b.IncludeEnd)
            //        {
            //            Interval end = CreateInterval(a.End);
            //            if (start.IsEmpty) return new Interval[] {end};
            //            Interval union = Union(start, end);
            //            return union.IsEmpty ? new Interval[] { start, end } : new Interval[] { union };                        
            //        }
            //        return new Interval[] { start };
            //    }
            //    else // remove.End is less than orig.end.
            //    {
            //        Interval end = CreateInterval(b.End, !b.IncludeEnd, a.End, a.IncludeEnd);
            //        if (start.IsEmpty) return new Interval[] { end };
            //        Interval union = Union(start, end);
            //        return union.IsEmpty ? new Interval[] { start, end } : new Interval[] { union };
            //    }
            //}
            //else // remove.Start is greater than orig.start.
            //{
            //    if (a.End.LessThan(b.Start))
            //        return new Interval[] { a };
            //    else if (a.End.EqualTo(b.Start))
            //        return new Interval[] { CreateInterval(a.Start, a.IncludeStart, a.End, a.IncludeEnd && !b.IncludeStart) };
            //    else if (b.Start.GreaterThan(a.End))
            //        return new Interval[] { a };
            //    else if (b.Start.EqualTo(a.End))
            //        return new Interval[] { CreateInterval(a.Start, a.IncludeStart, a.End, !b.IncludeEnd && a.IncludeEnd) };
            //    else if (b.End.GreaterThan(a.End))
            //        return new Interval[] { CreateInterval(a.Start, a.IncludeStart, b.Start, !b.IncludeStart) };
            //    else if (b.End.EqualTo(a.End)) // Split the original, leaving a singleton at the end?
            //    {
            //        Interval start = CreateInterval(a.Start, a.IncludeStart, b.Start, !b.IncludeStart);
            //        if (!a.IncludeEnd || b.IncludeEnd) return new Interval[] { start };
            //        Interval end = CreateInterval(a.End);
            //        if (start.IsEmpty) return new Interval[] { end };
            //        Interval union = Union(start, end);
            //        return union.IsEmpty ? new Interval[] { start, end } : new Interval[] { union };
            //    }
            //    else // the removal splits the original, leaving non-singletons at either end.
            //    {
            //        Interval start = CreateInterval(a.Start, a.IncludeStart, b.Start, !b.IncludeStart);
            //        Interval end = CreateInterval(b.End, !b.IncludeStart, a.End, a.IncludeStart);
            //        if (start.IsEmpty) return new Interval[] { end };
            //        Interval union = Union(start, end);
            //        return union.IsEmpty ? new Interval[] { start, end } : new Interval[] { union };
            //    }
            //}            
        }
        


        /// <summary>
        /// A simple struct representing the start of an interval, the end of an interval (the space between being 
        /// included), and whether to also include the start and end.
        /// </summary>
        protected struct Interval
        {
            /// <summary>Creates an <see cref="Interval"/> from a singleton value.</summary>
            public static Interval FromSingleton(T singleton) 
                => FromTo(false, singleton, true, singleton, true, false);
            /// <summary>Creates an <see cref="Interval"/> as a non-infinite range.</summary>
            public static Interval FromTo(T start, bool includeStart, T end, bool includeEnd)
                => FromTo(false, start, includeStart, end, includeEnd, false);
            /// <summary>Creates an <see cref="Interval"/> as a potentially infinite range.</summary>
            public static Interval FromTo(bool negativeInfinite, T start, bool includeStart, T end, bool includeEnd, bool positiveInfinite)
            {
                // Watch out for null-related exceptions where the interval cannot be safely simplified.
                if ((start == null) ^ (end == null))
                    throw new ArgumentNullException("Both or neither 'start' and 'end' must be null.");
                // If start-end are null, all the includes must be false and the interval will be empty.  Otherwise, cannot be simplified.
                else if (start == null)
                {
                    if (includeStart || includeEnd || negativeInfinite || positiveInfinite)
                        throw new ArgumentException("If 'start' and 'end' are null, this must be an empty interval.");
                    return Interval.Empty;
                }
                // Watch out for a reversed interval.  If nothing is infinite, we can safely infer that the reverse was intended.
                else if (start.GreaterThan(end))
                {
                    if (negativeInfinite || positiveInfinite)
                        throw new ArgumentException("Interval cannot be safely reversed if either tail is infinite.");
                    return FromTo(false, end, includeEnd, start, includeStart, false);
                }
                // A singleton?  Both start and end must be included.
                else if (start.EqualTo(end))
                {
                    if (includeStart ^ includeEnd)
                        throw new ArgumentException("With a singleton, both 'start' and 'end' must be included.");
                    if (!includeStart) return Interval.Empty;
                }
                // A singleton masquerading as consecutive start-end?
                else if (AreConsecutive(start, end))
                {
                    if (!negativeInfinite && !includeStart)
                    {
                        if (includeEnd || positiveInfinite) return new Interval(false, end, includeEnd, end, includeEnd, positiveInfinite);
                        else return Interval.Empty;
                    }
                    else if (!positiveInfinite && !includeEnd)
                    {
                        if (includeStart || negativeInfinite) return new Interval(negativeInfinite, start, includeStart, start, includeStart, false);
                        return Interval.Empty;
                    }
                }
                // A tailed singleton?
                else if (negativeInfinite && includeStart && includeEnd) return new Interval(true, end, true, end, true, positiveInfinite);
                else if (positiveInfinite && includeStart && includeEnd) return new Interval(negativeInfinite, start, true, start, true, true);
                // Universal?
                if (negativeInfinite && includeStart && includeEnd && positiveInfinite) return Interval.Universal;
                
                // Finally, it must be a simple range as requested.
                return new Interval(negativeInfinite, start, includeStart, end, includeEnd, positiveInfinite);
            }

            /// <summary>Where the interval starts.</summary>
            public readonly T Start;
            /// <summary>Where the interval ends.</summary>
            public readonly T End;
            /// <summary>include the start?</summary>
            public readonly bool IncludeStart;
            /// <summary>include the end?</summary>
            public readonly bool IncludeEnd;
            /// <summary>Infinite to negative infinity?</summary>
            public readonly bool IsInfiniteNegative;
            /// <summary>Infinite to positive infinity?</summary>
            public readonly bool IsInfinitePositive;
            private Interval(bool negativeInfinite, T start, bool includeStart, T end, bool includeEnd, bool positiveInfinite)
            {
                this.Start = start;
                this.End = end;
                this.IncludeStart = includeStart;
                this.IncludeEnd = includeEnd;
                this.IsInfiniteNegative = negativeInfinite;
                this.IsInfinitePositive = positiveInfinite;
            }

            /// <summary>Returns whether the item is included in this <see cref="Interval"/>.</summary>
            public bool Includes(T item)
            {
                int c_start = Start.CompareTo(item);
                if (c_start == 0) return IncludeStart;
                if (c_start > 0) return IsInfiniteNegative;

                int c_end = item.CompareTo(End);
                if (c_end == 0) return IncludeEnd;
                if (c_end > 0) return IsInfinitePositive;

                if (c_start < 0 && c_end < 0) return true;

                throw new Exception("This should be impossible - an interval cannot be set up with its End before its Start.");
            }

            /// <summary>A static <see cref="Interval"/> representing an empty interval.</summary>
            public static readonly Interval Empty = new Interval(false, default(T), false, default(T), false, false);
            /// <summary>A static <see cref="Interval"/> representing a universal set.</summary>
            public static readonly Interval Universal = new Interval(true, default(T), true, default(T), true, true);

            /// <summary>Returns whether this <see cref="Interval"/> represents an empty set.</summary>
            public bool IsEmpty { get => !IsInfiniteNegative 
                                         && !IsInfinitePositive 
                                         && !IncludeStart
                                         && !IncludeEnd
                                         && (Start == null ? End == null : Start.EqualTo(End)); }
            /// <summary>Returns whether this <see cref="Interval"/> represents a singleton.</summary>
            public bool IsSingleton { get => !IsInfiniteNegative
                                         && !IsInfinitePositive
                                         && IncludeStart
                                         && IncludeEnd
                                         && (Start == null ? false : (Start.EqualTo(End) )); }
            /// <summary>Returns whether this <see cref="Interval"/> represents a universal set in itself.</summary>
            public bool  IsUniversal { get => IsInfiniteNegative && IsInfinitePositive && IncludeStart && IncludeEnd; }
            private Interval[] Simplified(Interval a, Interval b)
            {

            }
            public static Interval[] Intersection (Interval a, Interval b)
            {
                if (a.IsEmpty) return new Interval[] { Interval.Empty };
                else if (b.IsEmpty) return new Interval[] { Interval.Empty };
                else if (a.IsUniversal) return new Interval[] { b };
                else if (b.IsUniversal) return new Interval[] { a };
            }
            public static Interval[] Union(Interval a, Interval b)
            {
                if (a.IsEmpty) return new Interval[] { b };
                else if (b.IsEmpty) return new Interval[] { a };
                else if (a.IsUniversal) return new Interval[] { Interval.Universal };
                else if (b.IsUniversal) return new Interval[] { Interval.Universal };
                else if (b.Start.LessThan(a.Start)) return Union(b, a);
                else if (a.End.LessThan(b.Start))
                {
                    if (!AreConsecutive(a.End, b.Start) || !a.IncludeEnd || !b.IncludeStart)
                    {
                        if (a.IsInfinitePositive) return new Interval[] { a };
                        else if (b.IsInfiniteNegative) return new Interval[] { b };
                        return new Interval[] 

                    }

                }
                else if (a.End.Equals(b.Start))
                {
                    if (a.IncludeEnd || b.IncludeStart)
                        return new Interval[] { new Interval(a.IsInfiniteNegative, a.Start, a.IncludeStart, b.End, b.IncludeEnd, b.IsInfinitePositive) };
                    if (a.IsInfinitePositive)
                        return new Interval[] { new Interval(a.IsInfiniteNegative, a.Start, a.IncludeStart, a.End, a.IncludeEnd || b.IncludeStart, true) };
                    if (b.IsInfiniteNegative)
                        return new Interval[] { new Interval(true, b.Start, b.IncludeStart || a.IncludeEnd, b.End, b.IncludeEnd, b.IsInfinitePositive) };
                    return new Interval[] { a, b };
                }
                T[] inflections = new T[] { a.Start, a.End, b.Start, b.End };
                Array.Sort(inflections);
                
            }
        }

        #endregion


    }


    /// <summary>Interval set for longs.</summary>
    public sealed class LongIntervalSet : IntervalSet<long>
    {
        static LongIntervalSet()
        {   
            IntervalSet<long>.AreConsecutive = (a, b) => (a == b - 1);
            LongIntervalSet.AreConsecutive = (a, b) => (a == b - 1);
        }
    }


    /// <summary>Real number interval set.  Based on doubles.</summary>
    public sealed class RealIntervalSet : IntervalSet<double>
    {
       

    }
}
