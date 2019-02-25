using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DataStructures.Intervals;

namespace DataStructures
{
    public static class Intervals
    {

        /// <summary>Convenience method to improve readability.</summary>
        public static bool LessThan<T>(this T a, T b) where T : IComparable<T> => a.CompareTo(b) < 0;
        /// <summary>Convenience method to improve readability.</summary>
        public static bool LessThanOrEqualTo<T>(this T a, T b) where T : IComparable<T> => a.CompareTo(b) <= 0;
        /// <summary>Convenience method to improve readability.</summary>
        public static bool EqualTo<T>(this T a, T b) where T : IComparable<T> => a.CompareTo(b) == 0;
        /// <summary>Convenience method to improve readability.</summary>
        public static bool GreaterThanOrEqualTo<T>(this T a, T b) where T : IComparable<T> => a.CompareTo(b) >= 0;
        /// <summary>Convenience method to improve readability.</summary>
        public static bool GreaterThan<T>(this T a, T b) where T : IComparable<T> => a.CompareTo(b) > 0;

        internal static Infinity Flip(this Infinity inf) => (Infinity)(-1 * (int)inf);

        internal static IInterval<T>[] AsArray<T>(this IInterval<T> interval) where T : IComparable<T>
            => new IInterval<T>[] { interval };
        internal static IInterval<T>[] AsArray<T>(params IInterval<T>[] intervals) where T : IComparable<T>
            => intervals;

        /// <summary>Finds the first inflection point in a given interval.</summary>
        public static T FirstInflection<T>(this IInterval<T> i) where T : IComparable<T>
            => FirstInflection(i, out bool _);
        /// <summary>
        /// Finds the first inflection point  in a given interval, and indicates whether that point is included.
        /// </summary>
        public static T FirstInflection<T>(this IInterval<T> i, out bool included) where T : IComparable<T>
        {
            switch (i)
            {
                case Limited<T> l: included = l.IncludeStart; return l.Start;
                case Infinite<T> inf: included = inf.IncludeHead; return inf.Head;
                case Singleton<T> s: included = true; return s.Item;
                case Universal<T> u: throw new Exception("Universal intervals have no inflections.");
                case Empty<T> e: throw new Exception("Empty intervals have no inflections.");
                default: throw new NotImplementedException();
            }
        }
        /// <summary>Finds the first inflection point in a given interval.</summary>
        public static T LastInflection<T>(this IInterval<T> i) where T : IComparable<T>
            => LastInflection(i, out bool _);
        /// <summary>
        /// Finds the first inflection point  in a given interval, and indicates whether that point is included.
        /// </summary>
        public static T LastInflection<T>(this IInterval<T> i, out bool included) where T : IComparable<T>
        {
            switch (i)
            {
                case Limited<T> l: included = l.IncludeEnd; return l.End;
                case Infinite<T> inf: included = inf.IncludeHead; return inf.Head;
                case Singleton<T> s: included = true; return s.Item;
                case Universal<T> u: throw new Exception("Universal intervals have no inflections.");
                case Empty<T> e: throw new Exception("Empty intervals have no inflections.");
                default: throw new NotImplementedException();
            }
        }

        /// <summary>Gets the index of the interval whose first inflection immediately precedes the given value.
        /// <para/>Returns -1 if there is no preceding interval.</summary>
        internal static int GetPrecedingIndex<T>(this IInterval<T>[] chain, T item) where T : IComparable<T>
        {
            int c = item.CompareTo(FirstInflection(chain[0]));
            if (c < 0) return -1;

            int min = 0, max = chain.Length - 1, half = (max - min) / 2;
            while (true)
            {
                if (min == max) return min;
                c = item.CompareTo(FirstInflection(chain[half]));
                if (c < 0) { min = half; half = ((max - min) / 2); if (min == half) half++; continue; }
                else if (c > 0) { max = half; half = (max - min) / 2; continue; }
                else return half;
            }
        }



        #region Interval AND operations

        internal static IInterval<T> And<T>(this Singleton<T> singleton, IInterval<T> other) where T : IComparable<T>
        {
            if (other.Contains(singleton.Item)) return singleton;
            return new Empty<T>();
        }

        internal static IInterval<T> And<T>(this Infinite<T> inf, Singleton<T> singleton) where T : IComparable<T>
            => And(singleton, inf);
        internal static IInterval<T> And<T>(this Infinite<T> a, Infinite<T> b) where T : IComparable<T>
        {
            int c = a.Head.CompareTo(b.Head);
            if (c == 0)
            {
                if (a.Tail != b.Tail)
                {
                    if (a.IncludeHead && b.IncludeHead) return new Singleton<T>(a.Head);
                    return new Empty<T>();
                }
                return new Infinite<T>(a.Head, a.IncludeHead && b.IncludeHead, a.Tail);
            }
            if (c < 0)
            {
                if (a.IsPositive)
                {
                    if (b.IsNegative)
                        return new Limited<T>(a.Head, a.IncludeHead, b.Head, b.IncludeHead);
                    return b;
                }
                else if (b.IsNegative) return a;
                return new Empty<T>();
            }
            else
            {
                if (a.IsPositive)
                {
                    if (b.IsPositive) return b;
                    return new Empty<T>();
                }
                else if (b.IsPositive)
                    return new Limited<T>(b.Head, b.IncludeHead, a.Head, a.IncludeHead);
                return new Empty<T>();
            }
        }
        internal static IInterval<T> And<T>(this Infinite<T> inf, Limited<T> ltd) where T : IComparable<T>
        {
            int c = inf.Head.CompareTo(ltd.Start);
            if (c < 0)
            {
                if (inf.IsNegative) return new Empty<T>();
                return ltd;
            }
            else if (c == 0)
                return new Limited<T>(inf.Head, inf.IncludeHead && ltd.IncludeStart, ltd.End, ltd.IncludeEnd);
            else if ((c = inf.Head.CompareTo(ltd.End)) > 0)
            {
                if (inf.IsPositive) return new Empty<T>();
                return ltd;
            }
            else if (c == 0)
                return new Limited<T>(ltd.Start, ltd.IncludeStart, inf.Head, inf.IncludeHead && ltd.IncludeEnd);
            else if (inf.IsPositive)
                return new Limited<T>(inf.Head, inf.IncludeHead, ltd.End, ltd.IncludeEnd);
            else
                return new Limited<T>(ltd.Start, ltd.IncludeStart, inf.Head, inf.IncludeHead);

        }
        internal static IInterval<T> And<T>(this Infinite<T> inf, IInterval<T> i) where T : IComparable<T>
        {
            switch (i)
            {
                case Singleton<T> s2: return And(inf, s2);
                case Limited<T> l2: return And(inf, l2);
                case Infinite<T> i2: return And(inf, i2);
                case Universal<T> u2: return inf;
                case Empty<T> e2: return e2;
                default: throw new NotImplementedException();
            }
        }

