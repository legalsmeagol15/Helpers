using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures.Sets
{
    public class IntervalSet<T> : IEnumerable<T>, ISet<T>, ISet<IntervalSet<T>.Interval> where T : IComparable<T>
    {
        private readonly Func<T, T> _Counter;
        protected List<Interval> Intervals = new List<Interval>();
        public bool PositiveInfinite { get; private set; }
        public bool NegativeInfinite { get; private set; }

       
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

        /// <summary>
        /// This is potentially an O(log N) operation in the best case, and an O(N) operation in the worst case.
        /// </summary>
        /// <param name="start">The start of the items to include.</param>
        /// <param name="end">The end of the items to include.</param>
        /// <returns>Returns whether this IntervalSet was changed by this operation.</returns>
        public bool Include(T start, T end)
        {
            // Case #0 - the start and end are supplied out-of-order.
            if (start.CompareTo(end) > 0) return Include(end, start);

            // Case #1 - there are no existing intervals to compare.
            Interval given = new Interval(start, end, true, true);
            if (Intervals.Count == 0) { Intervals.Add(given); return true; }

            // Case #2a - the starting index would precede the existing intervals.
            int startIdx = GetPrecedingIndex(start, out bool _, out bool _);
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
                else if (melded != null) given = melded;
                else startIdx++;
            }

            // From the starting index, figure out how far to meld.
            int endIdx = GetPrecedingIndex(end, out bool _, out bool _, startIdx, Intervals.Count - 1);

            // Finally, replace the meldable intervals with the amalgamated interval.
            if (startIdx == endIdx)
                Intervals[startIdx] = GetMeldedInterval(given, Intervals[startIdx]);
            else
            {
                Interval melded = GetMeldedInterval(given, Intervals[endIdx]);
                if (melded != null) given = melded;
                else if (endIdx > startIdx + 1) given = GetMeldedInterval(given, Intervals[endIdx - 1]);
                Intervals[startIdx] = given;
                if (startIdx + 1 < Intervals.Count) Intervals.RemoveRange(startIdx + 1, endIdx - (startIdx + 1));
            }
            return true;
        }

        public bool Include(T item)
        {
            int idx = GetPrecedingIndex(item, out bool bracketed, out bool included);
            if (idx < 0)
            {
                if (NegativeInfinite) return false;                
                if (Intervals.Count==0) { Intervals.Add(new Interval(item, item, true, true)); return true; }
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
                if (idx == Intervals.Count - 1 && PositiveInfinite) return false;
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
        /// <param name="brackets">Out.  Returns whether the interval at the preceding index brackets the given item.</param>
        /// <param name="includes">Out.  Returns whether the interval at the preceding index includes the given item.</param>
        /// <returns>The index of the bracket whose end is equal to or precedes the given item.  Returns -1 of no intervals precede the given item.</returns>
        private int GetPrecedingIndex(T item, out bool brackets, out bool includes) => GetPrecedingIndex(item, out brackets, out includes, 0, Intervals.Count - 1);


        /// <summary>
        /// Gets the index of the interval that precedes the given item, between the given min and max intervals.  The is an O(log N) operation, where N is the count of intervals.
        /// </summary>
        /// <param name="brackets">Out.  Returns whether the interval at the preceding index brackets the given item.</param>
        /// <param name="includes">Out.  Returns whether the interval at the preceding index includes the given item.</param>
        /// <returns>The index of the bracket whose end is equal to or precedes the given item.  Returns -1 of no preceding intervals exist between the given min and max.</returns>
        private int GetPrecedingIndex(T item, out bool brackets, out bool includes, int min, int max)
        {
            if (Intervals.Count == 0 || item.CompareTo(Intervals[0].Start) < 0) { brackets = false; includes = false; return -1; }

            while (max > min)
            {
                int idx = (max + min) / 2;
                Interval interval = Intervals[idx];
                int compareFrom, compareTo;
                if ((compareFrom = item.CompareTo(interval.Start)) < 0)
                {
                    max = idx;
                    continue;
                }
                if ((compareTo = item.CompareTo(interval.End)) > 0)
                {
                    if (min == idx)
                    {
                        brackets = false;
                        includes = PositiveInfinite;
                        return idx;
                    }
                    min = idx;
                    continue;
                }
                brackets = true;
                includes = (compareFrom > 0 || (compareFrom == 0 && interval.IncludesStart) || compareTo < 0 || (compareTo == 0 && interval.IncludesEnd));
                return idx;
            }
            throw new InvalidOperationException("Sanity check.");
        }


        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new IntervalEnumerator(this);


        IEnumerator IEnumerable.GetEnumerator() => new IntervalEnumerator(this);



        bool ISet<T>.Add(T item) => Include(item);

        void ISet<T>.UnionWith(IEnumerable<T> other)
        {
            foreach (T item in other) { Include(item); }
        }

        void ISet<T>.IntersectWith(IEnumerable<T> other)
        {
            IEnumerable < T > included = other.Where(item => this.Includes(item));
            this.Intervals.Clear();
            foreach (T item in included) Include(item);
        }

        void ISet<T>.ExceptWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }

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

        void ICollection<T>.Add(T item)
        {
            throw new NotImplementedException();
        }

        void ICollection<T>.Clear()
        {
            throw new NotImplementedException();
        }

        bool ICollection<T>.Contains(T item)
        {
            throw new NotImplementedException();
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        bool ICollection<T>.Remove(T item)
        {
            throw new NotImplementedException();
        }

        bool ISet<Interval>.Add(Interval item)
        {
            throw new NotImplementedException();
        }

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
            throw new NotImplementedException();
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

        void ICollection<Interval>.Clear()
        {
            throw new NotImplementedException();
        }

        bool ICollection<Interval>.Contains(Interval item)
        {
            throw new NotImplementedException();
        }

        void ICollection<Interval>.CopyTo(Interval[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        bool ICollection<Interval>.Remove(Interval item)
        {
            throw new NotImplementedException();
        }

        IEnumerator<Interval> IEnumerable<Interval>.GetEnumerator()
        {
            throw new NotImplementedException();
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
                try
                {
                    if (IntervalIndex < 0)
                    {
                        if (IntervalSet.Intervals.Count == 0) return false;
                        IntervalIndex = 0;
                        Interval = IntervalSet.Intervals[0];
                        Current = Interval.Start;
                        return true;
                    }
                    int c = Current.CompareTo(Interval.End);
                    if (c < 0)
                    {
                        Current = IntervalSet.GetNext(Current);
                        return true;
                    }
                    else if (c == 0)
                    {
                        if (IntervalIndex == IntervalSet.Intervals.Count - 1) return false;
                        else if (IntervalIndex < IntervalSet.Intervals.Count - 1) { Current = (Interval = IntervalSet.Intervals[++IntervalIndex]).Start; return true; }
                    }
                }
                catch { }

                // If flow reaches here, it means the set has been modified during enumeration.  Not all modifications will force an 
                // exception, but modifications that change the intervals within the set will do so, as well as any modification to the 
                // current interval which leaves the Current outside the interval.
                throw new InvalidOperationException("Unrecoverable modification of IntervalSet during enumeration.");
            }

            void IEnumerator.Reset() => IntervalIndex = -1;
        }
    }
}
