using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{


    public class IntegerSet : IntervalSet<int>
    {
        public IntegerSet() : base(i => i + 1) { }
    }




    /// <summary>
    /// An interval set is a data structure designed to account for whether a run of values is included in a set or not.  
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <remarks>For example, 
    /// consider the case of a video editing tool.  If a user makes some change to the video, it would be useful for the renderer to have 
    /// an efficient way of determining which frames have been rendered and which have not, without the need to store a value for every 
    /// frame.  In the meantime, imagine the user makes another change to video which partially overlaps the original changes.  The user 
    /// need only direct that the new run of frames is "included" in the unrendered IntervalSet, without checking to see if certain 
    /// frames are already included.  The data structure coalesces intersecting inclusions automatically.
    /// </remarks>
    public class IntervalSet<T> : IEnumerable<T>, IEnumerable<IntervalSet<T>.Discontinuity>, ISet<T>, ISet<IntervalSet<T>.Interval> where T : IComparable<T>
    {
        private readonly Func<T, T> _Counter;
        protected List<Interval> Intervals = new List<Interval>();
        private bool _PositiveInfinite = false, _NegativeInfinite = false;

        public bool PositiveInfinite
        {
            get => _PositiveInfinite;
            set
            {
                if (_PositiveInfinite = value)
                {
                    if (IsEmpty) { _NegativeInfinite = true; Intervals.Clear(); Include(default(T)); }
                }

            }
        }
        public bool NegativeInfinite
        {
            get => _NegativeInfinite;
            set
            {
                if (_NegativeInfinite = value)
                {
                    if (IsEmpty) { _PositiveInfinite = true; Intervals.Clear(); Include(default(T)); }
                }

            }
        }

        /// <summary>For most IntervalSet objects, the count of its contents is meaningless, but not for all.</summary>
        protected virtual int Count { get => throw new InvalidOperationException(); }

        bool ICollection<T>.IsReadOnly => false;


        int ICollection<T>.Count => this.Count;

        int ICollection<Interval>.Count => throw new NotImplementedException();

        bool ICollection<Interval>.IsReadOnly => false;

        protected IntervalSet(Func<T, T> counter = null) { _Counter = counter ?? ((item) => item); }

        /// <summary>Returns whether the two items are consecutive, by testing whether the GetNext() item following 'a' has CompareTo(b) == 0.</summary>
        protected bool AreConsecutive(T a, T b) => GetNext(a).CompareTo(b) == 0;

        /// <summary>
        /// Returns the next item following the given item, for purposes of enumeration or to determine consecutiveness.  If there no 
        /// meaningful "next" item for Type T, should simply return the given item (this is the base class behavior).
        /// </summary>
        protected virtual T GetNext(T item) => _Counter(item);


        #region IntervalSet contents modification

        public void Clear() { Intervals.Clear(); _PositiveInfinite = false; _NegativeInfinite = false; }

        public bool Exclude(T item)
        {
            if (IsEmpty) return false;

            int idx = GetIndex(item);

            // Carve out from negative infinity?
            if (idx < 0)
            {
                if (!_NegativeInfinite) return false;
                Interval firstInterval = Intervals[0];
                if (firstInterval.IncludesStart && AreConsecutive(item, firstInterval.Start)) { firstInterval.Start = item; firstInterval.IncludesStart = false; return true; }
                else { Intervals.Insert(0, new Interval(item, item, false, false)); return true; }
            }

            // Carve out from positive infinity?
            if (!TryTrimInterval(Intervals[idx], new Interval(item, item, true, true), out Interval a, out Interval b))
            {
                if (!_PositiveInfinite || idx < Intervals.Count - 1) return false;
                Interval lastInterval = Intervals[Intervals.Count - 1];
                if (lastInterval.IncludesEnd && AreConsecutive(lastInterval.End, item)) { lastInterval.End = item; lastInterval.IncludesEnd = false; return true; }
                else { Intervals.Add(new Interval(item, item, false, false)); return true; }
            }

            // In all other cases, carve out from a middle-of-the-set interval.
            if (b != null) Intervals.Insert(idx + 1, b);
            if (a != null) Intervals[idx] = a;
            else Intervals.RemoveAt(idx);
            return true;
        }

        public bool Exclude(T start, T end)
        {
            // STep #0 - are start/end out of order, or better handled as a singleton?
            int c = start.CompareTo(end);
            if (c > 0) return Exclude(end, start);
            else if (c == 0) return Exclude(start);

            if (IsEmpty) return false;
            int endIdx = GetIndex(end);
            if (endIdx < 0)
            {
                if (!_NegativeInfinite) return false;
                else { Intervals.Insert(0, new Interval(start, start, false, false)); return true; }
            }
            int startIdx = GetIndex(start, 0, endIdx + 1);
            throw new NotImplementedException();
        }

        /// <summary>
        /// This is potentially an O(log N) operation in the best case, and an O(N) operation in the worst case.
        /// </summary>
        /// <param name="start">The start of the items to include.</param>
        /// <param name="end">The end of the items to include.</param>
        /// <returns>Returns whether this IntervalSet was changed by this operation.</returns>
        public bool Include(T start, T end)
        {
            // Case #0 - the start and end are supplied out-of-order, or more efficiently handled as a singleton.
            int c = start.CompareTo(end);
            if (c > 0) return Include(end, start);
            else if (c == 0) return Include(start);

            // Case #1 - there are no existing intervals to compare.
            Interval given = new Interval(start, end, true, true);
            if (IsEmpty) { Intervals.Add(given); return true; }

            // Case #2a - the starting index would precede the existing intervals.
            int startIdx = GetIndex(start);
            if (startIdx < 0)
            {
                Interval melded = GetMeldedInterval(given, Intervals[0]);
                if (melded == null) { Intervals.Insert(0, melded); return true; }
                given = melded;
                startIdx = 0;
            }

            // Case #2b - the starting index falls among the intervals.  Try to meld with the preceding interval.  If melding produces no 
            // change here, return false.  Otherwise, we're guaranteed a change will occur.
            else
            {
                Interval melded = GetMeldedInterval(Intervals[startIdx], given);
                if (melded == Intervals[startIdx]) return false;
                if (melded == null && ++startIdx == Intervals.Count)
                {
                    Intervals.Add(given);
                    return true;
                }
                given = melded;
            }

            // From the starting index, figure out how far to meld.
            int endIdx = GetIndex(end, startIdx, Intervals.Count - 1);

            // Finally, replace the meldable intervals with the amalgamated interval.
            if (startIdx == endIdx)
                Intervals[startIdx] = GetMeldedInterval(given, Intervals[startIdx]);
            else
            {
                if (endIdx < 0) endIdx = startIdx;
                Interval melded = endIdx < Intervals.Count ? GetMeldedInterval(given, Intervals[endIdx]) : given;
                if (melded != null) given = melded;
                else if (endIdx > startIdx + 1) given = GetMeldedInterval(given, Intervals[endIdx - 1]);
                if (startIdx == Intervals.Count) Intervals.Add(given);
                else Intervals[startIdx] = given;
                if (startIdx + 1 < Intervals.Count) Intervals.RemoveRange(startIdx + 1, endIdx - (startIdx + 1));
            }
            return true;
        }

        public bool Include(T item)
        {
            if (IsEmpty) { Intervals.Add(new Interval(item, item, true, true)); return true; }
            int idx = GetIndex(item);
            if (idx < 0)
            {
                if (_NegativeInfinite) return false;
                if (IsEmpty) { Intervals.Add(new Interval(item, item, true, true)); return true; }
                Interval firstInterval = Intervals[0];
                if (AreConsecutive(item, firstInterval.Start) && firstInterval.IncludesStart) { firstInterval.Start = item; return true; }
                Intervals.Insert(0, new Interval(item, item, true, true)); return true;
            }
            Interval prevInterval = Intervals[idx];
            int c = item.CompareTo(prevInterval.End);
            if (c == 0)
            {
                if (prevInterval.IncludesEnd) return false;
                prevInterval.IncludesEnd = true;
                return true;
            }
            else if (c > 0)
            {
                if (idx == Intervals.Count - 1 && _PositiveInfinite) return false;
                if (AreConsecutive(prevInterval.End, item) && prevInterval.IncludesEnd) { prevInterval.End = item; return true; }
                Intervals.Insert(idx + 1, new Interval(item, item, true, true));
                return true;
            }
            else if (item.CompareTo(prevInterval.Start) == 0)
            {
                if (prevInterval.IncludesStart) return false;
                prevInterval.IncludesStart = true;
                return true;
            }
            return false;
        }



        bool ISet<T>.Add(T item) => Include(item);



        public void ExceptWith(IEnumerable<T> other) { foreach (T item in other) Exclude(item); }

        public void IntersectWith(IEnumerable<T> other)
        {
            IEnumerable<T> included = other.Where(item => this.Includes(item));
            Clear();
            foreach (T item in included) Include(item);
        }

        public void UnionWith(IEnumerable<T> other) { foreach (T item in other) { Include(item); } }

        #endregion





        #region IntervalSet contents queries

        /// <summary>
        /// Returns a - b.
        /// </summary>       
        /// <returns>Returns whether there was any overlap of the two intervals.</returns>
        private bool TryTrimInterval(Interval a, Interval b, out Interval first, out Interval second)
        {
            // Step #1 - look for non-overlap cases.

            //                     =========a=======
            //  =========b=========
            int start_end = a.Start.CompareTo(b.End);
            if (start_end > 0) { first = null; second = null; return false; }

            //                    =========a=======
            //  =========b=========
            else if (start_end == 0)
            {
                if (!a.IncludesStart || b.IncludesEnd) { first = null; second = null; return false; }
                else { first = new Interval(a.Start, a.End, false, a.IncludesEnd); second = null; return true; }
            }


            //  =========a=========
            //                     =========b=======
            int end_start = a.End.CompareTo(b.Start);
            if (end_start < 0) { first = null; second = null; return false; }

            //  =========a=========
            //                    =========b=======
            if (end_start == 0)
            {
                if (!a.IncludesEnd || b.IncludesStart) { first = null; second = null; return false; }
                else { first = new Interval(a.Start, a.End, a.IncludesStart, false); second = null; return true; }
            }

            // Step #2 - definitely an overlap occurs.  Anything remain of A's start?
            int end_end = a.End.CompareTo(b.End), start_start = a.Start.CompareTo(b.Start);
            Interval[] results = { null, null };
            int i = 0;
            if (start_start < 0)
            {
                if (a.IncludesStart || !b.IncludesStart || !AreConsecutive(a.Start, b.Start)) results[i++] = new Interval(a.Start, b.Start, a.IncludesStart, !b.IncludesStart);
            }
            else if (start_start == 0)
            {
                if (a.IncludesStart && !b.IncludesStart) results[i++] = new Interval(a.Start, b.Start, true, true);
            }
            // If start_start > 0, nothing can remain of A's start.

            // Step #3 - anything remain of A's end?
            if (end_end > 0)
            {
                if (a.IncludesEnd || !b.IncludesEnd || !AreConsecutive(b.End, a.End)) results[i++] = new Interval(b.End, a.End, !b.IncludesEnd, a.IncludesEnd);
            }
            else if (end_end == 0)
            {
                if (a.IncludesEnd && !b.IncludesEnd) results[i++] = new Interval(a.End, a.End, true, true);
            }
            // If end_end > 0, nothing can remain of A's end.

            // Step #4 - return the results.
            first = results[0];
            second = results[0];
            return true;

        }

        /// <summary>
        /// Returns the melded interval two, or null if a gap exists between the intervals.  If one interval would completely subsume the 
        /// other, returns that interval.  If the intervals would be equal, returns 'a'.
        /// </summary>
        private Interval GetMeldedInterval(Interval a, Interval b)
        {
            // Case #0 - 'a' and 'b' need to be in start order (favoring 'a' when the starts are equal), or this gets too hairy.
            int start_start = a.Start.CompareTo(b.Start);
            if (start_start > 0) return GetMeldedInterval(b, a);

            // From here, I'm guaranteed that 'a's start preceds or is at the same place as b's start.

            // Case #1 - 'a' entirely precedes 'b'
            int end_start = a.End.CompareTo(b.Start);
            if (end_start < 0)
            {
                if (AreConsecutive(a.End, b.Start) && a.IncludesEnd && b.IncludesStart) return new Interval(a.Start, b.End, a.IncludesStart, b.IncludesEnd);
                return null;
            }

            // Case #2 - 'a' end lands on 'b's start, but neither includes that value.
            if (end_start == 0 && !a.IncludesEnd && !b.IncludesStart) return null;

            // From here, I'm guaranteed that there is some sort of overlap.

            // Case #3 - 'a' stretches beyond 'b'
            int end_end = a.End.CompareTo(b.End);
            if (end_end > 0)
            {
                if (start_start < 0) return a;
                else if (start_start == 0)
                {
                    if (a.IncludesStart || !b.IncludesStart) return a;
                    return new Interval(a.Start, a.End, true, a.IncludesEnd);
                }
                // Else not possible for from_from > 0
            }

            // Case #4 - 'a' ends before 'b'
            else if (end_end < 0)
            {
                if (start_start < 0) return new Interval(a.Start, b.End, a.IncludesStart, b.IncludesEnd);
                else if (start_start == 0) return new Interval(a.Start, b.End, a.IncludesStart || b.IncludesStart, b.IncludesEnd);
            }

            // Case #5 - 'a' ends at same place as 'b'
            else if (end_end == 0)
            {
                if (a.IncludesStart && a.IncludesEnd) return a;
                if (a.IncludesStart == b.IncludesStart && a.IncludesEnd == b.IncludesEnd) return a;
                if (!b.IncludesStart && !b.IncludesEnd) return a;
                if ((a.IncludesStart ^ a.IncludesEnd) && (b.IncludesStart ^ b.IncludesEnd)) return new Interval(a.Start, a.End, true, true);
                return b;
            }

            throw new InvalidOperationException("Sanity check.");
        }


        /// <summary>
        /// Gets the index of the interval that precedes the given item.  The is an O(log N) operation, where N is the count of intervals.
        /// </summary>       
        /// <returns>The index of the bracket whose end is equal to or precedes the given item.  Returns -1 of no intervals precede the given item.</returns>
        private int GetIndex(T item) => GetIndex(item, 0, Intervals.Count - 1);



        /// <summary>
        /// Gets the index of the interval that precedes the given item, between the given min and max intervals.  The is an O(log N) operation, where N is the count of intervals.
        /// </summary>        
        /// <returns>The index of the bracket whose end is equal to or precedes the given item.  Returns -1 of no preceding intervals exist between the given min and max.</returns>
        private int GetIndex(T item, int min, int max)
        {
            if (IsEmpty || item.CompareTo(Intervals[0].Start) < 0 || min > max) return -1;

            while (max > min)
            {
                int idx = ((max + min) / 2) + 1;
                Interval interval = Intervals[idx];
                int compareEnd = item.CompareTo(interval.End), compareStart = item.CompareTo(interval.Start);

                if (compareEnd > 0) { min = idx; continue; }
                if (compareStart < 0) { max = idx - 1; continue; }
                return idx;
            }
            return min;
        }


        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new IntervalEnumerator(this);


        IEnumerator IEnumerable.GetEnumerator() => new IntervalEnumerator(this);

        public bool Includes(T item)
        {
            if (IsEmpty) return false;
            int idx = GetIndex(item);
            if (idx < 0) return _NegativeInfinite;
            Interval interval = Intervals[idx];
            if (interval.Includes(item)) return true;
            if (idx < Intervals.Count - 1) return false;
            return item.CompareTo(Intervals[idx].End) > 0 && _PositiveInfinite;
        }

        public bool Includes(T start, T end)
        {
            if (IsEmpty) return false;
            int startIdx = GetIndex(start), endIdx = GetIndex(start, startIdx, Intervals.Count - 1);
            throw new NotImplementedException();

        }


        public bool IsEmpty
        {
            get
            {
                if (Intervals.Count == 0 && !_NegativeInfinite && !_PositiveInfinite) return true;
                foreach (Interval i in Intervals) if (!i.IsEmptySingleton) return false;
                return true;
            }
        }

        public bool IsUniversal
        {
            get
            {
                if (!_PositiveInfinite || !_NegativeInfinite) return false;
                if (Intervals.Count > 1) return false;
                if (!Intervals[0].IncludesStart || !Intervals[0].IncludesEnd) return false;
                return true;
            }
        }

        #endregion




        void ISet<T>.SymmetricExceptWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        bool ISet<T>.IsSubsetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        bool ISet<T>.IsSupersetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        bool ISet<T>.IsProperSupersetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        bool ISet<T>.IsProperSubsetOf(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        bool ISet<T>.Overlaps(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

        bool ISet<T>.SetEquals(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }




        bool ICollection<T>.Contains(T item) => Includes(item);

        void ICollection<T>.CopyTo(T[] array, int arrayIndex) => throw new InvalidOperationException("An IntervalSet of items cannot be copied to an array of items, because it is impossible to determine array size beforehand.");



        void ICollection<T>.Add(T item) => Include(item);
        bool ISet<Interval>.Add(Interval interval) => Include(interval.Start, interval.End);


        bool ICollection<T>.Remove(T item) => Exclude(item);

        void ISet<Interval>.UnionWith(IEnumerable<Interval> other)
        {
            throw new NotImplementedException();
        }

        void ISet<Interval>.IntersectWith(IEnumerable<Interval> other)
        {
            throw new NotImplementedException();
        }

        void ISet<Interval>.ExceptWith(IEnumerable<Interval> other)
        {
            throw new NotImplementedException();
        }

        void ISet<Interval>.SymmetricExceptWith(IEnumerable<Interval> other)
        {
            throw new NotImplementedException();
        }

        bool ISet<Interval>.IsSubsetOf(IEnumerable<Interval> other)
        {
            throw new NotImplementedException();
        }

        bool ISet<Interval>.IsSupersetOf(IEnumerable<Interval> other)
        {
            foreach (Interval interval in other) if (!Includes(interval.Start, interval.End)) return false;
            return true;
        }

        bool ISet<Interval>.IsProperSupersetOf(IEnumerable<Interval> other)
        {
            throw new NotImplementedException();
        }

        bool ISet<Interval>.IsProperSubsetOf(IEnumerable<Interval> other)
        {
            throw new NotImplementedException();
        }

        bool ISet<Interval>.Overlaps(IEnumerable<Interval> other)
        {
            throw new NotImplementedException();
        }

        bool ISet<Interval>.SetEquals(IEnumerable<Interval> other)
        {
            throw new NotImplementedException();
        }

        void ICollection<Interval>.Add(Interval item)
        {
            throw new NotImplementedException();
        }


        bool ICollection<Interval>.Contains(Interval item) => Includes(item.Start, item.End);

        void ICollection<Interval>.CopyTo(Interval[] array, int arrayIndex) => Intervals.CopyTo(array, arrayIndex);

        bool ICollection<Interval>.Remove(Interval item)
        {
            throw new NotImplementedException();
        }

        IEnumerator<Interval> IEnumerable<Interval>.GetEnumerator() => Intervals.GetEnumerator();

        IEnumerator<Discontinuity> IEnumerable<Discontinuity>.GetEnumerator()
        {
            throw new NotImplementedException();
        }



        public struct Discontinuity
        {
            public readonly T Item;
            public readonly bool Included;
            public Discontinuity(T item, bool included) { this.Item = item; this.Included = included; }
        }

        protected class Interval
        {
            public T Start;
            public T End;
            public bool IncludesStart;
            public bool IncludesEnd;
            public Interval(T from, T to, bool includeFrom, bool includeTo)
            {
                this.Start = from;
                this.End = to;
                this.IncludesStart = includeFrom;
                this.IncludesEnd = includeTo;
            }

            public bool IsSingleton => Start.CompareTo(End) == 0;
            public bool IsEmptySingleton => IsSingleton && !IncludesStart && !IncludesEnd;

            /// <summary>Returns whether the given item is included in this interval.</summary>
            public bool Includes(T item)
            {
                int c = item.CompareTo(Start);
                if (c < 0) return false;
                if (c == 0) return IncludesStart;
                c = item.CompareTo(End);
                if (c > 0) return false;
                if (c == 0) return IncludesEnd;
                return true;
            }
            /// <summary>Includes true if this interval brackets the given item (i.e. From &lt= item &lt= To). /// </summary>
            public bool Brackets(T item) => item.CompareTo(Start) < 0 ? false : item.CompareTo(End) > 0 ? false : true;

            public override string ToString() => Start.ToString() + " .. " + End.ToString();
        }


        private class IntervalEnumerator : IEnumerator<T>
        {
            private int IntervalIndex = -1;
            private Interval Interval = null;
            private readonly IntervalSet<T> IntervalSet;
            public T Current { get; private set; }

            public IntervalEnumerator(IntervalSet<T> intervalSet) { this.IntervalSet = intervalSet; }
            object IEnumerator.Current => Current;

            void IDisposable.Dispose() { } // Do nothing.  All refs are cleaned by the GC.

            bool IEnumerator.MoveNext()
            {
                // A negative interval index means iteration hasn't started yet.
                if (IntervalIndex < 0)
                {
                    if (IntervalSet._NegativeInfinite) throw new IndexOutOfRangeException("Cannot begin enumeration in a negative-infinite IntervalSet.");
                    if (IntervalSet.IsEmpty) return false;
                    try { Interval = IntervalSet.Intervals[0]; }
                    catch { throw new InvalidOperationException("Unrecoverable modification of IntervalSet during enumeration."); }
                    IntervalIndex = 0;
                    Current = Interval.Start;
                    return true;
                }

                // Null interval means iterating through positive-infinite territory.
                if (Interval == null)
                {
                    T nextItem = IntervalSet.GetNext(Current);
                    if (!IntervalSet.Includes(nextItem)) throw new InvalidOperationException("Unrecoverable modification of IntervalSet during enumeration.");
                    Current = nextItem;
                    return true;
                }

                // Since there's an interval, check to see if we've reached the end of it.
                int c = Current.CompareTo(Interval.End);
                if (c < 0)
                {
                    Current = IntervalSet.GetNext(Current);
                    return true;
                }
                else if (c == 0)
                {
                    if (++IntervalIndex >= IntervalSet.Intervals.Count - 1)
                    {
                        if (IntervalSet._PositiveInfinite)
                        {
                            Interval = null;
                            Current = IntervalSet.GetNext(Current);
                            return true;
                        }
                        else return false;
                    }
                    else
                    {
                        try { Current = (Interval = IntervalSet.Intervals[IntervalIndex]).Start; }
                        catch { throw new InvalidOperationException("Unrecoverable modification of IntervalSet during enumeration."); }
                        return true;
                    }
                }

                // If flow reaches here, it means the set has been modified during enumeration.  Not all modifications will force an 
                // exception, but modifications that change the intervals within the set will do so, as well as any modification to the 
                // current interval which leaves the Current outside the interval.
                throw new InvalidOperationException("Unrecoverable modification of IntervalSet during enumeration.");


            }

            void IEnumerator.Reset() { IntervalIndex = -1; Interval = null; }
        }
    }
}