        internal static IInterval<T> And<T>(this Limited<T> ltd, Singleton<T> singleton) where T : IComparable<T>
            => And(singleton, ltd);
        internal static IInterval<T> And<T>(this Limited<T> ltd, Infinite<T> inf) where T : IComparable<T>
            => And(inf, ltd);
        internal static IInterval<T> And<T>(this Limited<T> a, Limited<T> b) where T : IComparable<T>
        {
            int c = a.Start.CompareTo(b.Start);
            if (c > 0) return And(b, a);
            c = a.End.CompareTo(b.Start);
            if (c < 0) return new Empty<T>();
            if (c == 0)
            {
                if (a.IncludeEnd && b.IncludeStart) return new Singleton<T>(a.End);
                return new Empty<T>();
            }
            c = a.Start.CompareTo(b.Start);
            if (c == 0) return (a.End.CompareTo(b.End) <= 0) ? a : b;
            c = a.End.CompareTo(b.End);
            if (c < 0) return new Limited<T>(b.Start, b.IncludeStart, a.End, a.IncludeEnd);
            if (c == 0) return new Limited<T>(b.Start, b.IncludeStart, a.End, a.IncludeEnd && b.IncludeEnd);
            return b;
        }
        internal static IInterval<T> And<T>(this Limited<T> l, IInterval<T> i) where T : IComparable<T>
        {
            switch (i)
            {
                case Singleton<T> s2: return And(l, s2);
                case Limited<T> l2: return And(l, l2);
                case Infinite<T> i2: return And(l, i2);
                case Universal<T> u2: return l;
                case Empty<T> e2: return e2;
                default: throw new NotImplementedException();
            }
        }

        /// <summary>Returns the intersection of the two intervals.</summary>
        public static IInterval<T>[] And<T>(this IInterval<T> a, IInterval<T> b) where T : IComparable<T>
        {
            switch (a)
            {
                case Singleton<T> s: return And(s, b).AsArray();
                case Limited<T> l: return And(l, b).AsArray();
                case Infinite<T> i: return And(i, b).AsArray();
                case Universal<T> u: return b.AsArray();
                case Empty<T> e: return e.AsArray();
                default: throw new NotImplementedException();
            }
        }
        /// <summary>Returns the intervals which constitute the intersection of the interval and the interval set.</summary>
        /// <param name="chain">Must be in ascending order and not overlapping.</param>
        /// <param name="single">The interval with which the interval set will be intersected.</param>
        public static IInterval<T>[] And<T>(this IInterval<T> single, IInterval<T>[] chain) where T : IComparable<T>
        {
            if (chain.Length == 0) return new Empty<T>().AsArray();
            List<IInterval<T>> list = new List<IInterval<T>>();
            IInterval<T> anded = new Empty<T>();
            int idx = 0;
            while (idx < chain.Length && anded is Empty<T>)
                anded = And(single, chain[idx++])[0];
            while (idx < chain.Length
                    && !(anded is Empty<T>))
            {
                IInterval<T> newAnded = And(anded, chain[idx++])[0];
                if (newAnded is Empty<T>) list.Add(anded);
                anded = newAnded;
            }
            if (list.Count == 0) return new Empty<T>().AsArray();
            return list.ToArray();
        }
        /// <summary>Returns the intervals which constitute the intersection of the two interval sets.</summary>
        /// <param name="a">Must be in ascending order and not overlapping.</param>
        /// <param name="b">Must be in ascending order and not overlapping.</param>
        public static IInterval<T>[] And<T>(IInterval<T>[] a, IInterval<T>[] b) where T : IComparable<T>
        {
            if (a.Length == 0 || b.Length == 0) return new Empty<T>().AsArray();
            if (FirstInflection(a[0]).GreaterThanOrEqualTo(FirstInflection(b[0])))
                return And(b, a);
            int aIdx = 0, bIdx = 0;
            List<IInterval<T>> list = new List<IInterval<T>>();
            while (aIdx < a.Length && bIdx < b.Length)
            {
                IInterval<T> x = And(a[aIdx], b[bIdx])[0];
                if (x is Empty<T>)
                {
                    AppendList(x);
                    bIdx++;
                }
                else if (x is Universal<T>)
                    return new Universal<T>().AsArray();
                else if (FirstInflection(a[aIdx]).GreaterThanOrEqualTo(FirstInflection(b[bIdx])))
                {
                    // we've come far enough that 'b' is ahead.  Swap them and keep going.
                    aIdx++;
                    Swap(ref b, ref a);
                    Swap(ref bIdx, ref aIdx);

                }
            }
            while (aIdx < a.Length)
            {
                IInterval<T> x = And(a[aIdx++], b[bIdx - 1])[0];
                if (x is Empty<T>)
                    AppendList(x);
            }
            while (bIdx < b.Length)
            {
                IInterval<T> x = And(b[bIdx++], a[aIdx - 1])[0];
                if (x is Empty<T>)
                    AppendList(x);
            }
            return list.ToArray();

            void Swap<U>(ref U first, ref U second)
            {
                var temp = first;
                first = second;
                second = first;
            }
            void AppendList(IInterval<T> interval)
            {
                IInterval<T>[] newIntervals = Or(interval, list[list.Count - 1]);
                if (newIntervals.Length == 2) list.Add(newIntervals[1]);
                else list[list.Count - 1] = newIntervals[0];
            }
        }

        #endregion



        #region Interval NOT operations 

        internal static IInterval<T>[] Not<T>(this Singleton<T> singleton) where T : IComparable<T>
            => AsArray(new Infinite<T>(singleton.Item, false, Infinity.Negative),
                        new Infinite<T>(singleton.Item, false, Infinity.Positive));
        internal static IInterval<T> Not<T>(this Infinite<T> inf) where T : IComparable<T>
        {
            return new Infinite<T>(inf.Head, !inf.IncludeHead, inf.Tail.Flip());
        }
        internal static IInterval<T>[] Not<T>(this Limited<T> ltd) where T : IComparable<T>
            => AsArray(new Infinite<T>(ltd.Start, !ltd.IncludeStart, Infinity.Negative),
                        new Infinite<T>(ltd.End, !ltd.IncludeEnd, Infinity.Positive));

