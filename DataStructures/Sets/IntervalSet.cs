using Dependency;
using Mathematics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{
    /// <summary>Describes an interval set, which is a loss-tolerant data inclusion/exclusion set.</summary>
    public interface IIntervalSet<T> : Mathematics.ITrueSet<T>, ICollection<T> where T : IComparable<T>
    {
        bool IsPositiveInfinite { get; }
        
        bool IsNegativeInfinite { get; }

        void MakeUniversal();
        void MakeEmpty();
        void MakePositiveInfinite();
        void MakeNegativeInfinite();
    }


    /// <summary>An abstract interval set of the given type.</summary>
    public abstract class IntervalSet<T> : IIntervalSet<T> where T : IComparable<T>
    {
        /// <summary>The set of inflection points for this interval set.  This must be guaranteed to be in strictly 
        /// ascending order at all times.</summary>
        protected internal Inflection[] Inflections { get; internal set; }

        /// <summary>Creates a new <see cref="IntervalSet{T}"/> with the given inflection points.  The inflections 
        /// should be in strictly ascending order - this constructor does not check.</summary>
        /// <param name="inflections">Optional.  If omitted, creates an empty <see cref="IntervalSet{T}"/>.</param>
        [DebuggerStepThrough]
        protected IntervalSet(params Inflection[] inflections) { Inflections = inflections ?? new Inflection[0]; }
        /// <summary>Creates a new <see cref="IntervalSet{T}"/> with the given item contents included as singletons.
        /// </summary>
        /// <param name="items">Optional.  If omitted, creates an empty <see cref="IntervalSet{T}"/>.</param>
        [DebuggerStepThrough]
        protected IntervalSet(IEnumerable<T> items = null) : this((Inflection[])null)
        {
            if (items == null || !items.Any()) return;

            // As long as I'm copying the array, might as well check for strict ascendance too.
            T lastItem = items.First();
            this.Inflections = new Inflection[items.Count()];
            this.Inflections[0] = Inflection.Singleton(lastItem);
            int idx = 1;
            foreach (T item in items.Skip(1))
            {
                if (item.CompareTo(lastItem) <= 0)
                    throw new ArgumentException("Items must be in strictly ascending order.");
                Inflections[idx++] = Inflection.Singleton(item);
                lastItem = item;
            }
        }

        /// <summary>Returns whether this <see cref="IntervalSet{T}"/> represents an empty set.</summary>
        public bool IsEmpty => Inflections.Length == 0;
        /// <summary>Returns whether this <see cref="IntervalSet{T}"/> represents an universal set.</summary>
        public bool IsUniversal => Inflections.Length == 1 && Inflections[0].IsUniversal;
        /// <summary>Returns whether this <see cref="IntervalSet{T}"/> represents a positive-infinite set.</summary>
        public bool IsPositiveInfinite => Inflections.Length > 0 && Inflections[Inflections.Length - 1].IsStart;
        /// <summary>Returns whether this <see cref="IntervalSet{T}"/> represents a negative-infinite set.</summary>
        public bool IsNegativeInfinite => Inflections.Length > 0 && Inflections[0].IsEnd;

        ITrueSet<T> ITrueSet<T>.And(ITrueSet<T> other) => throw new NotImplementedException();
        ITrueSet<T> ITrueSet<T>.Or(ITrueSet<T> other) => throw new NotImplementedException();
        ITrueSet<T> ITrueSet<T>.Not() => throw new NotImplementedException();

        /// <summary>Returns whether the given item is included in this <see cref="IntervalSet{T}"/>.</summary>
        public bool Contains(T item)
        {
            int idx = GetPrecedingIndex(item);
            if (idx < 0)
            {
                if (Inflections.Length == 0) return false;
                return Inflections[0].HasBefore;
            }
            Inflection inf = Inflections[idx];
            if (item.CompareTo(inf.Point) == 0) return inf.IsIncluded;
            return inf.HasAfter;
        }

        private int GetPrecedingIndex (T item)
        {
            if (Inflections.Length == 0) return -1;
            if (Inflections[0].IsUniversal) return 0;
            if (item.CompareTo( Inflections[0].Point) <0) return -1;

            // Get the inflection at or preceding the given item.
            int min = 0, max = Inflections.Length;
            int mid = 0, oldMid = mid;
            int c;
            Inflection inf;
            while (true)
            {
                mid = (min + max) / 2;
                inf = Inflections[mid];
                c = item.CompareTo(inf.Point);
                if (c > 0)
                {
                    if (mid == oldMid) break;
                    min = mid;
                }
                else if (c < 0)
                    max = mid + (oldMid == mid ? 0 : 1);
                else
                    break;
                if (min >= max) break;
                oldMid = mid;
            }
            return mid;
        }

        /// <summary>Returns the inflection points of this <see cref="IntervalSet{T}"/>.</summary>
        public virtual IEnumerable<T> GetInflections() { foreach (Inflection f in Inflections) yield return f.Point; }

        

        #region IntervalSet in-place contents manipulation
        /// <summary>Adds the given singleton to this <see cref="IntervalSet{T}"/>.</summary>
        public bool Add(T item)
        {
            // TODO:  this could be more efficient.
            if (Contains(item)) return false;
            Add(item, true, item, true);
            return true;
        }
        void ICollection<T>.Add(T item) => Add(item);
        /// <summary>Unions with the given range to this <see cref="IntervalSet{T}"/>.</summary>
        /// <param name="start">The start of the range, inclusive.</param>
        /// <param name="end">The end of the range, inclusive.</param>
        public void Add(T start, T end) => Add(start, true, end, true);
        /// <summary>Unions with the given range to this <see cref="IntervalSet{T}"/>.</summary>
        public void Add(T start, bool includeStart, T end, bool includeEnd)
        {
            Inflections = Or(Inflections, new Inflection[] { Inflection.Start(start, includeStart),
                                                                 Inflection.End(end, includeEnd) });
        }

        /// <summary>Removes the given range of items.</summary>
        public bool Remove(T item)
        {
            // TODO:  this could be more efficient.
            if (!Contains(item)) return false;
            Remove(item, true, item, false);
            return true;
        }
        /// <summary>Removes the given range of items.</summary>
        public void Remove(T start, T end) => Remove(start, true, end, true);
        /// <summary>Removes the given range of items.</summary>
        public void Remove(T start, bool removeStart, T end, bool removeEnd)
        {
            Inflections = Subtract(Inflections, new Inflection[] { Inflection.Start(start, removeStart),
                                                                    Inflection.End(end, removeEnd) });
        }
        public void Clear() => MakeEmpty();

        /// <summary>Makes this set an universal <see cref="IntervalSet{T}"/>.</summary>
        public void MakeUniversal() => Inflections = new Inflection[] { Inflection.Universal };
        /// <summary>Makes this set an empty <see cref="IntervalSet{T}"/>.</summary>
        public void MakeEmpty() => Inflections = new Inflection[0];
        /// <summary>Unions this interval with a positive-infinite set, starting as indicated.</summary>
        public void MakePositiveInfinite()
        {
            if (Inflections.Length == 0)
                Inflections = new Inflection[] { Inflection.Universal };  
            else
            {
                Inflection inf = Inflections[Inflections.Length - 1];
                Inflections[Inflections.Length - 1] = Inflection.Compose(inf.Point, inf.HasBefore, inf.IsIncluded, true);
            }


        }
        /// <summary>Unions this interval with a negative-infinite set, ending as indicated.</summary>
        public void MakeNegativeInfinite()
        {
            if (Inflections.Length == 0)
                Inflections = new Inflection[] { Inflection.Universal };
            else
            {
                Inflection inf = Inflections[0];
                Inflections[0] = Inflection.Compose(inf.Point, true, inf.IsIncluded, inf.HasAfter);
            }
            

        }

        #endregion



        #region IntervalSet inflection logic

        /// <summary>Returns a set-equal copy of this <see cref="IntervalSet{T}"/>.</summary>
        public virtual IntervalSet<T> Copy()
        {
            IntervalSet<T> copy = (IntervalSet<T>)Activator.CreateInstance(this.GetType());
            copy.Inflections = this.Inflections.ToArray();
            return copy;
        }
        

        /// <summary>Returns the inverse of the given <see cref="Inflection"/> array.</summary>
        protected virtual Inflection[] Not(Inflection[] inflections)
        {
            if (IsUniversal) return new Inflection[0];
            if (IsEmpty) return new Inflection[] { Inflection.Universal };
            return inflections.Select(s => Inflection.Not(s)).ToArray();
        }


        /// <summary>
        /// Returns <see cref="Inflection"/>s representing the operation of the given <paramref name="logic"/> 
        /// operations on <paramref name="a"/> and <paramref name="b"/>.
        /// </summary>
        /// <param name="a">The first set on which to perform logical operations.</param>
        /// <param name="b">The second set on which to perform logical operations.</param>
        /// <param name="logic">Returns true if and only if, operating on the bool given from <paramref name="a"/> and 
        /// the bool given from <paramref name="b"/>, the result would be included.</param>
        /// <returns></returns>
        protected IList<Inflection> ApplyFunction(IList<Inflection> a, IList<Inflection> b, Func<bool, bool, bool> logic)
        {
            int aIdx = 0, bIdx = 0;
            Inflection aInf = Inflection.Empty, bInf = Inflection.Empty;
            List<Inflection> list = new List<Inflection>();
            bool anyUniversal = false;
            while (aIdx < a.Count && bIdx < b.Count)
            {
                aInf = a[aIdx];
                bInf = b[bIdx];
                if (aInf.IsBefore(bInf))
                {
                    Inflection inf = Inflection.Compose(aInf.Point, logic(aInf.HasBefore, bInf.HasBefore),
                                                                    logic(aInf.IsIncluded, bInf.HasBefore),
                                                                    logic(aInf.HasAfter, bInf.HasBefore));
                    _Append(inf);
                    aIdx++;
                }
                else if (bInf.IsBefore(aInf))
                {
                    Inflection inf = Inflection.Compose(bInf.Point, logic(aInf.HasBefore, bInf.HasBefore),
                                                                    logic(aInf.HasBefore, bInf.IsIncluded),
                                                                    logic(aInf.HasBefore, bInf.HasAfter));
                    _Append(inf);
                    bIdx++;
                }
                else
                {
                    Inflection inf = Inflection.Compose(aInf.Point, logic(aInf.HasBefore, bInf.HasBefore),
                                                                    logic(aInf.IsIncluded, bInf.IsIncluded),
                                                                    logic(aInf.HasAfter, bInf.HasAfter));
                    _Append(inf);
                    aIdx++;
                    bIdx++;
                }


            }
            while (aIdx < a.Count)
            {
                aInf = a[aIdx++];
                Inflection inf = Inflection.Compose(aInf.Point, logic(aInf.HasBefore, bInf.HasAfter),
                                                                logic(aInf.IsIncluded, bInf.HasAfter),
                                                                logic(aInf.HasAfter, bInf.HasAfter));
                _Append(inf);
            }
            while (bIdx < b.Count)
            {
                bInf = b[bIdx++];
                Inflection inf = Inflection.Compose(bInf.Point, logic(aInf.HasAfter, bInf.HasBefore),
                                                                logic(aInf.HasAfter, bInf.IsIncluded),
                                                                logic(aInf.HasAfter, bInf.HasAfter));
                _Append(inf);
            }

            if (list.Count == 0 && anyUniversal)
                list.Add(Inflection.Universal);
            return list;

            void _Append(Inflection inf)
            {
                if (inf.IsEmpty) return;
                if (inf.IsUniversal) anyUniversal = true;
                else list.Add(inf);
            }
        }


        /// <summary>Returns the intersection of the given inflection arrays.</summary>
        protected virtual Inflection[] And(Inflection[] a, Inflection[] b)
            => ApplyFunction(a, b, (aOn, bOn) => aOn && bOn).ToArray();
        /// <summary>Returns the union of the two <see cref="Inflection"/> arrays.</summary>
        protected virtual Inflection[] Or(Inflection[] a, Inflection[] b)
            => ApplyFunction(a, b, (aOn, bOn) => aOn || bOn).ToArray();
        protected virtual Inflection[] Imply(Inflection[] a, Inflection[] b)
            => ApplyFunction(a, b, (aOn, bOn) => (!aOn || bOn)).ToArray();
        /// <summary>Returns the set difference of the two <see cref="Inflection"/> arrays.</summary>
        protected virtual Inflection[] Subtract(Inflection[] a, Inflection[] b)
            => ApplyFunction(a, b, (aOn, bOn) => (aOn && !bOn)).ToArray();
        /// <summary>Returns the exclusive-or (symmetric exception with) of the two <see cref="Inflection"/> arrays.
        /// </summary>
        protected virtual Inflection[] Xor(Inflection[] a, Inflection[] b)
            => ApplyFunction(a, b, (aOn, bOn) => (aOn ^ bOn)).ToArray();

        #endregion

        

        /// <summary>Returns interval set equality.</summary>
        public static bool operator ==(IntervalSet<T> a, IntervalSet<T> b)
        {
            if (a.GetType() != b.GetType()) return false;
            if (a.Inflections.Length != b.Inflections.Length) return false;
            for (int i = 0; i < a.Inflections.Length; i++)
                if (!a.Inflections[i].Equals(b.Inflections[i])) return false;
            return true;
        }
        /// <summary>Returns interval set inequality.</summary>
        public static bool operator !=(IntervalSet<T> a, IntervalSet<T> b) => !(a == b);

        /// <summary>
        /// Returns whether the given object is a matching <see cref="IntervalSet{T}"/> with identical inclusions and 
        /// exclusions.
        /// </summary>
        public override sealed bool Equals(object obj) => (obj is IntervalSet<T> other && this == other);

        /// <summary>The hashcode is based on the hash codes of the first and last inflection points in the set.
        /// </summary>
        public override sealed int GetHashCode()
        {
            if (Inflections.Length == 0) return 0;
            unchecked
            {
                return Inflections[0].Point.GetHashCode() + Inflections[Inflections.Length - 1].Point.GetHashCode();
            }
        }

        /// <summary>Returns a string representation of this <see cref="IntervalSet{T}"/>.</summary>
        public sealed override string ToString()
        {
            if (Inflections.Length == 0) return "..";
            Inflection first = Inflections[0];
            Inflection last = Inflections[Inflections.Length - 1];
            StringBuilder sb = new StringBuilder();
            sb.Append((first.IsEnd || (first.IsSingleton && !first.IsIncluded)) ? "<." : "");
            for (int i = 0; i < Inflections.Length-1; i++)
            {
                Inflection inf = Inflections[i];
                sb.Append(inf.ToString());
                if (inf.IsEnd || inf.IsSingleton && inf.IsIncluded) sb.Append(",");
            }
            sb.Append(last.ToString());
            sb.Append((last.IsStart || (last.IsSingleton && !last.IsIncluded)) ? ".>" : "");
            return sb.ToString();            
        }

        /// <summary>Marks a change in an interval set between inclusion and exclusion, or vice versa.</summary>
        protected internal struct Inflection
        {
            /// <summary>These values and the masks are all calculated at compile time.  Weeee!</summary>
            private enum Flags
            {
                ERROR = 0x0,
                POINT = 0x1,
                START = 0x2,
                END = 0x4,
                SINGLETON = 0x8,  // TODO:  this could be cut out for efficiency.
                UNIVERSAL = 0x10, // TODO:  this could be cut out for efficiency.
                UNVERSAL_MASK = START | POINT | END,
                HAS_BEFORE_MASK = (UNIVERSAL | END),
                HAS_POINT_MASK = (UNIVERSAL | POINT),
                HAS_AFTER_MASK = (UNIVERSAL | START),
                IS_INCLUDED_MASK = (UNIVERSAL | POINT)
            }
            private readonly Flags _Flags;

            /// <summary>A universal inflection.</summary>
            public static readonly Inflection Universal = new Inflection(default(T), Flags.UNIVERSAL);

            /// <summary>An empty inflection.  Used only for signaling and composition.</summary>
            internal static readonly Inflection Empty = new Inflection(default(T), Flags.ERROR);

            /// <summary>The point in the universe marked by this <see cref="Inflection"/>.</summary>
            public readonly T Point;


            /// <summary>Returns whether the <see cref="Point"/> is included.</summary>
            public bool IsIncluded => (_Flags & Flags.IS_INCLUDED_MASK) != Flags.ERROR;
            /// <summary>Returns whether the <see cref="Point"/> is an inclusion start.</summary>
            public bool IsStart => (_Flags & Flags.START) != Flags.ERROR;
            /// <summary>Returns whether the <see cref="Point"/> is an inclusion end.</summary>
            public bool IsEnd => (_Flags & Flags.END) != Flags.ERROR;
            /// <summary>Returns whether this inflection represents an universal interval.</summary>
            public bool IsUniversal => (_Flags & Flags.UNIVERSAL) != Flags.ERROR;
            /// <summary>Returns whether this inflection represents a singleton.</summary>
            public bool IsSingleton => (_Flags & Flags.SINGLETON) != Flags.ERROR;
            
            /// <summary>
            /// Used only to signal that a composed inflection has no contents.  An empty <see cref="IntervalSet{T}"/> 
            /// will have no Inflections in its <seealso cref="IntervalSet{T}.Inflections"/> property.
            /// </summary>
            internal bool IsEmpty => _Flags == Flags.ERROR;
            internal bool HasBefore => (_Flags & Flags.HAS_BEFORE_MASK) != Flags.ERROR;
            
            internal bool HasAfter => (_Flags & Flags.HAS_AFTER_MASK) != Flags.ERROR;
            [DebuggerStepThrough]
            public bool IsSameDirection(Inflection other) => !IsSingleton && !other.IsSingleton && IsStart == other.IsStart && IsEnd == other.IsEnd;


            /// <summary>Creates a starting inflection.</summary>
            [DebuggerStepThrough]
            public static Inflection Start(T point, bool include = true) => new Inflection(point, include ? (Flags.START | Flags.POINT) : Flags.START);
            /// <summary>Creates an ending inflection.</summary>
            [DebuggerStepThrough]
            public static Inflection End(T point, bool include = true) => new Inflection(point, include ? (Flags.END | Flags.POINT) : Flags.END);
            /// <summary>Creates a singleton inflection.</summary>
            [DebuggerStepThrough]
            public static Inflection Singleton(T point, bool include = true) => new Inflection(point, include ? (Flags.SINGLETON | Flags.POINT) : Flags.SINGLETON);
            /// <summary>Creates a new inflection with the given properties.</summary>
            [DebuggerStepThrough]
            private Inflection(T point, Flags flags)
            {
                this.Point = point;
                this._Flags = flags;
            }


            

            /// <summary>Conveniently cases the given inflection to its <see cref="Point"/>.</summary>
            public static implicit operator T(Inflection f) { return f.Point; }

            [DebuggerStepThrough]
            public bool IsBefore(Inflection other) => (Point.CompareTo(other.Point) < 0);
            [DebuggerStepThrough]
            public bool IsAfter(Inflection other) => (Point.CompareTo(other.Point) > 0);
            

            /// <summary>
            /// Two <see cref="Inflection"/> structs are equal if their <see cref="Point"/> compare equally, and they 
            /// represent the same role in an interval.
            /// </summary>            
            public override bool Equals(object obj)
                => (obj is Inflection f)
                    && this.Point.CompareTo(f.Point)==0
                    && this._Flags == f._Flags;

            /// <summary>Returns the hashcode of the <see cref="Point"/>.</summary>
            public override int GetHashCode() => Point.GetHashCode();

            /// <summary>Returns a string representation of this <see cref="Inflection"/>.</summary>
            public override string ToString()
            {
                string pt = IsIncluded ? Point.ToString() : "(" + Point.ToString() + ")";
                if (IsSingleton) return IsIncluded ? pt : "." + pt + ".";
                if (IsStart) return pt + ".";
                if (IsEnd) return "." + pt;
                if (IsUniversal) return "<..>";
                return "<empty>";
            }
            
            /// <summary>
            /// Returns a mirror of this <see cref="Inflection"/>, with the same <see cref="Point"/> but the opposite 
            /// direction and the <seealso cref="IsIncluded"/> value flipped..
            /// </summary>
            public static Inflection Not(Inflection inf)
            {
                if (inf.IsStart) return Inflection.End(inf.Point, !inf.IsIncluded);
                if (inf.IsEnd) return Inflection.Start(inf.Point, !inf.IsIncluded);
                if (inf.IsSingleton) return Inflection.Singleton(inf.Point, !inf.IsIncluded);
                throw new InvalidOperationException("An inflection of type " + inf.ToString() + " cannot be mirrored.");
            }
            
            public static Inflection Compose(T point, bool before, bool pt, bool after)
            {
                if (before)
                {
                    if (after)
                    {
                        if (pt) return Inflection.Universal;
                        else return Inflection.Singleton(point, false);
                    }
                    else return Inflection.End(point, pt);
                }
                else if (after)
                {
                    return Inflection.Start(point, pt);
                }
                else if (pt)
                    return Inflection.Singleton(point, true);
                else
                    return Inflection.Empty;
            }
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex) { throw new NotImplementedException(); }
        protected virtual IEnumerator<T> GetEnumerator() { throw new NotImplementedException(); }
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => this.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
        bool ICollection<T>.IsReadOnly => false;
        protected virtual int Count { get { throw new NotImplementedException(); } }
        int ICollection<T>.Count => this.Count;
    }



    /// <summary>An exception thrown when the inflection contents are out of order or logically inconsistent.</summary>
    public class SetIntegrityException : Exception
    {
        /// <summary>Creates a new new <see cref="SetIntegrityException"/>.</summary>
        public SetIntegrityException(string message) : base(message) { }
    }



    /// <summary>
    /// An <seealso cref="IntervalSet{T}"/> which uses the idea of consecutiveness to simplify its inclusions and 
    /// allow for enumeration.
    /// </summary>
    public abstract class DiscreteIntervalSet<T> : IntervalSet<T>, IEnumerable<T> where T : IComparable<T>
    {
        /// <summary>Creates a new <see cref="DiscreteIntervalSet{T}"/> with the given inflections.</summary>
        protected DiscreteIntervalSet(params Inflection[] inflections) : base(Simplify(inflections)) { }
        /// <summary>Creates a new <see cref="DiscreteIntervalSet{T}"/> with the given inclusions.</summary>
        protected DiscreteIntervalSet(IEnumerable<T> items) : base()
        {
            // Ascending-order checking is done in Simplify(), so I didn't call ::base(items).
            if (items == null) this.Inflections = new Inflection[0];
            else this.Inflections = Simplify(items.Select(item => Inflection.Singleton(item)).ToArray());
        }

        /// <summary>Returns whether the two given inflections are consecutive.</summary>
        [DebuggerStepThrough]
        protected static bool AreConsecutive(Inflection a, Inflection b) => AreConsecutive(a.Point, b.Point);
        /// <summary>Returns whether the two given inflection points are consecutive.</summary>
        [DebuggerStepThrough]
        protected static bool AreConsecutive(T a, T b) => GetNext(a).Equals(b);
        /// <summary>Returns the next item.</summary>
        protected static Func<T, T> GetNext =
            (item) => throw new NotImplementedException("No static GetNext() has been set.");
        /// <summary>Returns the previous item.</summary>
        protected static Func<T, T> GetPrevious =
            (item) => throw new NotImplementedException("No static GetPrevious() has been set.");

        /// <summary>
        /// All inflections in a <see cref="DiscreteIntervalSet{T}"/> should have Include == true.  This method looks for 
        /// some pathological conditions and increments inflections (up or down, as appropriate) to have only 
        /// Include == true.
        /// </summary>
        protected static Inflection[] Simplify(Inflection[] orig)
        {
            if (orig.Length == 0) return orig;
            Inflection last = orig[0];
            if (last.IsUniversal) return orig;

            List<Inflection> list = new List<Inflection>();
            foreach (Inflection focus in orig)
                _Append(focus);
            return list.ToArray();

            void _Append(Inflection inf)
            {
                // Included inf is the base case.
                if (inf.IsIncluded)
                {
                    if (list.Count == 0) { list.Add(inf); return; }

                    Inflection lastInf = list[list.Count - 1];
                    if (inf.IsBefore(lastInf))
                        throw new SetIntegrityException("Inflections are not in strictly ascending order.");
                    else if (lastInf.IsSameDirection(inf))
                        throw new SetIntegrityException("Nested interval.");
                    else if (lastInf.Point.CompareTo(inf.Point) == 0)
                    {
                        if (lastInf.IsStart)
                        {
                            if (inf.IsEnd) list[list.Count - 1] = Inflection.Singleton(lastInf.Point, true);
                        }
                        else if (lastInf.IsEnd)
                        {
                            if (inf.IsStart) list.RemoveAt(list.Count - 1);
                        }
                        else
                            list[list.Count - 1] = inf;
                        return;
                    }
                    else if (AreConsecutive(lastInf, inf))
                    {
                        if (lastInf.IsStart)
                        {
                            if (inf.IsEnd) list.Add(inf);
                        }
                        else if (lastInf.IsEnd)
                        {
                            if (inf.IsStart) list.RemoveAt(list.Count - 1);
                            else list[list.Count - 1] = Inflection.End(inf.Point, true);
                        }
                        else if (lastInf.IsSingleton)
                        {
                            list[list.Count - 1] = Inflection.Start(lastInf.Point, true);
                            if (!inf.IsStart)
                                list.Add(Inflection.End(inf.Point, true));
                        }
                        return;
                    }
                    else  // A gap between lastInf and inf
                    {
                        list.Add(inf);
                        return;
                    }
                }
                // Omitted end - count the Point down.
                else if (inf.IsEnd)
                    _Append(Inflection.End(GetPrevious(inf.Point), true));
                // Omitted start - cound the Point up.
                else if (inf.IsStart)
                    _Append(Inflection.Start(GetNext(inf.Point), true));
                // The omitted singleton can only occur when previous inflection is a start.
                else if (inf.IsSingleton)
                {
                    _Append(Inflection.End(GetPrevious(inf.Point), true));
                    list.Add(Inflection.Start(GetNext(inf.Point), true));
                }
                else
                    throw new SetIntegrityException("Invalid inflection for appending: " + inf.ToString());
            }
            
            
            
        }
        
        
        /// <summary>Returns the included inflection points in this <see cref="DiscreteIntervalSet{T}"/>.</summary>        
        public override IEnumerable<T> GetInflections()
        {
            foreach (Inflection f in Inflections)
                yield return (f.IsIncluded ? f.Point : GetNext(f.Point));
        }
        
        public IEnumerable<T> Reverse()
        {
            throw new NotImplementedException();
        }

        
        /// <summary>Enumerates through this <see cref="DiscreteIntervalSet{T}"/>, returning one included item at a time.
        /// </summary>
        protected override IEnumerator<T> GetEnumerator()
        {
            if (Inflections.Length == 0) yield break;
            Inflection inf = Inflections[0];
            if (inf.IsUniversal)
                throw new InvalidOperationException("Cannot iterate through an infinite " + this.GetType().Name + ".");
            if (inf.IsEnd)
                throw new InvalidOperationException("Cannot iterate through a negative-infinite " + this.GetType().Name + " set.");

            if (inf.IsIncluded)
                yield return inf.Point;
            if (Inflections.Length == 1)
                yield break;
            for (int idx = 1; idx < Inflections.Length; idx++)
            {
                Inflection nextInf = Inflections[idx];                
                while (inf.HasAfter || (inf.IsSingleton && !inf.IsIncluded))
                {
                    T pt = GetNext(inf.Point);
                    while (pt.CompareTo(nextInf.Point) < 0)
                    {
                        yield return pt;
                        pt = GetNext(pt);
                        
                    }
                    if (++idx >= Inflections.Length) break;
                    inf = nextInf;
                    if (inf.IsIncluded)
                        yield return inf.Point;                    
                    nextInf = Inflections[idx];
                }
                
                inf = nextInf;
                if (inf.IsIncluded)
                    yield return inf.Point;
            }

            // Positive-infinite can go on forever.
            if (inf.IsStart)
            {
                T pt = inf.Point;
                while (true)
                    yield return (pt = GetNext(pt));
            }            
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        
        
    }


    /// <summary>An discrete interval set whose contents are standard 4-byte <see cref="int"/>s.</summary>
    public sealed class Int32IntervalSet : DiscreteIntervalSet<int>
    {
        public static Int32IntervalSet Infinite() =>new Int32IntervalSet(Inflection.Universal);

        [DebuggerStepThrough]
        static Int32IntervalSet()
        {
            GetNext = (item) => ++item;
            GetPrevious = (item) => --item;
        }

        /// <summary>Returns a copy of this <see cref="Int32IntervalSet"/>.</summary>
        public override IntervalSet<int> Copy() => new Int32IntervalSet(this.Inflections);

        private Int32IntervalSet(params Inflection[] inflections) : base(inflections) { }
        /// <summary>Creates a new <see cref="Int32IntervalSet"/> containing the given items.</summary>
        public Int32IntervalSet(params int[] items) : base(items) { }
        /// <summary>Creates a new <see cref="Int32IntervalSet"/> containing the given items.</summary>
        public Int32IntervalSet(IEnumerable<int> items = null) : base(items) { }

#pragma warning disable 1591
        public static Int32IntervalSet operator +(Int32IntervalSet a, DiscreteIntervalSet<int> b) => a | b;
        public static Int32IntervalSet operator *(Int32IntervalSet a, DiscreteIntervalSet<int> b) => a & b;
        public static Int32IntervalSet operator -(Int32IntervalSet a, DiscreteIntervalSet<int> b) => new Int32IntervalSet(a.Subtract(a.Inflections, b.Inflections));
        public static Int32IntervalSet operator ^(Int32IntervalSet a, DiscreteIntervalSet<int> b) => new Int32IntervalSet(a.Xor(a.Inflections, b.Inflections));
        public static Int32IntervalSet operator &(Int32IntervalSet a, DiscreteIntervalSet<int> b) => new Int32IntervalSet(a.And(a.Inflections, b.Inflections));
        public static Int32IntervalSet operator |(Int32IntervalSet a, DiscreteIntervalSet<int> b) => new Int32IntervalSet(a.Or(a.Inflections, b.Inflections));
        public static Int32IntervalSet operator !(Int32IntervalSet a) => new Int32IntervalSet(a.Not(a.Inflections));
        public static Int32IntervalSet operator ~(Int32IntervalSet a) => !a;
#pragma warning restore 1591

        public Int32IntervalSet And(Int32IntervalSet other) => this & other;
        public Int32IntervalSet Or(Int32IntervalSet other) => this | other;
        public Int32IntervalSet Subtract(Int32IntervalSet other) => this - other;
        public Int32IntervalSet Xor(Int32IntervalSet other) => this ^ other;
        public Int32IntervalSet Implies(Int32IntervalSet other) => new Int32IntervalSet(Imply(this.Inflections, other.Inflections));
    }

    /// <summary>An discrete interval set whose contents are standard 8-byte <see cref="long"/>s.</summary>
    public sealed class Int64IntervalSet : DiscreteIntervalSet<long>
    {
        static Int64IntervalSet()
        {
            GetNext = (item) => ++item;
            GetPrevious = (item) => --item;
        }

        /// <summary>Returns a universal set.</summary>
        public static Int64IntervalSet Universal() => new Int64IntervalSet(Inflection.Universal);
        /// <summary>Returns a positive-infinite set.</summary>
        public static Int64IntervalSet From(long start) => new Int64IntervalSet(Inflection.Start(start));
        /// <summary>Returns a negative-infinite set.</summary>
        public static Int64IntervalSet To(long end) => new Int64IntervalSet(Inflection.End(end));
        /// <summary>Returns a copy of this <see cref="Int64IntervalSet"/>.</summary>
        public override IntervalSet<long> Copy() => new Int64IntervalSet(this.Inflections.ToArray());

        /// <summary>Parses the given string into a new <see cref="Int64IntervalSet"/>.</summary>
        public Int64IntervalSet(string str) : base()
        {
            // Specimens:
            // "<..>"
            // ".."
            // "<..0,4..6,9..12,16..17,19..23,27,31,33,35..>"  

            // Empty?
            if (str == null || string.IsNullOrWhiteSpace(str) || str == "..") { this.MakeEmpty(); return; }

            // Universal?
            if (str == "<..>") { this.MakeUniversal(); return; }

            bool isPosInf = false;
            string[] clauses = str.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            List<Inflection> list = new List<Inflection>();
            foreach (string clause in clauses)
            {
                string[] terms = clause.Split(new string[] { ".." }, StringSplitOptions.RemoveEmptyEntries);
                if (terms.Length == 1)
                {
                    if (!long.TryParse(terms[0], out long l))
                        throw new ArgumentException("Invalid term \"" + terms[0] + "\" in: " + str);
                    if (list.Count > 0 && list[list.Count - 1].Point.CompareTo(l) >= 0)
                        throw new ArgumentException("All terms must be strictly ascending in: " + str);
                    list.Add(Inflection.Singleton(l));
                    continue;
                }
                else if (!long.TryParse(terms[0], out long l0))
                {
                    if (terms[0] == "<")
                    {
                        if (list.Count > 0)
                            throw new ArgumentException("Invalid negative-infinite in: " + str);
                        if (!long.TryParse(terms[1], out long l))
                            throw new ArgumentException("Invalid term \"" + terms[1] + "\" in: " + str);
                        list.Add(Inflection.End(l));
                    }
                    else
                        throw new ArgumentException("Invalid term \"" + terms[0] + "\" in: " + str);
                }
                else if (list.Count > 0 && list[list.Count - 1].Point.CompareTo(l0) >= 0)
                    throw new ArgumentException("All terms must be strictly ascending in: " + str);
                else if (!long.TryParse(terms[1], out long l1))
                {
                    if (terms[1] == ">")
                    {
                        if (isPosInf)
                            throw new ArgumentException("Invalid positive-infinite in: " + str);
                        list.Add(Inflection.Start(l0));
                        isPosInf = true;  // Flag used to determine if a positive-infinite has already been added.
                    }
                    else
                        throw new ArgumentException("Invalid term \"" + terms[1] + "\" in: " + str);
                }
                else if (l0.CompareTo(l1) >= 0)
                    throw new ArgumentException("Span terms (for example, \"a..b\") must be strictly ascending in: " + str);
                else
                    list.Add(Inflection.Singleton(l0));
            }
            Inflections = Simplify(list.ToArray());
        }
        private Int64IntervalSet(params Inflection[] inflections) : base(inflections) { }
        /// <summary>Creates a new <see cref="Int64IntervalSet"/> containing the given items.</summary>
        public Int64IntervalSet(params long[] items) : base(items) { }
        /// <summary>Creates a new <see cref="Int64IntervalSet"/> containing the given items.</summary>
        public Int64IntervalSet(IEnumerable<long> items = null) : base(items) { }
        /// <summary>Creates a new <see cref="Int64IntervalSet"/> starting and ending with the indicated interval.</summary>
        public Int64IntervalSet(long from, long to) : base(Inflection.Start(from), Inflection.End(to)) { }

#pragma warning disable 1591
        public static Int64IntervalSet operator +(Int64IntervalSet a, DiscreteIntervalSet<long> b) => new Int64IntervalSet(a.Or(a.Inflections, b.Inflections));
        public static Int64IntervalSet operator *(Int64IntervalSet a, DiscreteIntervalSet<long> b) => new Int64IntervalSet(a.And(a.Inflections, b.Inflections));
        public static Int64IntervalSet operator -(Int64IntervalSet a, DiscreteIntervalSet<long> b) => new Int64IntervalSet(a.Subtract(a.Inflections, b.Inflections));
        public static Int64IntervalSet operator ^(Int64IntervalSet a, DiscreteIntervalSet<long> b) => new Int64IntervalSet(a.Xor(a.Inflections, b.Inflections));
        public static Int64IntervalSet operator |(Int64IntervalSet a, DiscreteIntervalSet<long> b) => a + b;
        public static Int64IntervalSet operator &(Int64IntervalSet a, DiscreteIntervalSet<long> b) => a * b;
        public static Int64IntervalSet operator !(Int64IntervalSet i) => new Int64IntervalSet(i.Not(i.Inflections));
        public static Int64IntervalSet operator ~(Int64IntervalSet i) => !i;
#pragma warning restore 1591

    }

    //public sealed class NumberIntegerSet : DiscreteIntervalSet<Number>, ITrueSet<IEvaluateable>
    //{
    //    public static readonly NumberIntegerSet Empty = new NumberIntegerSet();
    //    public static NumberIntegerSet Infinite() { NumberIntegerSet result = new NumberIntegerSet(); result.MakeUniversal(); return result; }
    //    public NumberIntegerSet(params Number[] numbers) : base(numbers) { }
    //    bool ITrueSet<IEvaluateable>.Contains(IEvaluateable item) => this.Contains((Number)item);

    //    ITrueSet<IEvaluateable> ITrueSet<IEvaluateable>.And(ITrueSet<IEvaluateable> other)
    //    {
    //        throw new NotImplementedException();
    //    }


    //    ITrueSet<IEvaluateable> ITrueSet<IEvaluateable>.Not()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    ITrueSet<IEvaluateable> ITrueSet<IEvaluateable>.Or(ITrueSet<IEvaluateable> other)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    //public sealed class NumberIntervalSet : IntervalSet<Number>, ITrueSet<IEvaluateable>
    //{
    //    public static NumberIntervalSet Infinite() { var result = new NumberIntervalSet(); result.MakeUniversal(); return result; }
        
    //    public NumberIntervalSet(params Dependency.Number[] items) : base(items) { }

    //    // TODO:  the overrides of the logical operators (and, or, etc...)  For now, I just need
    //    // contains()


    //    bool ITrueSet<IEvaluateable>.Contains(IEvaluateable item) => this.Contains((Number)item);

    //    ITrueSet<IEvaluateable> ITrueSet<IEvaluateable>.And(ITrueSet<IEvaluateable> other)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    ITrueSet<IEvaluateable> ITrueSet<IEvaluateable>.Or(ITrueSet<IEvaluateable> other)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    ITrueSet<IEvaluateable> ITrueSet<IEvaluateable>.Not()
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
    public sealed class Float64IntervalSet : IntervalSet<double>
    {
        public Float64IntervalSet(params double[] items) : base(items) { }
        public Float64IntervalSet(IEnumerable<double> items = null) : base(items) { }
        protected override Inflection[] And(Inflection[] a, Inflection[] b)
        {
            throw new NotImplementedException();
        }
        protected override Inflection[] Or(Inflection[] a, Inflection[] b)
        {
            throw new NotImplementedException();
        }
    }
}

