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

    }

    /// <summary>An abstract interval set of the given type.</summary>
    public abstract class IntervalSet<T> : IIntervalSet<T> where T : IComparable<T>
    {
        /// <summary>The set of inflection points for this interval set.</summary>
        protected internal  Inflection[] Inflections { get; set; }

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
                T[] sorted = items.ToArray();
                Array.Sort(sorted);
                List<Inflection> list = new List<Inflection>();
                foreach (T item in sorted)
                {
                    list.Add(new Inflection(item, true, TailType.Start));
                    list.Add(new Inflection(item, true, TailType.End));
                }
                this.Inflections = list.ToArray();
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
                if (inf.Include) return true;
                inf = Inflections[mid - 1];
                if (mid > 0 && inf.Point.CompareTo(item) == 0)
                    return inf.Include;
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
        public void Except(IntervalSet<T> other) => Inflections = Subtract(Inflections, other.Inflections);
        /// <summary>Returns the symmetric difference ("OR") of this and the given <see cref="IntervalSet{T}"/>.
        /// </summary>
        public void SymmetricExcept(IntervalSet<T> other) => Inflections = Xor(Inflections, other.Inflections);
        /// <summary>Returns the implication ("IF") of this and the given <see cref="IntervalSet{T}"/>.</summary>
        public void Imply(IntervalSet<T> other) => Inflections = Imply(Inflections, other.Inflections);

        /// <summary>Unions with the given range to this <see cref="IntervalSet{T}"/>.</summary>
        /// <param name="start">The start of the range, inclusive.</param>
        /// <param name="end">The end of the range, inclusive.</param>
        public void Add(T start, T end) => Add(start, true, end, true);

        /// <summary>Unions with the given range to this <see cref="IntervalSet{T}"/>.</summary>
        public void Add(T start, bool includeStart, T end, bool includeEnd)
        {
            Inflections = Or(Inflections, new Inflection[] { new Inflection(start, includeStart, TailType.Start),
                                                                new Inflection(end, includeEnd, TailType.End) });
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
            Inflections = Subtract(Inflections, new Inflection[] { new Inflection(start, removeStart, TailType.Start),
                                                                    new Inflection(end, removeEnd, TailType.End) });
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
            => Inflections = Or(Inflections, new Inflection[] { new Inflection(start, includeStart, TailType.Start) });
        /// <summary>Unions this interval with a negative-infinite set, ending as indicated.</summary>
        public void MakeNegativeInfinite(T end, bool includeEnd = true)
            => Inflections = Or(Inflections, new Inflection[] { new Inflection(end, includeEnd, TailType.End) });        

        /// <summary>Returns the inverse of the given <see cref="Inflection"/> array.</summary>
        protected virtual Inflection[] Not(Inflection[] inflections)
        {
            if (IsUniversal) return new Inflection[0];
            if (IsEmpty) return new Inflection[] { Inflection.Universal };
            return inflections.Select(s => s.Mirror()).ToArray();
        }
        /// <summary>Returns the intersection of the two <see cref="Inflection"/> arrays.</summary>
        protected abstract Inflection[] And(Inflection[] a, Inflection[] b);
        /// <summary>Returns the union of the two <see cref="Inflection"/> arrays.</summary>
        protected abstract Inflection[] Or(Inflection[] a, Inflection[] b);
        /// <summary>Returns the implication of the two <see cref="Inflection"/> arrays.</summary>
        protected virtual Inflection[] Imply(Inflection[] a, Inflection[] b) => throw new NotImplementedException();
        /// <summary>Returns the set difference of the two <see cref="Inflection"/> arrays.</summary>
        protected virtual Inflection[] Subtract(Inflection[] a, Inflection[] b) => And(a, Not(b));
        /// <summary>Returns the exclusive-or (symmetric exception with) of the two <see cref="Inflection"/> arrays.
        /// </summary>
        protected virtual Inflection[] Xor(Inflection[] a, Inflection[] b) => Or(Subtract(a, b), Subtract(b, a));
        
        internal Inflection[] AsArray(params Inflection[] inflections) => inflections;

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
            Inflection infFocus = Inflections[0];
            if (infFocus.IsUniversal) return "<..>";
            StringBuilder sb = new StringBuilder();
            if (infFocus.IsEnd)
                sb.Append("<..");
            sb.Append(_InfToString(infFocus));
            for (int i = 1; i < Inflections.Length; i++)
            {
                Inflection infLast = infFocus;
                infFocus = Inflections[i];
                if (infLast == infFocus) continue;
                else if (infLast.IsEnd) sb.Append(",");
                else sb.Append("..");
                sb.Append(_InfToString(infFocus));
            }
            if (infFocus.IsStart)
                sb.Append("..>");
            return sb.ToString();
            string _InfToString(Inflection inf) => inf.Include ? inf.Point.ToString() : "(" + inf.Point.ToString() + ")";
            
        }

        /// <summary>The direction of an interval marked by an <see cref="Inflection"/>.</summary>
        public enum TailType
        {
            /// <summary>This <see cref="Inflection"/> starts an interval.</summary>
            Start = 1,
            /// <summary>This <see cref="Inflection"/> ends an interval.</summary>
            End = -1,
            /// <summary>This <see cref="Inflection"/> marks a universal (non-inflection) interval.</summary>
            Universal = 0
        }

        /// <summary>Marks a change in an interval set between inclusion and exclusion, or vice versa.</summary>
        public struct Inflection
        {
            /// <summary>A universal inflection.</summary>
            public static readonly Inflection Universal = new Inflection(default(T), true, TailType.Universal);

            /// <summary>The point in the universe marked by this <see cref="Inflection"/>.</summary>
            public readonly T Point;
            /// <summary>Whether the <see cref="Point"/> is included in the universe marked by this <see cref="Inflection"/>.</summary>
            public readonly bool Include;
            /// <summary>The direction of the interval marked by this <see cref="Inflection"/>.</summary>
            public TailType Tail;

            /// <summary>Creates a new inflection with the given properties.</summary>
            [DebuggerStepThrough]
            public Inflection(T point, bool include, TailType tail)
            {
                this.Point = point;
                this.Include = include;
                this.Tail = tail;
            }

            /// <summary>
            /// Returns a mirror of this <see cref="Inflection"/>, with the same <see cref="Point"/> but the opposite 
            /// <see cref="Include"/> value and <see cref="Tail"/> direction.
            /// </summary>
            public Inflection Mirror() => new Inflection(Point, !Include, (TailType)((int)this.Tail * -1));

            /// <summary>Returns whether this <see cref="Inflection"/> marks a universal set.</summary>
            public bool IsUniversal => Tail == TailType.Universal;
            /// <summary>Returns whether this <see cref="Inflection"/> starts an interval.</summary>
            public bool IsStart => Tail == TailType.Start;
            /// <summary>Returns whether this <see cref="Inflection"/> ends an interval.</summary>
            public bool IsEnd => Tail == TailType.End;

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
            /// Two <see cref="Inflection"/> structs are equal if their <see cref="Point"/> compare equally, an their 
            /// <see cref="Include"/> and <see cref="Tail"/> are both identical.
            /// </summary>            
            public override bool Equals(object obj)
                => (obj is Inflection f)
                    && this == f
                    && this.Include == f.Include
                    && this.Tail == f.Tail;

            /// <summary>Returns the hashcode of the <see cref="Point"/>.</summary>
            public override int GetHashCode() => Point.GetHashCode();

            /// <summary>Returns a string representation of this <see cref="Inflection"/>.</summary>
            public override string ToString()
            {
                switch (Tail)
                {
                    case TailType.Start: return (Include ? Point.ToString() : "(" + Point.ToString() + ")") + "..";
                    case TailType.End: return ".." + (Include ? Point.ToString() : "(" + Point.ToString() + ")");
                    default: throw new NotImplementedException();
                }
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
        private static Inflection[] Simplify(Inflection[] orig)
        {
            if (orig.Length == 0) return new Inflection[0];
            if (orig[0].IsUniversal) return new Inflection[] { Inflection.Universal };
            List<Inflection> list = new List<Inflection>() { _IncludeOnly(orig[0]) };
            for (int i = 1; i < orig.Length; i++)
            {
                Inflection last = orig[i - 1];
                Inflection focus = _IncludeOnly(orig[i]);
                /*  TODO:  is this impossible?
                if (focus < last)
                    throw new SetIntegrityException("Inflections \"" + last.ToString() + "\" and \""
                                                    + focus.ToString() + "\" out of order.");
                                                    */
                if (last.Tail == focus.Tail)
                    throw new SetIntegrityException("Set integrity error - nested interval.");
                if (last == focus && last.Include != focus.Include)
                    throw new SetIntegrityException("Set integrity error - inconsistent singleton.");
                if (last.IsEnd && AreConsecutive(last.Point, focus.Point))
                {
                    list.RemoveAt(list.Count - 1);
                    continue;
                }
                list.Add(focus);
            }
            return list.ToArray();
            Inflection _IncludeOnly(Inflection inf)
            {
                if (inf.Include)
                    return inf;
                if (inf.IsEnd)
                    return new Inflection(GetPrevious(inf.Point), true, TailType.End);
                if (inf.IsStart)
                    return new Inflection(GetNext(inf.Point), true, TailType.Start);
                throw new Exception("Set integrity error - universal not ruled out.");
            }

        }

       
        /// <summary>Returns the intersection of the given inflection arrays.</summary>
        protected sealed override Inflection[] And(Inflection[] a, Inflection[] b)
        {
            a = Simplify(a);
            b = Simplify(b);
            if (a.Length == 0 || b.Length == 0) return AsArray();
            if (a[0].IsUniversal) return b.ToArray();
            if (b[0].IsUniversal) return a.ToArray();
            int aIdx = 0, bIdx = 0;
            int aDepth = a[0].IsEnd ? 1 : 0;
            int bDepth = b[0].IsEnd ? 1 : 0;

            List<Inflection> list = new List<Inflection>();
            while (true)
            {
                Inflection aInf = a[aIdx], bInf = b[bIdx];
                if (aInf < bInf)
                {
                    if (aInf.IsStart && ++aDepth == 1 && bDepth > 0)
                        Append(list, aInf);
                    else if (aInf.IsEnd && --aDepth == 0 && bDepth > 0)
                        Append(list, aInf);
                    else if (aInf == bInf && bDepth > 0)
                        Append(list, aInf);
                    if (++aIdx >= a.Length) break;
                    continue;
                }
                else if (bInf < aInf)
                {
                    if (bInf.IsStart && ++bDepth==1 && aDepth > 0)
                        Append(list, bInf);
                    else if (bInf.IsEnd && --bDepth == 0 && aDepth > 0)
                        Append(list, bInf);
                    if (++bIdx >= b.Length) break;
                    continue;
                } else // aInf == bInf
                {
                    if (aInf.IsStart) aDepth++; else aDepth--;
                    if (bInf.IsStart) bDepth++; else bDepth--;
                    if (aInf.Tail == bInf.Tail)
                    {
                        if (!aInf.Include) Append(list, bInf);
                        else Append(list, aInf);
                    }
                    else if (aInf.Include && bInf.Include)
                    {
                        Append(list, new Inflection(aInf.Point, true, TailType.Start));
                        Append(list, new Inflection(aInf.Point, true, TailType.End));
                    }
                    else
                        throw new SetIntegrityException("This should be impossible.");
                    aIdx++;
                    bIdx++;
                }
            }
            if (bDepth > 0)
                for (; aIdx < a.Length; aIdx++)
                    Append(list, a[aIdx]);
            else if (aDepth > 0)
                for (; bIdx < b.Length; bIdx++)
                    Append(list, b[bIdx]);

            return list.ToArray();

        }

        /// <summary>Returns the implications of the given inflection arrays.</summary>
        protected sealed override Inflection[] Imply(Inflection[] a, Inflection[] b) => Simplify(base.Imply(Simplify(a), Simplify(b)));

        /// <summary>Returns the union of the given inflection arrays.</summary>
        protected sealed override Inflection[] Or(Inflection[] a, Inflection[] b)
        {
            a = Simplify(a);
            b = Simplify(b);
            if (a.Length == 0) return AsArray(b);
            if (b.Length == 0) return AsArray(a);
            if (a[0].IsUniversal || b[0].IsUniversal) return AsArray(Inflection.Universal);

            int aIdx = 0, bIdx = 0;
            int aDepth = a[0].IsEnd ? 1 : 0;
            int bDepth = b[0].IsEnd ? 1 : 0;
            List<Inflection> list = new List<IntervalSet<T>.Inflection>();
            Inflection aInf, bInf;
            while (true)
            {
                aInf = a[aIdx];
                bInf = b[bIdx];
                if (aInf < bInf)
                {
                    if (aInf.IsStart && (++aDepth == 1) && (bDepth == 0)) Append(list, aInf);
                    else if (aInf.IsEnd && (--aDepth == 0) && (bDepth == 0)) Append(list, aInf);
                    if (++aIdx >= a.Length) break;
                }
                else if (bInf < aInf)
                {
                    if (bInf.IsStart && (++bDepth == 1) && (aDepth == 0))
                        Append(list, bInf);
                    else if (bInf.IsEnd && (--bDepth == 0) && (aDepth == 0))
                        Append(list, bInf);
                    if (++bIdx >= b.Length) break;
                }
                else
                {
                    if (aInf.IsStart) aDepth++; else aDepth--;
                    if (bInf.IsStart) bDepth++; else bDepth--;
                    if (!aInf.Include && !bInf.Include)
                    {
                        if (aInf.Tail == bInf.Tail)
                            Append(list, new Inflection(aInf.Point, aInf.Include || bInf.Include, aInf.Tail));
                        else if (aInf.IsEnd) { Append(list, aInf); Append(list, bInf); }
                        else { Append(list, bInf); Append(list, aInf); }
                    }
                    if (++aIdx >= a.Length) break;
                    if (++bIdx >= b.Length) break;
                }                
            }
            if (aIdx < a.Length && bInf.IsEnd)
                for (; aIdx < a.Length; aIdx++)
                    Append(list, a[aIdx]);
            else if (bIdx < b.Length && aInf.IsEnd)
                for (; bIdx < b.Length; bIdx++)
                    Append(list, b[bIdx]);

            return Simplify(list.ToArray());
        }

        /// <summary>Returns the inverse of the given inflection array.</summary>
        protected sealed override Inflection[] Not(Inflection[] inflections)
        {
            inflections = Simplify(inflections);
            if (IsUniversal) return new Inflection[0];
            if (IsEmpty) return new Inflection[] { Inflection.Universal };
            List<Inflection> list = new List<IntervalSet<T>.Inflection>();
            foreach (Inflection inf in Inflections)
            {
                Inflection mirror = inf.Mirror();
                if (!mirror.Include)
                    mirror = (mirror.IsEnd ? new Inflection(GetPrevious(mirror.Point), true, TailType.End)
                                           : new Inflection(GetNext(mirror.Point), true, TailType.Start));
                Append(list, mirror);
            }
            return Simplify(list.ToArray());
        }

        /// <summary>Returns the set difference of the given inflection arrays.</summary>
        protected sealed override Inflection[] Subtract(Inflection[] a, Inflection[] b) => Simplify(base.Subtract(Simplify(a), Simplify(b)));

        /// <summary>Returns the symmetric set difference of the given inflection arrays.</summary>
        protected sealed override Inflection[] Xor(Inflection[] a, Inflection[] b) => Simplify(base.Xor(Simplify(a), Simplify(b)));

        /// <summary>Returns a copy of this <see cref="DiscreteIntervalSet{T}"/>.</summary>
        public override IntervalSet<T> Copy() => base.Copy();

        /// <summary>Returns the included inflection points in this <see cref="DiscreteIntervalSet{T}"/>.</summary>        
        public override IEnumerable<T> GetInflections()
        {
            foreach (Inflection f in Inflections)
                yield return (f.Include ? f.Point : GetNext(f.Point));
        }

        private static void Append(List<Inflection> list, Inflection inf)
        {
            if (list.Count == 0) { list.Add(inf); return; }
            Inflection last = list[list.Count - 1];
            if (inf.IsStart)
            {
                // The last inf in the list is presumed to be IsEnd.
                if (AreConsecutive(last, inf) && last.Include && inf.Include)
                    list.RemoveAt(list.Count - 1);
                else if (last == inf && (inf.Include || last.Include))
                    list.RemoveAt(list.Count - 1);
                else
                    list.Add(inf);
            }
            else // inf.IsEnd
            {
                // the last inf in the list is presumed to be IsStart.
                if (AreConsecutive(last, inf))
                {
                    if (!last.Include)
                    {
                        list.RemoveAt(list.Count - 1);
                        if (inf.Include)
                        {
                            list.Add(new Inflection(inf.Point, true, TailType.Start));
                            list.Add(inf);
                        }
                    }
                    else if (!inf.Include)
                        list.Add(new Inflection(last.Point, true, TailType.End));
                    else
                        list.Add(inf);
                }
                else if (last == inf)
                {
                    if (last.Include ^ inf.Include)                    
                        list.RemoveAt(list.Count - 1);
                    else
                        list.Add(inf);
                }
                else
                    list.Add(inf);
            }
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
                if (infFocus.Include) yield return pt;
                if (infFocus.IsStart)
                    while ((pt = GetNext(pt)).CompareTo(infNext.Point) <0)
                        yield return pt;
                infFocus = infNext;
            }
            if (infFocus.Include) yield return pt;
            if (infFocus.IsStart)
                while (true)
                    yield return (pt = GetNext(pt));  // Positive-infinite can go on forever.
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    }


    /// <summary>An discrete interval set whose contents are standard 4-byte <see cref="int"/>s.</summary>
    public sealed class Int32IntervalSet : DiscreteIntervalSet<int>
    {
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
        public Int32IntervalSet(IEnumerable<int> items=null) : base(items) { }        
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

        /// <summary>Returns a copy of this <see cref="Int64IntervalSet"/>.</summary>
        public override IntervalSet<long> Copy() => new Int64IntervalSet(this.Inflections);

        private Int64IntervalSet(params Inflection[] inflections) : base(inflections) { }
        /// <summary>Creates a new <see cref="Int64IntervalSet"/> containing the given items.</summary>
        public Int64IntervalSet(params long[] items) : base(items) { }
        /// <summary>Creates a new <see cref="Int64IntervalSet"/> containing the given items.</summary>
        public Int64IntervalSet(IEnumerable<long> items = null) : base(items) { }

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