        /// <summary>Returns the intervals which constitute the inverse of the given interval.</summary>
        public static IInterval<T>[] Not<T>(this IInterval<T> interval) where T : IComparable<T>
        {
            switch (interval)
            {
                case Singleton<T> s: return Not(s);
                case Limited<T> l: return Not(l);
                case Infinite<T> i: return Not(i).AsArray();
                case Universal<T> _: return new Empty<T>().AsArray();
                case Empty<T> _: return new Universal<T>().AsArray();
                default: throw new NotImplementedException();
            }
        }
        /// <summary>Returns the inverse of the given set of intervals.</summary>
        /// <param name="chain">Must be in ascending order and non-overlapping.</param>
        public static IInterval<T>[] Not<T>(params IInterval<T>[] chain) where T : IComparable<T>
        {
            if (chain.Length == 0)
                return new Universal<T>().AsArray();
            if (chain.Length == 1)
                switch (chain[0])
                {
                    case Singleton<T> singleton: return Not(singleton);
                    case Infinite<T> inf: return Not(inf).AsArray();
                    case Limited<T> ltd: return Not(ltd);
                    default: throw new NotImplementedException();
                }

            T first = FirstInflection(chain[0], out bool includeFirst);
            List<IInterval<T>> list = new List<IInterval<T>>();
            if (!(chain[0] is Infinite<T> negInf) || negInf.Tail != Infinity.Negative)
                list.Add(new Infinite<T>(first, !includeFirst, Infinity.Negative));
            T last = LastInflection(chain[0], out bool includeLast);
            for (int i = 0; i < chain.Length - 1; i++)
            {
                last = LastInflection(chain[i], out includeLast);
                first = FirstInflection(chain[i + 1], out includeFirst);
                if (first.EqualTo(last))
                {
                    if (includeLast || includeFirst) throw new NotImplementedException("This shouldn't be possible.");
                    list.Add(new Singleton<T>(last));
                }
                else
                    list.Add(new Limited<T>(last, !includeLast, first, !includeFirst));
            }
            if (!(chain[chain.Length - 1] is Infinite<T> posInf) || posInf.Tail != Infinity.Positive)
                list.Add(new Infinite<T>(last, !includeLast, Infinity.Positive));
            return list.ToArray();
        }
        #endregion



        #region Interval OR operations

        internal static IInterval<T>[] Or<T>(this Singleton<T> a, Singleton<T> b) where T : IComparable<T>
            => (a.Item.CompareTo(b.Item) == 0) ? a.AsArray() : AsArray(a, b);
        internal static IInterval<T>[] Or<T>(this Singleton<T> singleton, Infinite<T> inf) where T : IComparable<T>
        {
            int c = singleton.Item.CompareTo(inf.Head);
            if (c < 0)
            {
                if (inf.IsNegative) return inf.AsArray();
                return AsArray(singleton, inf);
            }
            else if (c > 0)
            {
                if (inf.IsPositive) return inf.AsArray();
                return AsArray(inf, singleton);
            }
            return new Infinite<T>(singleton.Item, true, inf.Tail).AsArray();
        }
        internal static IInterval<T>[] Or<T>(this Singleton<T> singleton, Limited<T> ltd) where T : IComparable<T>
        {
            int c;
            if ((c = singleton.Item.CompareTo(ltd.Start)) < 0) return AsArray(singleton, ltd);
            else if (c == 0) return new Limited<T>(singleton.Item, true, ltd.End, ltd.IncludeEnd).AsArray();
            else if ((c = singleton.Item.CompareTo(ltd.End)) > 0) return AsArray(ltd, singleton);
            else if (c == 0) return new Limited<T>(ltd.Start, ltd.IncludeStart, singleton.Item, true).AsArray();
            return ltd.AsArray();
        }
        internal static IInterval<T>[] Or<T>(this Singleton<T> s, IInterval<T> i) where T : IComparable<T>
        {
            switch (i)
            {
                case Singleton<T> s2: return Or(s, s2);
                case Limited<T> l2: return Or(s, l2);
                case Infinite<T> i2: return Or(s, i2);
                case Universal<T> u2: return u2.AsArray();
                case Empty<T> e2: return s.AsArray();
                default: throw new NotImplementedException();
            }
        }

        internal static IInterval<T>[] Or<T>(this Infinite<T> inf, Singleton<T> singleton) where T : IComparable<T>
            => Or(singleton, inf);
        internal static IInterval<T>[] Or<T>(this Infinite<T> a, Infinite<T> b) where T : IComparable<T>
        {
            int c = a.Head.CompareTo(b.Head);
            if (c < 0)
            {
                if (a.IsPositive) return (a.Tail == b.Tail) ? a.AsArray() : new Universal<T>().AsArray();
                else return (a.Tail == b.Tail) ? b.AsArray() : AsArray(a, b);
            }
            else if (c > 0)
            {
                if (a.IsNegative) return (a.Tail == b.Tail) ? a.AsArray() : new Universal<T>().AsArray();
                else return (a.Tail == b.Tail) ? b.AsArray() : AsArray(b, a);
            }
            else if (a.Tail == b.Tail)
                return new Infinite<T>(a.Head, a.IncludeHead || b.IncludeHead, a.Tail).AsArray();
            else if (a.IncludeHead || b.IncludeHead)
                return new Universal<T>().AsArray();
            else
                return AsArray(new Infinite<T>(a.Head, false, Infinity.Negative),
                                new Infinite<T>(a.Head, false, Infinity.Positive));
        }
        internal static IInterval<T>[] Or<T>(this Infinite<T> inf, Limited<T> ltd) where T : IComparable<T>
        {
            int c;
            if ((c = inf.Head.CompareTo(ltd.Start)) < 0)
            {
                if (inf.IsNegative) return AsArray(inf, ltd);
                return inf.AsArray();
            }
            else if (c == 0)
            {
                if (inf.IsNegative)
                    return (inf.IncludeHead || ltd.IncludeStart) ? new Infinite<T>(ltd.End, ltd.IncludeEnd, Infinity.Negative).AsArray()
                                                                    : AsArray(inf, ltd);
                return new Infinite<T>(inf.Head, inf.IncludeHead || ltd.IncludeStart, inf.Tail).AsArray();
            }
            else if ((c = inf.Head.CompareTo(ltd.End)) > 0)
            {
                if (inf.IsPositive) return AsArray(ltd, inf);
                return inf.AsArray();
            }
            else if (c == 0)
            {
                if (inf.IsPositive)
                    return (inf.IncludeHead || ltd.IncludeEnd) ? new Infinite<T>(ltd.Start, ltd.IncludeStart, Infinity.Positive).AsArray()
                                                                    : AsArray(ltd, inf);
                return new Infinite<T>(inf.Head, inf.IncludeHead || ltd.IncludeEnd, inf.Tail).AsArray();
            }
            else if (inf.IsPositive)
                return new Infinite<T>(ltd.Start, ltd.IncludeStart, inf.Tail).AsArray();
            else
                return new Infinite<T>(ltd.End, ltd.IncludeEnd, inf.Tail).AsArray();
        }
        internal static IInterval<T>[] Or<T>(this Infinite<T> inf, IInterval<T> i) where T : IComparable<T>
        {
            switch (i)
            {
                case Singleton<T> s2: return Or(inf, s2);
                case Limited<T> l2: return Or(inf, l2);
                case Infinite<T> i2: return Or(inf, i2);
                case Universal<T> u2: return u2.AsArray();
                case Empty<T> e2: return inf.AsArray();
                default: throw new NotImplementedException();
            }
        }

