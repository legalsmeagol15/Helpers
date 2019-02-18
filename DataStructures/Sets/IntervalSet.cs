using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DataStructures.ConvenienceMethods;

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

        internal static IInterval<T>[] AsArray<T>(this IInterval<T> interval) => new IInterval<T>[] { interval };
        internal static IInterval<T>[] AsArray<T>(params IInterval<T>[] intervals) => intervals;
        
    }

    public interface IIntervalSet<T>
    {
        bool Includes(T item);

       
    }
    public interface IInterval<T>
    {
        bool Brackets(T item);

        bool Contains(T item);

        IInterval<T> And(IInterval<T> other);

        IInterval<T>[] Or(IInterval<T> other);

        IInterval<T>[] Not();
    }


    public sealed class ContinuousIntervalSet<T> : IIntervalSet<T> where T : IComparable<T>
    {
        protected static class Interval
        {
            
            public static readonly IInterval<T> Universal = new Infinite(default(T), true, Infinite.Direction.Universal);
            public static readonly IInterval<T> Empty = new Infinite(default(T), false, Infinite.Direction.Empty);
            public static IInterval<T> AsPositiveInfinite(T head, bool includeHead =true ) => new Infinite(head, includeHead, Infinite.Direction.Positive);
            public static IInterval<T> AsNegativeInfinite(T head, bool includeHead = true) => new Infinite(head, includeHead, Infinite.Direction.Negative);
            public static IInterval<T> AsSingleton(T item) => new Singleton(item);
            public static IInterval<T> FromTo(T start, T end) => FromTo(start, true, end, true);
            public static IInterval<T> FromTo(T start, bool includeStart, T end, bool includeEnd)
            {
                int c = start.CompareTo(end);
                if (c > 0)
                    throw new ArgumentException("Parameters 'start' and 'end' are inverted.");
                else if (c == 0)
                {
                    if (!includeStart || !includeEnd)
                        throw new ArgumentException("For singletons, both 'start' and 'end' must be included.");
                    return AsSingleton(start);
                }
                else
                    return Interval.FromTo(start, includeStart, end, includeEnd);
            }


            #region Interval AND operations

            internal static IInterval<T> And(Singleton a, Singleton b) => (a.Item.CompareTo(b.Item) == 0) ? a : Interval.Empty;
            internal static IInterval<T> And(Singleton singleton, Infinite inf)
            {
                if (inf.IsUniversal) return singleton;
                if (inf.IsEmpty) return Interval.Empty;
                int c = singleton.Item.CompareTo(inf.Head);
                if (c == 0)
                    return inf.IncludeHead ? singleton : Interval.Empty;
                return ((c < 0) && (inf.Tail == Infinite.Direction.Negative)) ? singleton : Interval.Empty;
            }
            internal static IInterval<T> And(Singleton singleton, Limited ltd)
            {
                int c = singleton.Item.CompareTo(ltd.Start);
                if (c < 0) return Interval.Empty;
                if (c == 0) return ltd.IncludeStart ? singleton : Interval.Empty;
                c = singleton.Item.CompareTo(ltd.End);
                if (c > 0) return Interval.Empty;
                if (c == 0) return ltd.IncludeEnd ? singleton : Interval.Empty;
                return singleton;
            }

            internal static IInterval<T> And(Infinite inf, Singleton singleton) => And(singleton, inf);
            internal static IInterval<T> And(Infinite a, Infinite b)
            {
                if (a.IsUniversal) return b;
                if (b.IsUniversal) return a;
                if (a.IsEmpty || b.IsEmpty) return Interval.Empty;

                int c = a.Head.CompareTo(b.Head);
                if (c == 0)
                {
                    if (a.Tail != b.Tail)
                    {
                        if (a.IncludeHead && b.IncludeHead) return AsSingleton(a.Head);
                        return Interval.Empty;
                    }
                    return new Infinite(a.Head, a.IncludeHead && b.IncludeHead, a.Tail);
                }
                if (c < 0)
                {
                    if (a.Tail == Infinite.Direction.Positive)
                    {
                        if (b.Tail == Infinite.Direction.Negative)
                            return Interval.FromTo(a.Head, a.IncludeHead, b.Head, b.IncludeHead);
                        return b;
                    }
                    else if (b.Tail == Infinite.Direction.Negative) return a;
                    return Interval.Empty;
                }
                else
                {
                    if (a.Tail == Infinite.Direction.Positive)
                    {
                        if (b.Tail == Infinite.Direction.Positive) return b;
                        return Interval.Empty;
                    }
                    else if (b.Tail == Infinite.Direction.Positive)
                        return Interval.FromTo(b.Head, b.IncludeHead, a.Head, a.IncludeHead);
                    return Interval.Empty;
                }

            }
            internal static IInterval<T> And(Infinite inf, Limited ltd)
            {
                if (inf.IsUniversal) return ltd;
                if (inf.IsEmpty) return Interval.Empty;
                int c = inf.Head.CompareTo(ltd.Start);
                if (c < 0)
                {
                    if (inf.Tail == Infinite.Direction.Negative) return Interval.Empty;
                    return ltd;
                }
                else if (c == 0)
                    return Interval.FromTo(inf.Head, inf.IncludeHead && ltd.IncludeStart, ltd.End, ltd.IncludeEnd);
                else if ((c = inf.Head.CompareTo(ltd.End)) > 0)
                {
                    if (inf.Tail == Infinite.Direction.Positive) return Interval.Empty;
                    return ltd;
                }
                else if (c == 0)
                    return Interval.FromTo(ltd.Start, ltd.IncludeStart, inf.Head, inf.IncludeHead && ltd.IncludeEnd);
                else if (inf.Tail == Infinite.Direction.Positive)
                    return Interval.FromTo(inf.Head, inf.IncludeHead, ltd.End, ltd.IncludeEnd);
                else
                    return Interval.FromTo(ltd.Start, ltd.IncludeStart, inf.Head, inf.IncludeHead);

            }

            internal static IInterval<T> And(Limited ltd, Singleton singleton) => And(singleton, ltd);
            internal static IInterval<T> And(Limited ltd, Infinite inf) => And(inf, ltd);
            internal static IInterval<T> And(Limited a, Limited b)
            {
                int c;
                if ((c = a.Start.CompareTo(b.Start)) > 0) return And(b, a);
                else if ((c = a.End.CompareTo(b.Start)) < 0) return Interval.Empty;
                else if (c == 0) return (a.IncludeEnd && b.IncludeStart) ? AsSingleton(a.End) : Interval.Empty;
                else if ((c = a.Start.CompareTo(b.Start)) == 0) return (a.End.CompareTo(b.End) <= 0) ? a : b;
                else if ((c = a.End.CompareTo(b.End)) < 0) return Interval.FromTo(b.Start, b.IncludeStart, a.End, a.IncludeEnd);
                else if (c == 0) return Interval.FromTo(b.Start, b.IncludeStart, a.End, a.IncludeEnd && b.IncludeEnd);
                else return b;
            }

            #endregion



            #region Interval OR operations

            internal static IInterval<T>[] Or(Singleton a, Singleton b) => (a.Item.CompareTo(b.Item) == 0) ? a.AsArray() : AsArray(a, b);
            internal static IInterval<T>[] Or(Singleton singleton, Infinite inf)
            {
                if (inf.IsUniversal) return Universal.AsArray();
                if (inf.IsEmpty) return singleton.AsArray();
                int c = singleton.Item.CompareTo(inf.Head);
                if (c < 0)
                {
                    if (inf.Tail == Infinite.Direction.Negative) return inf.AsArray();
                    return AsArray(singleton, inf);
                }
                else if (c > 0)
                {
                    if (inf.Tail == Infinite.Direction.Positive) return inf.AsArray();
                    return AsArray(inf, singleton);
                }
                return new Infinite(singleton.Item, true, inf.Tail).AsArray();

            }
            internal static IInterval<T>[] Or(Singleton singleton, Limited ltd)
            {
                int c;
                if ((c = singleton.Item.CompareTo(ltd.Start)) < 0) return AsArray(singleton, ltd);
                else if (c == 0) return Interval.FromTo(singleton.Item, true, ltd.End, ltd.IncludeEnd).AsArray();
                else if ((c = singleton.Item.CompareTo(ltd.End)) > 0) return AsArray(ltd, singleton);
                else if (c == 0) return Interval.FromTo(ltd.Start, ltd.IncludeStart, singleton.Item, true).AsArray();
                return ltd.AsArray();
            }

            internal static IInterval<T>[] Or(Infinite inf, Singleton singleton) => Or(singleton, inf);
            internal static IInterval<T>[] Or(Infinite a, Infinite b)
            {
                if (a.IsUniversal || b.IsUniversal) return Universal.AsArray();
                if (a.IsEmpty) return b.AsArray();
                if (b.IsEmpty) return a.AsArray();
                int c = a.Head.CompareTo(b.Head);
                if (c < 0)
                {
                    if (a.Tail == Infinite.Direction.Positive) return (a.Tail == b.Tail) ? a.AsArray() : Universal.AsArray();
                    else return (a.Tail == b.Tail) ? b.AsArray() : AsArray(a, b);
                }
                else if (c > 0)
                {
                    if (a.Tail == Infinite.Direction.Negative) return (a.Tail == b.Tail) ? a.AsArray() : Universal.AsArray();
                    else return (a.Tail == b.Tail) ? b.AsArray() : AsArray(b, a);
                }
                else if (a.Tail == b.Tail)
                    return new Infinite(a.Head, a.IncludeHead || b.IncludeHead, a.Tail).AsArray();
                else if (a.IncludeHead || b.IncludeHead)
                    return Universal.AsArray();
                else
                    return AsArray(new Infinite(a.Head, false, Infinite.Direction.Negative),
                                    new Infinite(a.Head, false, Infinite.Direction.Positive));
            }
            internal static IInterval<T>[] Or(Infinite inf, Limited ltd)
            {
                if (inf.IsUniversal) return Universal.AsArray();
                if (inf.IsEmpty) return ltd.AsArray();
                int c;
                if ((c = inf.Head.CompareTo(ltd.Start)) < 0)
                {
                    if (inf.Tail == Infinite.Direction.Negative) return AsArray(inf, ltd);
                    return inf.AsArray();
                }
                else if (c == 0)
                {
                    if (inf.Tail == Infinite.Direction.Negative)
                        return (inf.IncludeHead || ltd.IncludeStart) ? new Infinite(ltd.End, ltd.IncludeEnd, Infinite.Direction.Negative).AsArray()
                                                                        : AsArray(inf, ltd);
                    return new Infinite(inf.Head, inf.IncludeHead || ltd.IncludeStart, inf.Tail).AsArray();
                }
                else if ((c = inf.Head.CompareTo(ltd.End)) > 0)
                {
                    if (inf.Tail == Infinite.Direction.Positive) return AsArray(ltd, inf);
                    return inf.AsArray();
                }
                else if (c == 0)
                {
                    if (inf.Tail == Infinite.Direction.Positive)
                        return (inf.IncludeHead || ltd.IncludeEnd) ? new Infinite(ltd.Start, ltd.IncludeStart, Infinite.Direction.Positive).AsArray()
                                                                        : AsArray(ltd, inf);
                    return new Infinite(inf.Head, inf.IncludeHead || ltd.IncludeEnd, inf.Tail).AsArray();
                }
                else if (inf.Tail == Infinite.Direction.Positive)
                    return new Infinite(ltd.Start, ltd.IncludeStart, inf.Tail).AsArray();
                else
                    return new Infinite(ltd.End, ltd.IncludeEnd, inf.Tail).AsArray();
            }

            internal static IInterval<T>[] Or(Limited ltd, Singleton singleton) => Or(singleton, ltd);
            internal static IInterval<T>[] Or(Limited ltd, Infinite inf) => Or(inf, ltd);
            internal static IInterval<T>[] Or(Limited a, Limited b)
            {
                int c;
                if ((c = a.Start.CompareTo(b.Start)) > 0) return Or(b, a);
                else if (c == 0)
                {
                    if ((c = a.End.CompareTo(b.End)) < 0) return Interval.FromTo(a.Start, a.IncludeStart || b.IncludeStart, b.End, b.IncludeEnd).AsArray();
                    else if (c > 0) return Interval.FromTo(a.Start, a.IncludeStart || b.IncludeStart, a.End, a.IncludeEnd).AsArray();
                    return Interval.FromTo(a.Start, a.IncludeStart || b.IncludeStart, a.End, a.IncludeEnd || b.IncludeEnd).AsArray();
                }
                else if ((c = a.End.CompareTo(b.Start)) < 0) return AsArray(a, b);
                else if (c == 0) return (a.IncludeEnd || b.IncludeStart) ? Interval.FromTo(a.Start, a.IncludeStart, b.End, b.IncludeEnd).AsArray()
                                                                            : AsArray(a, b);
                else if ((c = a.End.CompareTo(b.End)) < 0) return Interval.FromTo(a.Start, a.IncludeStart, b.End, b.IncludeEnd).AsArray();
                else if (c == 0) return Interval.FromTo(a.Start, a.IncludeStart, a.End, a.IncludeEnd || b.IncludeEnd).AsArray();
                else return Interval.FromTo(a.Start, a.IncludeStart, a.End, a.IncludeEnd).AsArray();
            }
            
            #endregion



            #region Interval NOT operations 

            internal static IInterval<T>[] Not(Singleton singleton)
                => AsArray(new Infinite(singleton.Item, false, Infinite.Direction.Negative),
                            new Infinite(singleton.Item, false, Infinite.Direction.Positive));
            internal static IInterval<T>[] Not(Infinite inf)
            {
                if (inf.IsUniversal) return Universal.AsArray();
                if (inf.IsEmpty) return Empty.AsArray();
                return new Infinite(inf.Head, !inf.IncludeHead, ~inf.Tail).AsArray();
            }
            internal static IInterval<T>[] Not(Limited ltd) 
                => AsArray(new Infinite(ltd.Start, !ltd.IncludeStart, Infinite.Direction.Negative),
                            new Infinite(ltd.End, !ltd.IncludeEnd, Infinite.Direction.Positive));

            internal static IInterval<T>[] Not(params IInterval<T>[] other)
            {
                switch (other.Length)
                {
                    case 0: return Universal.AsArray();
                    case 1: switch (other[0])
                        {
                            case Singleton singleton: return Not(singleton);
                            case Infinite inf: return Not(inf);
                            case Limited ltd: return Not(ltd);
                            default: throw new NotImplementedException();
                        }
                }

                List<IInterval<T>> intervals = new List<IInterval<T>>();
                T lastInflection;
                bool lastIncluded;
                IInterval<T> lastInterval = other[0];
                switch (lastInterval)
                {
                    case Singleton singleton:
                        lastInflection = singleton.Item;
                        lastIncluded = true;
                        intervals.Add(new Infinite(singleton.Item, false, Infinite.Direction.Negative));
                        break;
                    case Infinite inf:
                        lastInflection = inf.Head;
                        lastIncluded = !inf.IncludeHead;
                        if (inf.Tail != Infinite.Direction.Negative)
                            intervals.Add(new Infinite(inf.Head, !inf.IncludeHead, Infinite.Direction.Negative));
                        break;
                    case Limited ltd:
                        lastInflection = ltd.End;
                        lastIncluded = !ltd.IncludeEnd;
                        intervals.Add(new Infinite(ltd.Start, ltd.IncludeStart, Infinite.Direction.Negative));
                        break;
                    default: throw new NotImplementedException();
                }

                for (int i = 1; i < other.Length; i++)
                {
                    IInterval<T> thisInterval = other[i];

                }


                // TODO:  finish here.
                throw new NotImplementedException();
                
                
                

            }
            #endregion



            internal struct Limited : IInterval<T>
            {
                public readonly T Start;
                public readonly T End;
                public readonly bool IncludeStart;
                public readonly bool IncludeEnd;
                internal Limited(T start, bool includeStart, T end, bool includeEnd)
                {
                    if (start == null || end == null)
                        throw new ArgumentNullException("Parameters 'start' and 'end' cannot be null.");
                    this.Start = start;
                    this.End = end;
                    this.IncludeStart = includeStart;
                    this.IncludeEnd = includeEnd;
                }

                public IInterval<T> And(IInterval<T> other)
                {
                    switch (other)
                    {
                        case Singleton s: return Interval.And(this, s);
                        case Infinite i:return Interval.And(this, i);
                        case Limited l: return Interval.And(this, l);
                        default: throw new NotImplementedException();
                    }
                }

                bool IInterval<T>.Brackets(T item) => Start.CompareTo(item) <= 0 && item.CompareTo(End) <= 0;

                bool IInterval<T>.Contains(T item)
                {
                    int c = Start.CompareTo(item);
                    if (c > 0) return false;
                    if (c == 0) return IncludeStart;
                    c = End.CompareTo(item);
                    if (c < 0) return false;
                    if (c == 0) return IncludeEnd;
                    return true;
                }

                public IInterval<T>[] Not() => Interval.Not(this);

                public IInterval<T>[] Or(IInterval<T> other)
                {
                    switch (other)
                    {
                        case Singleton s: return Interval.Or(this, s);
                        case Infinite i: return Interval.Or(this, i);
                        case Limited l: return Interval.Or(this, l);
                        default: throw new NotImplementedException();
                    }
                }
            }

            internal struct Infinite : IInterval<T>
            {
                public enum Direction { Empty = 0, Positive = 1, Negative = 2, Universal = 3 }
                public readonly T Head;
                public readonly Direction Tail;
                public readonly bool IncludeHead;
                internal Infinite(T head, bool includeHead, Direction tail)
                {
                    if (head == null)
                        throw new ArgumentNullException("Parameter 'head' cannot be null.");
                    this.Head = head;
                    this.IncludeHead = includeHead;
                    this.Tail = tail;
                }
                public bool IsUniversal => Tail == Direction.Universal;
                public bool IsPositive => (Tail & Direction.Positive) > Direction.Empty;
                public bool IsNegative => (Tail & Direction.Negative) > Direction.Empty;
                public bool IsEmpty => Tail == Direction.Empty;

                public bool Brackets(T item)
                {
                    int c = item.CompareTo(item);
                    if (c < 0) return this.Tail == Direction.Negative;
                    else if (c > 0) return this.Tail == Direction.Positive;
                    return true;
                }

                public bool Contains(T item)
                {
                    int c = item.CompareTo(item);
                    if (c < 0) return this.Tail == Direction.Negative;
                    else if (c > 0) return this.Tail == Direction.Positive;
                    return IncludeHead;
                }

                public IInterval<T> And(IInterval<T> other)
                {
                    switch (other)
                    {
                        case Singleton s: return Interval.And(this, s);
                        case Infinite i: return Interval.And(this, i);
                        case Limited l: return Interval.And(this, l);
                        default: throw new NotImplementedException();
                    }
                }

                public IInterval<T>[] Or(IInterval<T> other)
                {
                    switch (other)
                    {
                        case Singleton s: return Interval.Or(this, s);
                        case Infinite i: return Interval.Or(this, i);
                        case Limited l: return Interval.Or(this, l);
                        default: throw new NotImplementedException();
                    }
                }

                IInterval<T>[] IInterval<T>.Not() => Interval.Not(this);
            }

            internal struct Singleton : IInterval<T>
            {
                public readonly T Item;
                public Singleton(T item)
                {
                    if (item == null)
                        throw new ArgumentNullException("Parameter 'item' and 'end' cannot be null.");
                    this.Item = item;
                }

                public IInterval<T> And(IInterval<T> other)
                {
                    switch (other)
                    {
                        case Singleton s: return Interval.And(this, s);
                        case Infinite i: return Interval.And(this, i);
                        case Limited l: return Interval.And(this, l);
                        default: throw new NotImplementedException();
                    }
                }

                bool IInterval<T>.Brackets(T item) => Item.CompareTo(item) == 0;

                bool IInterval<T>.Contains(T item) => Item.CompareTo(item) == 0;

                public IInterval<T>[] Not() => Interval.Not(this);

                public IInterval<T>[] Or(IInterval<T> other)
                {
                    switch (other)
                    {
                        case Singleton s: return Interval.Or(this, s);
                        case Infinite i: return Interval.Or(this, i);
                        case Limited l: return Interval.Or(this, l);
                        default: throw new NotImplementedException();
                    }
                }
            }

        }

        
    }

    public sealed class DiscreteIntervalSet<T> : IIntervalSet<T>, IEnumerable<T>, ISet<T>
    {

    }
    
}
