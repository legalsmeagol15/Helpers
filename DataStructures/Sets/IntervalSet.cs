using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{
    /// <summary>Describes an interval set, which is a loss-tolerant data inclusion/exclusion set.</summary>
    public interface IIntervalSet<T> where T : IComparable<T>
    {
        bool IsEmpty { get; }
        
        bool IsUniversal { get; }
        
        bool IsPositiveInfinite { get; }
        
        bool IsNegativeInfinite { get; }

        bool Includes(T item);

        void Negate();        
        void IntersectWith(IntervalSet<T> other);        
        void UnionWith(IntervalSet<T> other);
        void ExceptWith(IntervalSet<T> other);
        void SymmetricExceptWith(IntervalSet<T> other);
        void ImplyWith(IntervalSet<T> other);

        void MakeUniversal();
        void MakeEmpty();
        void MakePositiveInfinite(T start, bool includeStart = true);
        void MakeNegativeInfinite(T end, bool includeEnd = true);
    }


    /// <summary>An abstract interval set of the given type.</summary>
    public abstract class IntervalSet<T> : IIntervalSet<T> where T : IComparable<T>
    {
        /// <summary>The set of inflection points for this interval set.  This must be guaranteed to be in strictly 
        /// ascending order at all times.</summary>
        protected internal Inflection[] Inflections { get; set; }

        /// <summary>Creates a new <see cref="IntervalSet{T}"/> with the given inflection points.</summary>
        /// <param name="inflections">Optional.  If omitted, creates an empty <see cref="IntervalSet{T}"/>.</param>
        [DebuggerStepThrough]
        protected IntervalSet(params Inflection[] inflections) { Inflections = inflections ?? new Inflection[0]; }
        /// <summary>Creates a new <see cref="IntervalSet{T}"/> with the given item contents included as singletons.
        /// </summary>
        /// <param name="items">Optional.  If omitted, creates an empty <see cref="IntervalSet{T}"/>.</param>
        [DebuggerStepThrough]
        protected IntervalSet(IEnumerable<T> items = null) : this((Inflection[])null)
        {
            if (items == null)
                Inflections = new Inflection[0];
            else
            {
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
        }

        /// <summary>Returns whether this <see cref="IntervalSet{T}"/> represents an empty set.</summary>
        public bool IsEmpty => Inflections.Length == 0;
        /// <summary>Returns whether this <see cref="IntervalSet{T}"/> represents an universal set.</summary>
        public bool IsUniversal => Inflections.Length == 2 && Inflections[0].IsEnd && Inflections[1].IsEnd && Inflections[0] == Inflections[1];
        /// <summary>Returns whether this <see cref="IntervalSet{T}"/> represents a positive-infinite set.</summary>
        public bool IsPositiveInfinite => Inflections.Length > 0 && Inflections[Inflections.Length - 1].IsStart;
        /// <summary>Returns whether this <see cref="IntervalSet{T}"/> represents a negative-infinite set.</summary>
        public bool IsNegativeInfinite => Inflections.Length > 0 && Inflections[0].IsEnd;

        /// <summary>Returns whether the given item is included in this <see cref="IntervalSet{T}"/>.</summary>
        public bool Includes(T item)
        {
            if (Inflections.Length == 0) return false;
            if (Inflections[0].IsUniversal) return true;

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
            if (c == 0)
            {
                if (inf.IsIncluded) return true;
                inf = Inflections[mid - 1];
                if (mid > 0 && inf.Point.CompareTo(item) == 0)
                    return inf.IsIncluded;
                return false;
            }
            else if (c > 0) return inf.IsStart;
            else return inf.IsEnd;
        }

        /// <summary>Returns the inflection points of this <see cref="IntervalSet{T}"/>.</summary>
        public virtual IEnumerable<T> GetInflections() { foreach (Inflection f in Inflections) yield return f.Point; }

        /// <summary>Returns the negation of this <see cref="IntervalSet{T}"/>.</summary>
        public void Negate() => Inflections = Not(Inflections);
        /// <summary>Returns the intersection ("AND") of this and the given <see cref="IntervalSet{T}"/>.</summary>
        public void IntersectWith(IntervalSet<T> other) => Inflections = And(Inflections, other.Inflections);
        /// <summary>Returns the union ("OR") of this and the given <see cref="IntervalSet{T}"/>.</summary>
        public void UnionWith(IntervalSet<T> other) => Inflections = Or(Inflections, other.Inflections);
        /// <summary>Returns the set difference ("SUBTRACT") of this and the given <see cref="IntervalSet{T}"/>.
        /// </summary>
        public void ExceptWith(IntervalSet<T> other) => Inflections = Subtract(Inflections, other.Inflections);
        /// <summary>Returns the symmetric difference ("OR") of this and the given <see cref="IntervalSet{T}"/>.
        /// </summary>
        public void SymmetricExceptWith(IntervalSet<T> other) => Inflections = Xor(Inflections, other.Inflections);
        /// <summary>Returns the implication ("IF") of this and the given <see cref="IntervalSet{T}"/>.</summary>
        public void ImplyWith(IntervalSet<T> other) => Inflections = Imply(Inflections, other.Inflections);

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

        /// <summary>Adds the given singleton to this <see cref="IntervalSet{T}"/>.</summary>
        public bool Add(T item)
        {
            if (Includes(item)) return false;
            Add(item, true, item, true);
            return true;
        }
        /// <summary>Removes the given range of items.</summary>
        public bool Remove(T item)
        {
            if (!Includes(item)) return false;
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

        /// <summary>Returns a set-equal copy of this <see cref="IntervalSet{T}"/>.</summary>
        public virtual IntervalSet<T> Copy()
        {
            IntervalSet<T> copy = (IntervalSet<T>)Activator.CreateInstance(this.GetType());
            copy.Inflections = this.Inflections.ToArray();
            return copy;
        }
        /// <summary>Makes this set an universal <see cref="IntervalSet{T}"/>.</summary>
        public void MakeUniversal() => Inflections = new Inflection[] { Inflection.Universal };
        /// <summary>Makes this set an empty <see cref="IntervalSet{T}"/>.</summary>
        public void MakeEmpty() => Inflections = new Inflection[0];
        /// <summary>Unions this interval with a positive-infinite set, starting as indicated.</summary>
        public void MakePositiveInfinite(T start, bool includeStart = true)
            => Inflections = Or(Inflections, new Inflection[] { Inflection.Start(start, includeStart) });
        /// <summary>Unions this interval with a negative-infinite set, ending as indicated.</summary>
        public void MakeNegativeInfinite(T end, bool includeEnd = true)
            => Inflections = Or(Inflections, new Inflection[] { Inflection.End(end, includeEnd) });

        /// <summary>Returns the inverse of the given <see cref="Inflection"/> array.</summary>
        protected virtual Inflection[] Not(Inflection[] inflections)
        {
            if (IsUniversal) return new Inflection[0];
            if (IsEmpty) return new Inflection[] { Inflection.Universal };
            return inflections.Select(s => s.Mirror()).ToArray();
        }
        /// <summary>Returns the intersection of the two <see cref="Inflection"/> arrays.</summary>

        /// <summary>Returns the intersection of the given inflection arrays.</summary>
        protected virtual Inflection[] And(Inflection[] a, Inflection[] b)
        {
            if (a.Length == 0 || b.Length == 0) return AsArray();
            Inflection aInf = a[0], bInf = b[0];
            if (aInf.IsUniversal) return b.ToArray();
            if (bInf.IsUniversal) return a.ToArray();
            int aIdx = 0, bIdx = 0;
            int depth = 0;
            List<Inflection> list = new List<Inflection>();

            do
            {
                if (aInf < bInf)
                {
                    if (aInf.IsStart) { if (++depth == 2) list.Add(aInf); }
                    else if (aInf.IsEnd) { if (--depth == 1) list.Add(aInf); }
                    else if (aInf.IsSingleton)
                    {
                        if (depth == 1 && aInf.IsIncluded) list.Add(aInf);
                        else if (depth == 2 && !aInf.IsIncluded) list.Add(aInf);
                    }
                    if (++aIdx >= a.Length) break;
                    aInf = a[aIdx];
                    continue;
                }
                else if (bInf < aInf)
                {

                    if (bInf.IsStart) { if (++depth == 2) list.Add(bInf); }
                    else if (bInf.IsEnd) { if (--depth == 1) list.Add(bInf); }
                    else if (bInf.IsSingleton)
                    {
                        if (depth == 1 && bInf.IsIncluded) list.Add(bInf);
                        else if (depth == 2 && !bInf.IsIncluded) list.Add(bInf);
                    }
                    if (++bIdx >= b.Length) break;
                    bInf = b[bIdx];
                    continue;
                }
                // aInf == bInf
                else if (aInf.IsStart && bInf.IsStart)
                {
                    depth = 2;
                    list.Add(Inflection.Start(aInf.Point, aInf.IsIncluded && bInf.IsIncluded));
                }
                else if (aInf.IsEnd && bInf.IsEnd)
                {
                    depth = 0;
                    list.Add(Inflection.End(aInf.Point, aInf.IsIncluded && bInf.IsIncluded));
                }
                else if (aInf.IsSingleton)
                    list.Add(aInf);
                else if (bInf.IsSingleton)
                    list.Add(bInf);
                else
                {
                    if (aInf.IsIncluded && bInf.IsIncluded) list.Add(Inflection.Singleton(aInf.Point));
                    //depth = 1;
                }
                if (++aIdx < a.Length) aInf = a[aIdx];
                if (++bIdx < b.Length) bInf = b[bIdx];

            } while (aIdx < a.Length && bIdx < b.Length);
            if (bIdx == b.Length && b[b.Length - 1].IsStart)
                while (aIdx < a.Length)
                    list.Add(a[aIdx++]);
            else if (aIdx == a.Length && a[a.Length - 1].IsStart)
                while (bIdx < b.Length)
                    list.Add(b[bIdx++]);

            return list.ToArray();

        }
        /// <summary>Returns the union of the two <see cref="Inflection"/> arrays.</summary>
        protected virtual Inflection[] Or(Inflection[] a, Inflection[] b)
        {
            if (a.Length == 0) return AsArray(b);
            if (b.Length == 0) return AsArray(a);
            Inflection aInf = a[0], bInf = b[0];
            if (aInf.IsUniversal || bInf.IsUniversal) return AsArray(Inflection.Universal);

            int aIdx = 0, bIdx = 0;
            int depth = 0;
            List<Inflection> list = new List<IntervalSet<T>.Inflection>();
            do
            {
                if (aInf < bInf)
                {
                    if (aInf.IsStart) { if (++depth == 1) list.Add(aInf); }
                    else if (aInf.IsEnd) { if (--depth == 0) list.Add(aInf); }
                    else if (aInf.IsSingleton)
                    {
                        if (depth == 0 && aInf.IsIncluded) list.Add(aInf);
                        else if (depth == 1 && !aInf.IsIncluded) list.Add(aInf);
                    }
                    if (++aIdx >= a.Length) break;
                    aInf = a[aIdx];
                    continue;
                }
                else if (bInf < aInf)
                {
                    if (bInf.IsStart) { if (++depth == 1) list.Add(bInf); }
                    else if (bInf.IsEnd) { if (--depth == 0) list.Add(bInf); }
                    else if (bInf.IsSingleton)
                    {
                        if (depth == 0 && bInf.IsIncluded) list.Add(bInf);
                        else if (depth == 1 && !bInf.IsIncluded) list.Add(bInf);
                    }
                    if (++bIdx >= b.Length) break;
                    bInf = b[bIdx];
                    continue;
                }
                // aInf == bInf
                else if (aInf.IsSingleton)
                {
                    // If depth == 0, it must be an included singleton.  Add it.  If depth == 1, it's either an 
                    // included singleton or non-included singleton.  If it's included, it must come from the side 
                    // that didn't up the depth.  Skip it.  If it's not included, it must have come from the side that 
                    // upped the depth.  Append it.  If depth == 2, it must be a non-included singleton.  Skip it, 
                    // unless bInf is also a non-included singleton.
                    switch (depth)
                    {
                        case 0: list.Add(aInf); break;
                        case 1: if (!aInf.IsIncluded) list.Add(aInf); break;
                        case 2: if (bInf.IsSingleton && !bInf.IsIncluded) list.Add(aInf); break;
                    }
                    if (++aIdx >= a.Length) break;
                    aInf = a[aIdx];
                    continue;
                }
                else if (bInf.IsSingleton)
                {
                    switch (depth)
                    {
                        case 0: list.Add(bInf); break;
                        case 1: if (!bInf.IsIncluded) list.Add(bInf); break;
                    }
                    if (++bIdx >= b.Length) break;
                    bInf = b[bIdx];
                    continue;
                }
                else
                {
                    // If depth == 2, these must both be ends.
                    if (depth == 2) { depth = 0; list.Add(Inflection.End(aInf.Point, aInf.IsIncluded || bInf.IsIncluded)); }

                    // If depth == 2, these must both be starts.
                    else if (depth == 0) { depth = 2; list.Add(Inflection.Start(aInf.Point, aInf.IsIncluded || bInf.IsIncluded)); }

                    // Otherwise, it's a start/end or end/start.  Depth doesn't change.  If neither is included, we 
                    // have a singleton hole.
                    else if (!aInf.IsIncluded && !bInf.IsIncluded)
                        list.Add(Inflection.Singleton(aInf.Point, false));
                }

                // The following applies to all aInf==bInf
                if (++aIdx < a.Length) aInf = a[aIdx];
                if (++bIdx < b.Length) bInf = b[bIdx];
                
            } while (aIdx < a.Length && bIdx < b.Length);

            if (!bInf.IsStart)
                while (aIdx < a.Length)
                    list.Add(a[aIdx++]);
            if (!aInf.IsStart)
                while (bIdx < b.Length)
                    list.Add(b[bIdx++]);

            return list.ToArray();
        }
        /// <summary>Returns the implication of the two <see cref="Inflection"/> arrays.</summary>
        protected virtual Inflection[] Imply(Inflection[] a, Inflection[] b) => throw new NotImplementedException();
        /// <summary>Returns the set difference of the two <see cref="Inflection"/> arrays.</summary>
        protected virtual Inflection[] Subtract(Inflection[] a, Inflection[] b) => And(a, Not(b));
        /// <summary>Returns the exclusive-or (symmetric exception with) of the two <see cref="Inflection"/> arrays.
        /// </summary>
        protected virtual Inflection[] Xor(Inflection[] a, Inflection[] b) => Or(Subtract(a, b), Subtract(b, a));

        internal static Inflection[] AsArray(params Inflection[] inflections) => inflections;

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
            if (Inflections == null || Inflections.Length == 0) return "..";
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
            
            private const byte ERROR = 0x0;
            private const byte INCLUDED = 0x1;
            private const byte START = 0x2;
            private const byte END = 0x4;
            private const byte SINGLETON = 0x8;
            private const byte UNIVERSAL = 0x10; //=16
            private byte _Flags;

            /// <summary>A universal inflection.</summary>
            public static readonly Inflection Universal = new Inflection(default(T), UNIVERSAL);

            /// <summary>The point in the universe marked by this <see cref="Inflection"/>.</summary>
            public readonly T Point;
            

            /// <summary>Returns whether the <see cref="Point"/> is included.</summary>
            public bool IsIncluded => (_Flags & INCLUDED) != ERROR;
            /// <summary>Returns whether the <see cref="Point"/> is an inclusion start.</summary>
            public bool IsStart => (_Flags & START) != ERROR;
            /// <summary>Returns whether the <see cref="Point"/> is an inclusion end.</summary>
            public bool IsEnd => (_Flags & END) != ERROR;
            /// <summary>Returns whether this inflection represents an universal interval.</summary>
            public bool IsUniversal => (_Flags & UNIVERSAL) != ERROR;
            public bool IsSingleton => (_Flags & SINGLETON) != ERROR;
            [DebuggerStepThrough]
            public bool IsSameDirection(Inflection other) => !IsSingleton && !other.IsSingleton && IsStart == other.IsStart && IsEnd == other.IsEnd;

            /// <summary>Creates a starting inflection.</summary>
            [DebuggerStepThrough]
            public static Inflection Start(T point, bool include = true) => new Inflection(point, (byte)(include ? (START | INCLUDED) : START));
            /// <summary>Creates an ending inflection.</summary>
            [DebuggerStepThrough]
            public static Inflection End(T point, bool include = true) => new Inflection(point, (byte)(include ? (END | INCLUDED) : END));
            /// <summary>Creates a singleton inflection.</summary>
            [DebuggerStepThrough]
            public static Inflection Singleton(T point, bool include = true) => new Inflection(point, (byte)(include ? SINGLETON | INCLUDED : SINGLETON));
            /// <summary>Creates a new inflection with the given properties.</summary>
            [DebuggerStepThrough]
            private Inflection(T point, byte flags)
            {
                this.Point = point;
                this._Flags = flags;
            }


            /// <summary>
            /// Returns a mirror of this <see cref="Inflection"/>, with the same <see cref="Point"/> but the opposite 
            /// direction and the <seealso cref="IsIncluded"/> value flipped..
            /// </summary>
            public Inflection Mirror()
            {
                if (IsStart) return Inflection.End(Point, !IsIncluded);
                if (IsEnd) return Inflection.Start(Point, !IsIncluded);
                if (IsSingleton) return Inflection.Singleton(Point, !IsIncluded);
                throw new InvalidOperationException("An inflection of type " + this.ToString() + " cannot be mirrored.");
            }

            /// <summary>Conveniently cases the given inflection to its <see cref="Point"/>.</summary>
            public static implicit operator T(Inflection f) { return f.Point; }

            /// <summary>Compares <see cref="Inflection"/> ordering.</summary>
            public static bool operator <(Inflection a, Inflection b) => a.Point.CompareTo(b.Point) < 0;
            /// <summary>Compares <see cref="Inflection"/> ordering.</summary>
            public static bool operator >(Inflection a, Inflection b) => a.Point.CompareTo(b.Point) > 0;
            /// <summary>Compares <see cref="Inflection"/> ordering.</summary>
            public static bool operator >=(Inflection a, Inflection b) => a.Point.CompareTo(b.Point) >= 0;
            /// <summary>Compares <see cref="Inflection"/> ordering.</summary>
            public static bool operator <=(Inflection a, Inflection b) => a.Point.CompareTo(b.Point) <= 0;
            /// <summary>Compares <see cref="Inflection"/> ordering.</summary>
            public static bool operator ==(Inflection a, Inflection b) => a.Point.CompareTo(b.Point) == 0;
            /// <summary>Compares <see cref="Inflection"/> ordering.</summary>
            public static bool operator !=(Inflection a, Inflection b) => a.Point.CompareTo(b.Point) != 0;

            /// <summary>
            /// Two <see cref="Inflection"/> structs are equal if their <see cref="Point"/> compare equally, and they 
            /// represent the same role in an interval.
            /// </summary>            
            public override bool Equals(object obj)
                => (obj is Inflection f)
                    && this == f
                    && this._Flags == f._Flags;

            /// <summary>Returns the hashcode of the <see cref="Point"/>.</summary>
            public override int GetHashCode() => Point.GetHashCode();

            /// <summary>Returns a string representation of this <see cref="Inflection"/>.</summary>
            public override string ToString()
            {
                string pt = IsIncluded ? Point.ToString() : ".(" + Point.ToString() + ").";
                if (IsSingleton) return pt;
                if (IsStart) return pt + ".";
                if (IsEnd) return "." + pt;
                return "<..>";
            }
        }
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
        protected DiscreteIntervalSet(IEnumerable<T> items) : base(items) { this.Inflections = Simplify(this.Inflections); }

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
                    if (list.Count==0) { list.Add(inf); return; }

                    Inflection lastInf = list[list.Count - 1];
                    if (lastInf > inf)
                        throw new SetIntegrityException("Inflections are not in strictly ascending order.");
                    else if (lastInf.IsSameDirection(inf))
                        throw new SetIntegrityException("Nested interval.");
                    else if (lastInf == inf)
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
                else if (inf.IsSingleton && list.Count > 0 && list[list.Count - 1].IsStart)
                {
                    _Append(Inflection.End(GetPrevious(inf.Point), true));
                    list.Add(Inflection.Start(GetNext(inf.Point), true));
                }
                else
                    throw new SetIntegrityException("Invalid inflection for appending: " + inf.ToString());

            }
            

        }



        /// <summary>Returns the implications of the given inflection arrays.</summary>
        protected sealed override Inflection[] Imply(Inflection[] a, Inflection[] b) => Simplify(base.Imply(a,b));

        /// <summary>Returns the union of the given inflection arrays.</summary>


        /// <summary>Returns the inverse of the given inflection array.</summary>
        protected sealed override Inflection[] Not(Inflection[] inflections) => Simplify(base.Not(inflections));

        /// <summary>Returns the set difference of the given inflection arrays.</summary>
        protected sealed override Inflection[] Subtract(Inflection[] a, Inflection[] b) => Simplify(base.Subtract(a,b));

        /// <summary>Returns the symmetric set difference of the given inflection arrays.</summary>
        protected sealed override Inflection[] Xor(Inflection[] a, Inflection[] b) => Simplify(base.Xor(a,b));

        protected override Inflection[] And(Inflection[] a, Inflection[] b) => Simplify(base.And(a, b));

        protected override Inflection[] Or(Inflection[] a, Inflection[] b) => Simplify(base.Or(a, b));

        /// <summary>Returns a copy of this <see cref="DiscreteIntervalSet{T}"/>.</summary>
        public override IntervalSet<T> Copy() => base.Copy();

        /// <summary>Returns the included inflection points in this <see cref="DiscreteIntervalSet{T}"/>.</summary>        
        public override IEnumerable<T> GetInflections()
        {
            foreach (Inflection f in Inflections)
                yield return (f.IsIncluded ? f.Point : GetNext(f.Point));
        }
        

        /// <summary>Enumerates through this <see cref="DiscreteIntervalSet{T}"/>, returning one included item at a time.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            if (Inflections.Length == 0) yield break;
            if (Inflections[0].IsEnd)
                throw new InvalidOperationException("Cannot iterate through a negative-infinite interval set.");
            Inflection infFocus = Inflections[0];
            T pt = infFocus.Point;
            for (int i = 1; i < Inflections.Length; i++)
            {
                Inflection infNext = Inflections[i];
                if (infFocus.IsIncluded) yield return pt;
                if (infFocus.IsStart)
                    while ((pt = GetNext(pt)).CompareTo(infNext.Point) < 0)
                        yield return pt;
                infFocus = infNext;
            }
            if (infFocus.IsIncluded) yield return pt;
            if (infFocus.IsStart)
                while (true)
                    yield return (pt = GetNext(pt));  // Positive-infinite can go on forever.
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
    }


    /// <summary>An discrete interval set whose contents are standard 4-byte <see cref="int"/>s.</summary>
    public sealed class Int32IntervalSet : DiscreteIntervalSet<int>
    {
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