        internal static IInterval<T>[] Or<T>(this Limited<T> ltd, Singleton<T> singleton) where T : IComparable<T>
            => Or(singleton, ltd);
        internal static IInterval<T>[] Or<T>(this Limited<T> ltd, Infinite<T> inf) where T : IComparable<T>
            => Or(inf, ltd);
        internal static IInterval<T>[] Or<T>(this Limited<T> a, Limited<T> b) where T : IComparable<T>
        {
            int c;
            if ((c = a.Start.CompareTo(b.Start)) > 0) return Or(b, a);
            else if (c == 0)
            {
                if ((c = a.End.CompareTo(b.End)) < 0) return new Limited<T>(a.Start, a.IncludeStart || b.IncludeStart, b.End, b.IncludeEnd).AsArray();
                else if (c > 0) return new Limited<T>(a.Start, a.IncludeStart || b.IncludeStart, a.End, a.IncludeEnd).AsArray();
                return new Limited<T>(a.Start, a.IncludeStart || b.IncludeStart, a.End, a.IncludeEnd || b.IncludeEnd).AsArray();
            }
            else if ((c = a.End.CompareTo(b.Start)) < 0) return AsArray(a, b);
            else if (c == 0) return (a.IncludeEnd || b.IncludeStart) ? new Limited<T>(a.Start, a.IncludeStart, b.End, b.IncludeEnd).AsArray()
                                                                        : AsArray(a, b);
            else if ((c = a.End.CompareTo(b.End)) < 0) return new Limited<T>(a.Start, a.IncludeStart, b.End, b.IncludeEnd).AsArray();
            else if (c == 0) return new Limited<T>(a.Start, a.IncludeStart, a.End, a.IncludeEnd || b.IncludeEnd).AsArray();
            else return new Limited<T>(a.Start, a.IncludeStart, a.End, a.IncludeEnd).AsArray();
        }
        internal static IInterval<T>[] Or<T>(this Limited<T> l, IInterval<T> i) where T : IComparable<T>
        {
            switch (i)
            {
                case Singleton<T> s2: return Or(l, s2);
                case Limited<T> l2: return Or(l, l2);
                case Infinite<T> i2: return Or(l, i2);
                case Universal<T> u2: return u2.AsArray();
                case Empty<T> e2: return l.AsArray();
                default: throw new NotImplementedException();
            }
        }

        /// <summary>Returns the union of the two intervals.</summary>
        internal static IInterval<T>[] Or<T>(this IInterval<T> a, IInterval<T> b) where T : IComparable<T>
        {
            switch (a)
            {
                case Singleton<T> s: return Or(s, b);
                case Limited<T> l: return Or(l, b);
                case Infinite<T> i: return Or(i, b);
                case Universal<T> u: return a.AsArray();
                case Empty<T> e: return b.AsArray();
                default: throw new NotImplementedException();
            }
        }
        /// <summary>Returns the intervals which constitute the union of the interval and the interval set.</summary>
        /// <param name="chain">Must be in ascending order and not overlapping.</param>
        /// <param name="interval">The interval with which the interval set will be unioned.</param>
        internal static IInterval<T>[] Or<T>(this IInterval<T> interval, IInterval<T>[] chain) where T : IComparable<T>
        {
            List<IInterval<T>> list = new List<IInterval<T>>();
            IInterval<T> focus = interval;
            int idx = GetPrecedingIndex(FirstInflection(interval), chain);
            if (idx < 0) idx = 0;
            for (int i = 0; i < idx; i++)
                list.Add(chain[i]);
            for (; idx < chain.Length; idx++)
            {
                IInterval<T>[] union = Or(focus, chain[idx]);
                if (union.Length == 1) focus = union[0];
                else
                {
                    list.Add(union[0]);
                    list.Add(union[1]);
                    break;
                }
            }
            for (; idx < chain.Length; idx++)
                list.Add(chain[idx]);
            return chain.ToArray();
        }
        /// <summary>Returns the intervals which constitute the union of the two interval sets.</summary>
        /// <param name="a">Must be in ascending order and not overlapping.</param>
        /// <param name="b">Must be in ascending order and not overlapping.</param>
        internal static IInterval<T>[] Or<T>(IInterval<T>[] a, IInterval<T>[] b) where T : IComparable<T>
        {
            if (a.Length == 0) return b;
            if (b.Length == 0) return a;
            if (a.Length == 1) return Or(a[0], b);
            if (b.Length == 1) return Or(b[0], a);

            // From here, both 'a' and 'b' have length >= 2
            // Blend the two into a single list.
            List<IInterval<T>> sorted = new List<IInterval<T>>(a);
            sorted.AddRange(b);
            sorted.Sort();
            List<IInterval<T>> result = new List<IInterval<T>>();
            result.Add(sorted[0]);
            for (int i = 1; i < sorted.Count; i++)
            {
                IInterval<T>[] union = Or(result[result.Count - 1], sorted[i]);
                result[result.Count - 1] = union[0];
                if (union.Length > 1)
                    result.Add(union[1]);
            }
            return result.ToArray();
        }

        #endregion



        #region Interval SUBTRACT operations

        internal static IInterval<T> Subtract<T>(this Singleton<T> a, IInterval<T> i) where T : IComparable<T>
        {
            if (i.Contains(a.Item)) return new Empty<T>();
            return a;
        }

        internal static IInterval<T>[] Subtract<T>(this Infinite<T> inf, Singleton<T> singleton) where T : IComparable<T>
        {
            int c = inf.Head.CompareTo(singleton.Item);
            if (c < 0)
            {
                if (inf.IsNegative) return inf.AsArray();
                return AsArray(new Limited<T>(inf.Head, inf.IncludeHead, singleton.Item, false),
                                new Infinite<T>(singleton.Item, false, Infinity.Positive));
            }
            if (c > 0)
            {
                if (inf.IsPositive) return inf.AsArray();
                return AsArray(new Infinite<T>(singleton.Item, false, Infinity.Negative),
                                new Limited<T>(singleton.Item, false, inf.Head, inf.IncludeHead));
            }
            if (!inf.IncludeHead) return inf.AsArray();
            return new Infinite<T>(inf.Head, false, inf.Tail).AsArray();
        }
        internal static IInterval<T> Subtract<T>(this Infinite<T> a, Infinite<T> b) where T : IComparable<T>
        {
            int c = a.Head.CompareTo(b.Head);
            if (c > 0)
            {
                if (a.IsPositive)
                {
                    if (b.IsNegative) return a;
                    return new Empty<T>();
                }

                if (b.IsPositive) return new Infinite<T>(b.Head, !b.IncludeHead, Infinity.Negative);
                return new Limited<T>(b.Head, !b.IncludeHead, a.Head, a.IncludeHead);
            }
            else if (c < 0)
            {
                if (a.IsNegative)
                {
                    if (b.IsPositive) return a;
                    return new Empty<T>();
                }

                if (b.IsNegative) return new Infinite<T>(b.Head, !b.IncludeHead, Infinity.Positive);
                return new Limited<T>(a.Head, a.IncludeHead, b.Head, !b.IncludeHead);
            }
            return new Infinite<T>(a.Head, a.IncludeHead && !b.IncludeHead, a.Tail);
        }
        internal static IInterval<T>[] Subtract<T>(this Infinite<T> inf, Limited<T> ltd) where T : IComparable<T>
        {
            if (inf.IsPositive)
            {
                if (ltd.End.LessThan(inf.Head))
                    return inf.AsArray();
                if (ltd.End.EqualTo(inf.Head))
                    return new Infinite<T>(inf.Head, inf.IncludeHead && !ltd.IncludeEnd, Infinity.Positive).AsArray();
                if (ltd.Start.LessThan(inf.Head))
                    return new Infinite<T>(ltd.End, !ltd.IncludeEnd, Infinity.Positive).AsArray();
                if (ltd.Start.EqualTo(inf.Head))
                {
                    if (ltd.IncludeStart || !inf.IncludeHead)
                        return new Infinite<T>(ltd.End, inf.IncludeHead && !ltd.IncludeEnd, Infinity.Positive).AsArray();
                    return AsArray(new Singleton<T>(inf.Head),
                                    new Infinite<T>(ltd.End, !ltd.IncludeEnd, Infinity.Positive));
                }
                return AsArray(new Limited<T>(inf.Head, inf.IncludeHead, ltd.Start, !ltd.IncludeStart),
                                new Infinite<T>(ltd.End, !ltd.IncludeEnd, Infinity.Positive));
            }
            else // if (inf.IsNegative)
            {
                if (ltd.Start.GreaterThan(inf.Head))
                    return inf.AsArray();
                if (ltd.Start.EqualTo(inf.Head))
                    return new Infinite<T>(inf.Head, inf.IncludeHead && !ltd.IncludeEnd, Infinity.Negative).AsArray();
                if (ltd.End.GreaterThan(inf.Head))
                    return new Infinite<T>(ltd.Start, !ltd.IncludeStart, Infinity.Negative).AsArray();
                if (ltd.End.Equals(inf.Head))
                {
                    if (ltd.IncludeEnd || !inf.IncludeHead)
                        return new Infinite<T>(ltd.Start, inf.IncludeHead && !ltd.IncludeStart, Infinity.Negative).AsArray();
                    return AsArray(new Infinite<T>(ltd.Start, !ltd.IncludeStart, Infinity.Negative),
                                    new Singleton<T>(inf.Head));
                }
                return AsArray(new Infinite<T>(ltd.Start, !ltd.IncludeStart, Infinity.Negative),
                                new Limited<T>(ltd.End, !ltd.IncludeEnd, inf.Head, inf.IncludeHead));
            }
        }
        internal static IInterval<T>[] Subtract<T>(this Infinite<T> inf, IInterval<T> i) where T : IComparable<T>
        {
            switch (i)
            {
                case Singleton<T> s2: return Subtract(inf, s2);
                case Limited<T> l2: return Subtract(inf, l2);
                case Infinite<T> i2: return Subtract(inf, i2).AsArray();
                case Universal<T> u2: return new Empty<T>().AsArray();
                case Empty<T> e2: return inf.AsArray();
                default: throw new NotImplementedException();
            }
        }

        internal static IInterval<T>[] Subtract<T>(this Limited<T> ltd, Singleton<T> singleton) where T : IComparable<T>
        {
            if (singleton.Item.LessThan(ltd.Start)) return ltd.AsArray();
            if (singleton.Item.EqualTo(ltd.Start)) return new Limited<T>(ltd.Start, false, ltd.End, ltd.IncludeEnd).AsArray();
            if (singleton.Item.GreaterThan(ltd.End)) return ltd.AsArray();
            if (singleton.Item.EqualTo(ltd.End)) return new Limited<T>(ltd.Start, ltd.IncludeStart, ltd.End, false).AsArray();
            return AsArray(new Limited<T>(ltd.Start, ltd.IncludeStart, singleton.Item, false),
                            new Limited<T>(singleton.Item, false, ltd.End, ltd.IncludeEnd));
        }
        internal static IInterval<T> Subtract<T>(this Limited<T> ltd, Infinite<T> inf) where T : IComparable<T>
        {
            if (inf.IsPositive)
            {
                if (inf.Head.GreaterThan(ltd.End)) return ltd;
                if (inf.Head.EqualTo(ltd.End)) return new Limited<T>(ltd.Start, ltd.IncludeStart, ltd.End, ltd.IncludeEnd && !inf.IncludeHead);
                if (inf.Head.EqualTo(ltd.Start))
                {
                    if (!ltd.IncludeStart || inf.IncludeHead) return new Empty<T>();
                    return new Singleton<T>(ltd.Start);
                }
                if (inf.Head.LessThan(ltd.Start)) return new Empty<T>();
                return new Limited<T>(ltd.Start, ltd.IncludeStart, inf.Head, !inf.IncludeHead);
            }
            else //inf.isNegative
            {
                if (inf.Head.LessThan(ltd.Start)) return ltd;
                if (inf.Head.EqualTo(ltd.Start)) return new Limited<T>(ltd.Start, ltd.IncludeStart && !inf.IncludeHead, ltd.End, ltd.IncludeEnd);
                if (inf.Head.EqualTo(ltd.End))
                {
                    if (!ltd.IncludeEnd || inf.IncludeHead) return new Empty<T>();
                    return new Singleton<T>(ltd.End);
                }
                if (inf.Head.GreaterThan(ltd.End)) return new Empty<T>();
                return new Limited<T>(inf.Head, inf.IncludeHead, ltd.End, ltd.IncludeEnd);
            }
        }
        internal static IInterval<T>[] Subtract<T>(this Limited<T> a, Limited<T> b) where T : IComparable<T>
        {
            if (a.End.LessThan(b.Start)) return a.AsArray();
            if (a.End.EqualTo(b.Start)) return new Limited<T>(a.Start, a.IncludeStart, a.End, a.IncludeEnd && !b.IncludeStart).AsArray();
            if (a.Start.GreaterThan(b.End)) return a.AsArray();
            if (a.Start.EqualTo(b.End)) return new Limited<T>(a.Start, a.IncludeStart && !b.IncludeEnd, a.End, a.IncludeEnd).AsArray();

            List<IInterval<T>> list = new List<IInterval<T>>(2);
            if (a.Start.LessThan(b.Start))
                list.Add(new Limited<T>(a.Start, a.IncludeStart, b.Start, !b.IncludeStart));
            else if (a.Start.EqualTo(b.Start) && a.IncludeStart && !b.IncludeStart)
                list.Add(new Singleton<T>(a.Start));
            if (a.End.GreaterThan(b.End))
                list.Add(new Limited<T>(b.End, !b.IncludeEnd, a.End, a.IncludeEnd));
            else if (a.End.EqualTo(b.End) && a.IncludeEnd && !b.IncludeEnd)
                list.Add(new Singleton<T>(a.End));

            if (list.Count == 0) return new Empty<T>().AsArray();
            return list.ToArray();
        }
        internal static IInterval<T>[] Subtract<T>(this Limited<T> l, IInterval<T> i) where T : IComparable<T>
        {
            switch (i)
            {
                case Singleton<T> s2: return Subtract(l, s2);
                case Limited<T> l2: return Subtract(l, l2);
                case Infinite<T> i2: return Subtract(l, i2).AsArray();
                case Universal<T> u2: return new Empty<T>().AsArray();
                case Empty<T> e2: return l.AsArray();
                default: throw new NotImplementedException();
            }
        }

        /// <summary>Returns the difference of the two intervals.</summary>
        internal static IInterval<T>[] Subtract<T>(this IInterval<T> a, IInterval<T> b) where T : IComparable<T>
        {
            switch (a)
            {
                case Singleton<T> s: return Subtract(s, b).AsArray();
                case Limited<T> l: return Subtract(l, b);
                case Infinite<T> i: return Subtract(i, b);
                case Universal<T> u: return Not(b);
                case Empty<T> e: return e.AsArray();
                default: throw new NotImplementedException();
            }
        }
        /// <summary>Returns the intervals which constitute the difference of the interval less the interval set.</summary>
        /// <param name="chain">Must be in ascending order and not overlapping.</param>
        /// <param name="interval">The interval from which the chain will be removed.</param>
        internal static IInterval<T>[] Subtract<T>(this IInterval<T> interval, IInterval<T>[] chain) where T : IComparable<T>
        {
            List<IInterval<T>> list = new List<IInterval<T>>() { interval };
            foreach (IInterval<T> other in chain)
            {
                IInterval<T>[] diff = interval.Subtract(other);
                interval = diff[0];
                if (diff.Length > 1)
                {
                    list.Add(interval);
                    interval = diff[1];
                }
            }
            return list.ToArray();
        }
        /// <summary>Returns the intervals which constitute the difference of the two interval sets.</summary>
        /// <param name="a">Must be in ascending order and not overlapping.</param>
        /// <param name="b">Must be in ascending order and not overlapping.</param>
        internal static IInterval<T>[] Subtract<T>(IInterval<T>[] a, IInterval<T>[] b) where T : IComparable<T>
        {
            List<IInterval<T>> list = new List<IInterval<T>>();
            int aIdx = 0, bIdx = 0;

            // Find where the overlaps start.
            for (aIdx = 0; aIdx < a.Length; aIdx++)
            {
                IInterval<T> aVal = a[aIdx];                
                bIdx = IndexIn(b, (ival) => !aVal.Equals(aVal.Subtract(ival)[0]));
                if (bIdx < 0) break;
                list.Add(aVal);
            }

            // No overlap?
            if (bIdx < 0) return a;

            // Now find if each item in 'a' is affected by 'b'.
            for (; aIdx < a.Length; aIdx++)
            {
                IInterval<T> aVal = a[aIdx];
                list.Add(aVal);
                IInterval<T>[] diff;
                while (bIdx < b.Length)
                {
                    for (int i = bIdx; i < b.Length; i++)
                    {
                        diff = list[list.Count - 1].Subtract(b[i]);
                        if (diff[0].Equals(list[list.Count - 1]))
                            continue;
                        list[list.Count - 1] = diff[0];
                        if (diff.Length > 1) list.Add(diff[1]);
                        bIdx = i;
                    }
                }                
            }

            // Done.
            return list.ToArray();
            
            int IndexIn( IInterval<T>[] chain, Func<IInterval<T>, bool> func, int startIdx = 0)
            {
                for (int i = startIdx; i < chain.Length; i++)
                    if (func(chain[i])) return i;
                return -1;
            }
        }



        #endregion



        #region Datastructures intervals


        /// <summary>Refers to the infinite direction.</summary>
        internal enum Infinity
        {
            /// <summary>Positive infinity.</summary>
            Positive = 1,
            /// <summary>Negative infinity.</summary>
            Negative = -1
        }

        /// <summary>
        /// An interval with limited inclusion, ranging from the <see cref="Start"/> to the <see cref="End"/> indicated.
        /// </summary>
        internal struct Limited<T> : IInterval<T> where T : IComparable<T>
        {
            /// <summary>The lower limit of this limited interval.</summary>
            public readonly T Start;
            /// <summary>The upper limit of this limited interval.</summary>
            public readonly T End;
            /// <summary>The whether the lower limit is included in this limited interval.</summary>
            public readonly bool IncludeStart;
            /// <summary>The whether the upper limit is included in this limited interval.</summary>
            public readonly bool IncludeEnd;

            /// <summary>Creates a new limited interval with the given start and end.</summary>
            public Limited(T start, T end) : this(start, true, end, true) { }
            /// <summary>Creates a new limited interval with the given start and end.</summary>
            public Limited(T start, bool includeStart, T end, bool includeEnd)
            {
                if (start == null) throw new ArgumentNullException("start");
                if (end == null) throw new ArgumentNullException("end");
                if (start.GreaterThanOrEqualTo(end)) throw new ArgumentException("Argument 'start' must be less than 'end'.");
                this.Start = start;
                this.End = end;
                this.IncludeStart = includeStart;
                this.IncludeEnd = includeEnd;
            }

            /// <summary>Whether the item would fall at or between this limits <see cref="Start"/> and <see cref="End"/>.
            /// </summary>
            public bool Brackets(T item) => item.GreaterThanOrEqualTo(Start) && item.LessThanOrEqualTo(End);

            /// <summary>Whether the item is included in this limited interval.</summary>
            public bool Contains(T item)
            {
                int c = item.CompareTo(Start);
                if (c < 0) return false;
                if (c == 0) return IncludeStart;
                c = item.CompareTo(End);
                if (c > 0) return false;
                if (c == 0) return IncludeEnd;
                return true;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is Limited<T> l)) return false;
                return Start.EqualTo(l.Start) && End.EqualTo(l.End) && IncludeStart == l.IncludeStart && IncludeEnd == l.IncludeEnd;
            }
            public override int GetHashCode() => Start.GetHashCode();
        }

        /// <summary>
        /// An interval with one direction of infinite inclusion, ranging from the <see cref="Head"/> to positive or 
        /// negative infinity.
        /// </summary>
        internal struct Infinite<T> : IInterval<T> where T : IComparable<T>
        {
            /// <summary>The direction where this interval is infinite.</summary>
            public readonly Infinity Tail;

            /// <summary>The finite end of this infinite interval.</summary>
            public readonly T Head;

            /// <summary>Whether the finite end of thie infinite interval is included.</summary>
            public readonly bool IncludeHead;

            /// <summary>Whether this interval is infinite in the positive direction.</summary>
            public bool IsPositive => Tail == Infinity.Positive;
            /// <summary>Whether this interval is infinite in the negative direction.</summary>
            public bool IsNegative => Tail == Infinity.Negative;

            /// <summary>
            /// Creates an infinite interval starting at the indicated <see cref="Head"/>, and extending infinitely in the 
            /// direction indicated.
            /// </summary>
            public Infinite(T head, bool includeHead, Infinity tail) { this.Head = head; this.IncludeHead = includeHead; this.Tail = tail; }

            /// <summary>
            /// Returns whether the given item is equal to the <see cref="Head"/> or would fall in the infinite 
            /// <see cref="Tail"/>.
            /// </summary>
            public bool Brackets(T item)
            {
                int c = item.CompareTo(Head);
                if (c == 0) return true;
                return Math.Sign(c) == Math.Sign((int)Tail);
            }

            /// <summary>
            /// Returns whether the given item is equal an included <see cref="Head"/> or would fall in the infinite 
            /// <see cref="Tail"/>.
            /// </summary>
            public bool Contains(T item)
            {
                int c = item.CompareTo(Head);
                if (c == 0) return IncludeHead;
                return Math.Sign(c) == Math.Sign((int)Tail);
            }


            public bool Brackets(IInterval<T> other)
            {
                
            }

            public bool Contains(IInterval<T> other)
            {
                switch (other)
                {
                    case Singleton<T> s:
                        int c = s.Item.CompareTo(Head);
                        return (c == 0) ? IncludeHead : Math.Sign(c) == Math.Sign((int)Tail);
                    
                }
                
            }
        }


        /// <summary>An interval of a single item.</summary>
        public struct Singleton<T> : IInterval<T> where T : IComparable<T>
        {
            /// <summary>The singular item of this interval.</summary>
            public readonly T Item;

            /// <summary>Creates a new singleton equal to the given item.</summary>
            public Singleton(T item) { this.Item = item; }

            bool IInterval<T>.Brackets(T item) => Item.CompareTo(item) == 0;

            bool IInterval<T>.Contains(T item) => Item.CompareTo(item) == 0;


            bool IInterval<T>.Brackets(IInterval<T> other)
            {
                if (other is Singleton<T> s && Item.EqualTo(s.Item)) return true;
                if (other is Empty<T>) return true;
                return false;
            }

            bool IInterval<T>.Contains(IInterval<T> other)
            {
                if (other is Singleton<T> s && Item.EqualTo(s.Item)) return true;
                if (other is Empty<T>) return true;
                return false;                
            }
        }


        /// <summary>Represents an interval which contains nothing.</summary>
        internal struct Empty<T> : IInterval<T> where T : IComparable<T>
        {
            bool IInterval<T>.Brackets(T item) => false;

            bool IInterval<T>.Contains(T item) => false;

            bool IInterval<T>.Brackets(IInterval<T> other) => false;

            bool IInterval<T>.Contains(IInterval<T> other) => false;
        }


        /// <summary>Represents an interval which contains everything.</summary>
        internal struct Universal<T> : IInterval<T> where T : IComparable<T>
        {
            bool IInterval<T>.Brackets(T item) => true;

            bool IInterval<T>.Contains(T item) => true;


            bool IInterval<T>.Brackets(IInterval<T> other) => true;

            bool IInterval<T>.Contains(IInterval<T> other) => true;
        }

        #endregion

    }


    /// <summary>
    /// Represents all the necessary methods for unary and binary operations on an interval.  Consecutiveness is 
    /// disregarded.
    /// </summary>
    public interface IInterval<T> where T : IComparable<T>
    {
        /// <summary>
        /// Returns whether the item is bracketed by this interval.  Here, "bracketed" means the item would be 
        /// contained in this interval if all inclusions were true.
        /// </summary>
        bool Brackets(T item);

        /// <summary>
        /// Returns whether the item is both bracketed and contained by this interval.  In other words, the item 
        /// cannot be excluded from the interval in any way.
        /// </summary>
        bool Contains(T item);

        bool Brackets(IInterval<T> other);

        bool Contains(IInterval<T> other);
    }





    public interface IIntervalSet<T> where T : IComparable<T>
    {
        bool Includes(T item);

        IIntervalSet<T> FromString(string str);

        IInterval<T>[] GetIntervals();

        bool Add(T item);
        void Add(T from, T to);
        void Clear();
        void ExceptWith(IIntervalSet<T> other);
        void IntersectWith(IIntervalSet<T> other);
        bool IsProperSubsetOf(IIntervalSet<T> other);
        bool IsProperSuperSetOf(IIntervalSet<T> other);
        bool IsSubsetOf(IIntervalSet<T> other);
        bool IsSuperSetOf(IIntervalSet<T> other);
        bool Overlaps(IIntervalSet<T> other);
        bool Remove(T item);
        void Remove(T from, T to);
        void Remove(IIntervalSet<T> other);
        bool SetEquals(IIntervalSet<T> other);
        void SymmetricExceptWith(IIntervalSet<T> other);
        void UnionWith(IIntervalSet<T> other);

    }




    public sealed class ContinuousIntervalSet<T> : IIntervalSet<T> where T : IComparable<T>
    {
        private IInterval<T>[] _Intervals;

        /// <summary>Creates a new, empty interval set.</summary>
        public ContinuousIntervalSet() { this._Intervals = new Empty<T>().AsArray(); }

        /// <summary>Creates a new limited interval set spanning the given values.</summary>
        public ContinuousIntervalSet(T from, T to) { this._Intervals = new Limited<T>(from, true, to, true).AsArray(); }

        /// <summary>Returns a new, empty interval set.</summary>        
        public static ContinuousIntervalSet<T> Empty() => new ContinuousIntervalSet<T>();

        /// <summary>Returns a new, universal interval set.</summary>
        public static ContinuousIntervalSet<T> Universal() => new ContinuousIntervalSet<T>(new Universal<T>().AsArray());

        internal ContinuousIntervalSet(params IInterval<T>[] intervals) { this._Intervals = intervals; }

        /// <summary>Returns a new interval set from the given string.</summary>
        public ContinuousIntervalSet<T> FromString(string str)
        {
            throw new NotImplementedException();
        }

        IIntervalSet<T> IIntervalSet<T>.FromString(string str) => FromString(str);

        /// <summary>Returns whether this interval set includes the given item.</summary>
        public bool Includes(T item) => _Intervals[GetPrecedingOrBracketingIndex(item)].Brackets(item);

        private int GetPrecedingOrBracketingIndex(T item)
        {
            if (_Intervals.Length == 0)
                return -1;
            if (Intervals.FirstInflection(_Intervals[0]).GreaterThan(item))
                return -1;
            int min = 0;
            int max = _Intervals.Length - 1;
            while (min != max)
            {
                int mid = min + max / 2;
                IInterval<T> focus = _Intervals[mid];
                if (focus.Brackets(item)) return mid;
                else if (Intervals.FirstInflection(focus).GreaterThan(item))
                    max = mid;
                else if (Intervals.LastInflection(focus).LessThan(item))
                    min = mid + 1;
            }
            return -1;
        }

        /// <summary>Returns this interval set as a human-readable string.</summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _Intervals.Length - 1; i++)
                sb.Append(_Intervals[i].ToString() + ",");
            sb.Append(_Intervals[_Intervals.Length - 1].ToString());
            return sb.ToString();
        }


        IEnumerable<IInterval<T>> IIntervalSet<T>.GetIntervals() => _Intervals;

        /// <summary>Adds the given item to this set.</summary>
        public bool Add(T item)
        {
            bool result = !_Intervals.Any(i => i.Contains(item));
            _Intervals = new Singleton<T>(item).Or(_Intervals);
            return result;
        }

        /// <summary>Adds the given limited interval to this set.</summary>
        public void Add(T from, T to) => _Intervals = new Limited<T>(from, to).Or(_Intervals);

        /// <summary>Clears all contents from this interval set.</summary>
        public void Clear() => _Intervals = new IInterval<T>[] { new Intervals.Empty<T>() };

        /// <summary>Removes all items in the given other set, from this set.</summary>
        public void ExceptWith(IIntervalSet<T> other) => _Intervals = Subtract<T>(_Intervals, other.GetIntervals());

        /// <summary>Changes this to the set intersection of this set and the given other set.</summary>
        public void IntersectWith(IIntervalSet<T> other) => _Intervals = And<T>(_Intervals, other.GetIntervals());

        bool IIntervalSet<T>.IsProperSubsetOf(IIntervalSet<T> other)
        {
            throw new NotImplementedException();
        }

        bool IIntervalSet<T>.IsProperSuperSetOf(IIntervalSet<T> other)
        {
            throw new NotImplementedException();
        }

        bool IIntervalSet<T>.IsSubsetOf(IIntervalSet<T> other)
        {
            throw new NotImplementedException();
        }

        bool IIntervalSet<T>.IsSuperSetOf(IIntervalSet<T> other)
        {
            throw new NotImplementedException();
        }

        bool IIntervalSet<T>.Overlaps(IIntervalSet<T> other)
        {
            throw new NotImplementedException();
        }

        bool IIntervalSet<T>.Remove(T item)
        {
            throw new NotImplementedException();
        }

        void IIntervalSet<T>.Remove(IIntervalSet<T> other)
        {
            throw new NotImplementedException();
        }

        bool IIntervalSet<T>.SetEquals(IIntervalSet<T> other)
        {
            throw new NotImplementedException();
        }

        void IIntervalSet<T>.SymmetricExceptWith(IIntervalSet<T> other)
        {
            throw new NotImplementedException();
        }

        void IIntervalSet<T>.UnionWith(IIntervalSet<T> other)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class DiscreteIntervalSet<T> //: IIntervalSet<T>, IEnumerable<T>, ISet<T>
    {

    }

}

