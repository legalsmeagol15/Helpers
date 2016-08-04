using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataStructures.Sets;

namespace Arithmetic
{
    public class Range<T> : ISet<Range<T>> where T : IComparable<T>
    {
        protected T Min { get; private set; }
        protected T Max { get; private set; }
        protected Boundary MinBound { get; private set; }
        protected Boundary MaxBound { get; private set; }

        protected Range<T> LeftChild = null;
        protected Range<T> RightChild = null;


        /// <summary>
        /// Describes the boundary of a range.
        /// </summary>
        protected enum Boundary
        {
            Exclude = 0,
            Include = 1,
            Infinite = 2
        }


        #region Range constructors

        public Range(T min, T max)
        {
            this.Min = min;
            this.Max = max;
            MinBound = Boundary.Include;
            MaxBound = Boundary.Include;
        }
        protected Range(Range<T> left, Range<T> right)
        {
            LeftChild = left;
            RightChild = right;
            Min = left.Min;
            Max = right.Max;
            MinBound = left.MinBound;
            MaxBound = right.MaxBound;
        }
        protected Range() { }
        protected Range(T min, Boundary minBound, T max, Boundary maxBound)
        {
            Min = min;
            Max = max;
            MinBound = minBound;
            MaxBound = maxBound;
        }
        public static Range<T> FromConstant(T item)
        {
            return new Range<T>(item, Boundary.Include, item, Boundary.Include);
        }
      
        protected static Range<T> FromCombinedRanges(Range<T> range, Range<T> added = null)
        {
            List<Range<T>> existingRanges = range.GetFlattened();
            if (added != null) existingRanges.AddRange(added.GetFlattened());
            return FromRangeList(existingRanges);
        }
        protected static Range<T> FromRangeList(List<Range<T>> ranges)
        {
            ranges.Sort(new RangeComparator());
            ranges = GetSimplified(ranges);
            Range<T> result = FromSortedAndSimplifiedRanges(ranges);
            return result;
        }

        /// <summary>
        /// Creates a tree of Range objects whose head is the item returned.
        /// </summary>
        /// <param name="ranges">Ranges should be sorted according to their Min properties.</param>        
        private static Range<T> FromSortedAndSimplifiedRanges(IEnumerable<Range<T>> ranges)
        {
            if (ranges.Count() == 0) return Empty;

            //Returns a range representing the head of a balanced, filled left-to-right tree.
            Queue<Range<T>> queueA = new Queue<Range<T>>(ranges);
            Queue<Range<T>> queueB = new Queue<Range<T>>();
            while (queueA.Count > 1)
            {
                Range<T> left = queueA.Dequeue();
                Range<T> right = queueA.Dequeue();
                Range<T> newRange = new Range<T>(left, right);
                queueB.Enqueue(newRange);
                if (queueA.Count == 1) queueB.Enqueue(queueA.Dequeue());
                if (queueA.Count == 0)
                {
                    //Switch the references.
                    Queue<Range<T>> temp = queueA;
                    queueA = queueB;
                    queueB = temp;
                }
            }
            return queueA.Peek();
        }

        /// <summary>
        /// Returns a deep copy of this range to the given Range.
        /// </summary>        
        public Range<T> Copy()
        {
            Range<T> other = new Range<T>();
            other.Min = Min;
            other.Max = Max;
            other.MinBound = MinBound;
            other.MaxBound = MaxBound;
            if (LeftChild != null) other.LeftChild = LeftChild.Copy();
            if (RightChild != null) other.RightChild = RightChild.Copy();
            return other;
        }

        public static Range<T> Empty = new Range<T>(default(T), Boundary.Exclude, default(T), Boundary.Exclude);


        #endregion





        #region Range contents modifiers

        /// <summary>
        /// Ensures the given range is not included in this Range.
        /// </summary>        
        public void Exclude(Range<T> exclusion)
        {
            Range<T> diff = this - exclusion;
            diff.CopyRefsTo(this);
        }


        /// <summary>
        /// Includes the given Range in this Range.
        /// </summary>
        public void Include(Range<T> inclusion)
        {
            Range<T> sum = this + inclusion;
            sum.CopyRefsTo(this);
        }


        /// <summary>
        /// Copies the boundary data, and the child refs, from this Range to the specified other Range.
        /// </summary>        
        protected void CopyRefsTo(Range<T> other)
        {
            other.Min = Min;
            other.Max = Max;
            other.MinBound = MinBound;
            other.MaxBound = MaxBound;
            other.LeftChild = LeftChild;
            other.RightChild = RightChild;
        }




        #endregion





        #region Range relationship queries

        /// <summary>
        /// Returns a comparison that takes into account the ranges' boundary statuses.
        /// </summary>
        protected static int CompareBoundsMin(Range<T> a, Range<T> b)
        {
            if (b.MinBound == Boundary.Infinite) return (a.MinBound == Boundary.Infinite ? 0 : 1);
            if (a.MinBound == Boundary.Infinite) return -1;
            int c = a.Min.CompareTo(b.Min);

            if (c != 0) return c;
            if (a.MinBound == Boundary.Exclude && b.MinBound > Boundary.Exclude) return -1;
            if (b.MinBound == Boundary.Exclude && a.MinBound > Boundary.Exclude) return 1;
            return 0;
        }

        /// <summary>
        /// Returns a comparison that takes into account the ranges' boundary statuses.
        /// </summary>
        protected static int CompareBoundsMax(Range<T> a, Range<T> b)
        {
            if (b.MaxBound == Boundary.Infinite) return (a.MaxBound == Boundary.Infinite ? 0 : -1);
            if (a.MaxBound == Boundary.Infinite) return 1;
            int c = a.Max.CompareTo(b.Max);

            if (c != 0) return c;
            if (a.MaxBound == Boundary.Exclude && b.MaxBound > Boundary.Exclude) return -1;
            if (b.MaxBound == Boundary.Exclude && a.MaxBound > Boundary.Exclude) return 1;
            return 0;
        }

        /// <summary>
        /// Returns whether the boundaries of this Range are at or beyond those of the given other Range.
        /// </summary>
        protected bool Brackets(Range<T> other)
        {
            if (this.IsEmpty || other.IsEmpty) return false;

            int c = CompareBoundsMin(this, other);
            if (c > 0) return false;

            c = CompareBoundsMax(this, other);
            if (c < 0) return false;

            return true;
        }



        /// <summary>
        /// Returns the smallest item that brackets the given range.  Note that this is distinct from containing the given range.
        /// </summary>
        /// <returns>There are two possible results.  If null is the result, it means that there is no bracketing Range within the scope of 'this' Range.  If a Range returns, it 
        /// refers to the Range within this scope that fully brackets (ie, whose boundaries are equal to or greater than) the given Range.  The bracketer returned might be 'this', 
        /// or any Range within.</returns>
        protected Range<T> GetBracketer(Range<T> range)
        {
            if (!Brackets(range)) return null;

            Range<T> leftBracketer = (LeftChild == null) ? null : LeftChild.GetBracketer(range);
            if (leftBracketer != null) return leftBracketer;
            Range<T> rightBracketer = (RightChild == null) ? null : RightChild.GetBracketer(range);
            if (rightBracketer != null) return rightBracketer;
            return this;
        }

        /// <summary>
        /// Returns whether this Range includes the given range.
        /// </summary>
        /// <remarks>This method is an O(log N) operation in the best case, and an O(n log n) operation in the worst case, though it has been optimized to focus only on the parts 
        /// of the Range that might possibly contain items from a non-simple input.
        /// </remarks>
        public bool Includes(Range<T> other)
        {
            Range<T> bracketer = GetBracketer(other);
            if (bracketer == null) return false;
            if (bracketer.IsSimple) return true;

            //Every range from the flattened comparison should be contained in a simple bracketer, if it is included.
            List<Range<T>> otherFlattened = other.GetFlattened();
            foreach (Range<T> otherRange in otherFlattened)
            {
                Range<T> microBracketer = bracketer.GetBracketer(otherRange);
                if (microBracketer == null || !microBracketer.IsSimple) return false;                
            }
            return true;
        }

        /// <summary>
        /// Returns whether this Range includes the given constant number.
        /// </summary>
        /// <remarks>This operation is an O(1) operation  in the best case, and an O(log n) operation in the worst case.</remarks>
        public bool Includes(T number)
        {
            return Includes(FromConstant(number));
        }

        /// <summary>
        /// Signals the type of overlap.
        /// </summary>
        protected enum Overlap
        {
            /// <summary>
            /// Represents an invalid state for an overlap.
            /// </summary>
            Empty = 0,

            /// <summary>
            /// A entirely precedes B, with no overlap at all.
            /// </summary>
            A_precedes_B = -1,

            /// <summary>
            /// There is some overlap, with A either beginning or ending before A.
            /// </summary>
            A_laps_B = -2,

            /// <summary>
            /// A brackets and contains B.
            /// </summary>
            A_contains_B = -3,

            /// <summary>
            /// B brackets and contains A.
            /// </summary>
            B_contains_A = 3,

            /// <summary>
            /// There is some overlap, with B either beginning or ending before A.
            /// </summary>
            B_laps_A = 2,

            /// <summary>
            /// B entirely precedes A, with no overlap at all.
            /// </summary>
            B_precedes_A = 1,

            /// <summary>
            /// The elements of both ranges is included in the result.
            /// </summary>
            Identical=7 
                
        }
        /// <summary>
        /// Returns a Range describing the overlap between the two given ranges, along with an overlap signal describing the type of overlap.
        /// </summary>
        protected static Range<T> GetSimpleOverlap(Range<T> a, Range<T> b, out Overlap overlap)
        {
            if (!a.IsSimple || !b.IsSimple) throw new ArgumentException("Only the overlap of IsSimple ranges can be determined by this method.");
            if (a.IsEmpty || b.IsEmpty)
            {
                overlap = Overlap.Empty;
                return Empty;
            }

            //The first thing to do is determine which boundaries are infinite.
            int infinites = 0;
            if (b.MaxBound == Boundary.Infinite) infinites |= (int)Infinites.bMax;
            if (b.MinBound == Boundary.Infinite) infinites |= (int)Infinites.bMin;
            if (a.MaxBound == Boundary.Infinite) infinites |= (int)Infinites.aMax;
            if (a.MinBound == Boundary.Infinite) infinites |= (int)Infinites.aMin;
            int cMinMin = CompareBoundsMin(a, b);
            int cMaxMax = CompareBoundsMax(a, b);

            switch (infinites)
            {
                case 0:
                    {
                        //No infinites.
                        int cMaxMin = a.Max.CompareTo(b.Min);
                        if (cMaxMin < 0 || (cMaxMin == 0 && (a.MaxBound == Boundary.Exclude || b.MinBound == Boundary.Exclude)))
                        {
                            overlap = Overlap.A_precedes_B;
                            return Empty;
                        }
                        int cMinMax = a.Min.CompareTo(b.Max);
                        if (cMinMax > 0 || (cMinMax == 0 && (a.MinBound == Boundary.Exclude || b.MaxBound == Boundary.Exclude)))
                        {
                            overlap = Overlap.B_precedes_A;
                            return Empty;
                        }
                        if (cMinMin < 0 && cMaxMax < 0)
                        {
                            overlap = Overlap.A_laps_B;
                            return new Range<T>(b.Min, b.MinBound, a.Max, a.MaxBound);
                        }
                        if (cMinMin > 0 && cMaxMax > 0)
                        {
                            overlap = Overlap.B_laps_A;
                            return new Range<T>(a.Min, a.MinBound, b.Max, b.MaxBound);
                        }

                        if (cMinMin < 0 && cMaxMax >= 0)
                        {
                            overlap = Overlap.A_contains_B;
                            return b.Copy();
                        }
                        if (cMinMin > 0 && cMaxMax <= 0)
                        {
                            overlap = Overlap.B_contains_A;
                            return a.Copy();
                        }
                        if (cMinMin == 0 && cMaxMax == 0)
                        {
                            overlap = Overlap.Identical;
                            return a.Copy();
                        }
                        break;
                    }
                case 1:
                    {
                        //Only infinite is bMax.
                        if (cMinMin>=0 && cMaxMax < 0)
                        {
                            overlap = Overlap.B_contains_A;
                            return a.Copy();
                        }                        
                        int c = a.Max.CompareTo(b.Min);
                        if (c<0 || (c==0 && a.MaxBound==Boundary.Exclude && b.MinBound == Boundary.Exclude))
                        {
                            overlap = Overlap.A_precedes_B;
                            return Empty;
                        }
                        else
                        {
                            overlap = Overlap.A_laps_B;
                            return new Range<T>(b.Min, b.MinBound, a.Max, a.MaxBound);
                        }                        
                    }
                case 2:
                    {
                        //Only infinite is bMin.
                        if (cMinMin>0 && cMaxMax <= 0)
                        {
                            overlap = Overlap.B_contains_A;
                            return a.Copy();
                        }
                        int c = a.Min.CompareTo(b.Max);
                        if (c>0 || (c==0 && a.MinBound==Boundary.Exclude && a.MaxBound == Boundary.Exclude))
                        {
                            overlap = Overlap.B_precedes_A;
                            return Empty;
                        }
                        else
                        {
                            overlap = Overlap.B_laps_A;
                            return new Range<T>(a.Min, a.MinBound, b.Max, b.MaxBound);
                        }
                    }
                case 3:
                    {
                        //Infinites at bMin and bMax.
                        overlap = Overlap.B_contains_A;
                        return a.Copy();
                    }
                case 4:
                    {
                        //Only infinite at aMax.
                        if (cMinMin<=0 && cMaxMax < 0)
                        {
                            overlap = Overlap.A_contains_B;
                            return b.Copy();
                        }
                        int c = a.Min.CompareTo(b.Max);
                        if (c < 0)
                        {
                            overlap = Overlap.B_laps_A;
                            return new Range<T>(a.Min, a.MinBound, b.Max, b.MaxBound);
                        }
                        else
                        {
                            overlap = Overlap.B_precedes_A;
                            return Empty;
                        }
                    }
                case 5:
                    {
                        //Two Infinites at aMax and bMax
                        T larger = a.Max.CompareTo(b.Max) > 0 ? a.Max : b.Max;
                        if (cMinMin < 0)
                        {
                            overlap = Overlap.A_laps_B;
                            return new Range<T>(b.Min, b.MinBound, larger, Boundary.Infinite);
                        }
                        if (cMinMin > 0)
                        {
                            overlap = Overlap.B_laps_A;
                            return new Range<T>(a.Min, a.MinBound, larger, Boundary.Infinite);
                        }
                        else
                        {
                            overlap = Overlap.Identical;
                            return new Range<T>(a.Min, a.MinBound, larger, Boundary.Infinite);                            
                        }
                    }
                case 6:
                    {
                        //Two infinites at aMax and bMin.
                        int c = a.Min.CompareTo(b.Max);
                        if (c < 0 || (c == 0 && a.MinBound != Boundary.Exclude && b.MaxBound != Boundary.Exclude))
                        {
                            overlap = Overlap.B_laps_A;
                            return new Range<T>(a.Min, a.MinBound, b.Max, b.MaxBound);
                        }

                        else
                        {
                            overlap = Overlap.B_precedes_A;
                            return Empty;
                        }
                    }
                case 7:
                    {
                        //Three infinites at aMax, bMin, and bMax.
                        T larger = a.Max.CompareTo(b.Max) > 0 ? a.Max : b.Max;
                        overlap = Overlap.B_contains_A;
                        return new Range<T>(a.Min, a.MinBound, larger, Boundary.Infinite);
                    }
                case 8:
                    {
                        //single infinite at aMin.
                        if (cMinMin<0 && cMaxMax <= 0)
                        {
                            overlap = Overlap.A_contains_B;
                            return b.Copy();
                        }
                        int c = a.Max.CompareTo(b.Min);
                        if (c > 0)
                        {
                            overlap = Overlap.A_laps_B;
                            return new Range<T>(b.Min, b.MinBound, a.Max, a.MaxBound);
                        }
                        else
                        {
                            overlap = Overlap.A_precedes_B;
                            return Empty;
                        }
                    }
                case 9:
                    {
                        //Two infinites at aMin and bMax.
                        int c = a.Max.CompareTo(b.Min);
                        if (c>0 || (c==0 && a.MaxBound!=Boundary.Exclude && b.MinBound != Boundary.Exclude))
                        {
                            overlap = Overlap.A_laps_B;
                            return new Range<T>(b.Min, b.MinBound, a.Max, a.MaxBound);
                        }
                        else
                        {
                            overlap = Overlap.A_precedes_B;
                            return Empty;
                        }
                    }
                case 10:
                    {
                        //Two infinites at aMin and bMin;
                        T smaller = a.Min.CompareTo(b.Min) < 0 ? a.Min : b.Min;
                        if (cMaxMax > 0)
                        {
                            overlap = Overlap.B_laps_A;
                            return new Range<T>(smaller, Boundary.Infinite, b.Max, b.MaxBound);
                        }
                        if (cMaxMax < 0)
                        {
                            overlap = Overlap.A_laps_B;
                            return new Range<T>(smaller, Boundary.Infinite, a.Max, a.MaxBound);
                        }
                        else
                        {
                            overlap = Overlap.Identical;
                            return new Range<T>(smaller, Boundary.Infinite, a.Max, a.MaxBound);
                        }
                    }
                case 11:
                    {
                        //Three infinites at aMin, bMin, and bMax.
                        overlap = Overlap.B_contains_A;
                        T smaller = a.Min.CompareTo(b.Min) < 0 ? a.Min : b.Min;
                        return new Range<T>(smaller, Boundary.Infinite, a.Max, a.MaxBound);
                    }
                case 12:
                    {
                        //Two infinites at aMin and aMax.
                        overlap = Overlap.A_contains_B;
                        return b.Copy();
                    }
                case 13:
                    {
                        //Three infinites at aMin, aMax, and bMax.
                        overlap = Overlap.A_contains_B;
                        T larger = a.Max.CompareTo(b.Max) > 0 ? a.Max : b.Max;
                        return new Range<T>(b.Min, b.MinBound, larger, Boundary.Infinite);
                    }
                case 14:
                    {
                        //Three infinites at aMin, aMax, and bMin.
                        overlap = Overlap.A_contains_B;
                        T smaller = a.Min.CompareTo(b.Min) < 0 ? a.Min : b.Min;
                        return new Range<T>(smaller, Boundary.Infinite, b.Max, b.MaxBound);
                    }
                case 15:
                    {
                        //All four are infinites.
                        overlap = Overlap.Identical;
                        T smaller = a.Min.CompareTo(b.Min) < 0 ? a.Min : b.Min;
                        T larger = a.Max.CompareTo(b.Max) > 0 ? a.Max : b.Max;
                        return new Range<T>(smaller, Boundary.Infinite, larger, Boundary.Infinite);
                    }
            }

            throw new InvalidOperationException("Range<T>.GetSimpleRange error - program flow should never reach this point.");
        }

        /// <summary>
        /// Signifies bitwise flags associated with four positions where there may be an infinite range.
        /// </summary>
        [Flags]
        private enum Infinites
        {
            None=0,
            bMax=1,
            bMin=2,
            aMax=4,
            aMin=8
        }
        protected bool Overlaps(Range<T> other)
        {
            Overlap dummy;
            return !GetSimpleOverlap(this, other, out dummy).IsEmpty;
        }


        /// <summary>
        /// Returns true if this Range reflects an empty state.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return ((Min.Equals(default(T)) && Max.Equals(default(T))) || Min.Equals(Max) || Min.CompareTo(Max) == 0)
                       && MinBound == Boundary.Exclude && MaxBound == Boundary.Exclude;
            }
        }
        public bool IsInfinity { get { return IsSimple && MinBound == Boundary.Infinite && MaxBound == Boundary.Infinite; } }
        /// <summary>
        /// Returns true if there are no child ranges of this Range.
        /// </summary>
        protected bool IsSimple { get { return LeftChild == null && RightChild == null; } }
        /// <summary>
        /// Returns true if this Range reflects a single constant item.
        /// </summary>
        public bool IsSingleton { get { return Min.CompareTo(Max) == 0 && MinBound != Boundary.Exclude && MaxBound != Boundary.Exclude; } }




        private class RangeComparator : IComparer<Range<T>>
        {
            public int Compare(Range<T> x, Range<T> y)
            {
                int c = (x.Min.CompareTo(y.Min));

                //As long as the mins aren't equal, return in min order.
                if (c != 0) return c;

                //If the min's are equal and the boundary inclusion status of each min is the same, include in max order.
                if (x.MinBound == y.MinBound) return x.Max.CompareTo(y.Max);

                //Since x includes the min, it should come first.
                if (x.MinBound != Boundary.Exclude) return -1;

                //x doesn't include the min but y does, so y should come first.
                return 1;
            }
        }

        #endregion




        #region Range helper methods

        /// <summary>
        /// Returns the set of all leaf Ranges (those that are IsSimple==true), in ascending order according to their Min.
        /// </summary>        
        protected List<Range<T>> GetFlattened()
        {
            //Returns the leaf nodes of the tree, in ascending order.
            HashSet<Range<T>> visited = new HashSet<Range<T>>();
            Stack<Range<T>> stack = new Stack<Range<T>>();
            List<Range<T>> result = new List<Range<T>>();
            stack.Push(this);
            while (stack.Count > 0)
            {
                Range<T> focus = stack.Pop();
                if (focus.IsSimple) result.Add(focus);
                else
                {
                    //In reverse order cuz that's the order the stack pops them.
                    if (focus.RightChild != null) stack.Push(focus.RightChild);
                    if (focus.LeftChild != null) stack.Push(focus.LeftChild);
                }
            }

            return result;
        }



        private static List<Range<T>> GetSimplified(List<Range<T>> sortedAndFlattened)
        {
            //Some edge cases - 
            if (sortedAndFlattened.Count == 0) return sortedAndFlattened.ToList();
            if (sortedAndFlattened.Count == 1)
            {
                if (sortedAndFlattened.First().IsSimple) return sortedAndFlattened.ToList();
                throw new InvalidOperationException("Range<T>.GetSimplified requires flattened range items.");
            }
            if (sortedAndFlattened.Count <= 1) return sortedAndFlattened.ToList();

            List<Range<T>> result = new List<Range<T>>();

            Range<T> constructing = sortedAndFlattened.First().Copy();
            for (int i = 1; i < sortedAndFlattened.Count; i++)
            {
                Range<T> leaf = sortedAndFlattened[i];
                if (constructing.Overlaps(leaf))
                {
                    constructing += leaf;
                    continue;
                }
                result.Add(constructing);
                constructing = leaf.Copy();
            }

            return result;

        }

        #endregion






        #region Range operators


        public static Range<T> operator !(Range<T> range)
        {
            List<Range<T>> flattened = range.GetFlattened();
            List<Range<T>> inverted = new List<Range<T>>();

            Range<T> first = flattened.First();

            if (first.MinBound != Boundary.Infinite)
                inverted.Add(new Range<T>(first.Min, Boundary.Infinite, first.Min, first.MinBound == Boundary.Exclude ? Boundary.Include : Boundary.Exclude));

            Range<T> prev = first;
            for (int i = 1; i < flattened.Count; i++)
            {
                Range<T> focus = flattened[i];
                Range<T> newRange = new Range<T>(prev.Max, (prev.MaxBound == Boundary.Exclude) ? Boundary.Include : Boundary.Exclude, focus.Min,
                                                 (focus.MinBound == Boundary.Exclude) ? Boundary.Include : Boundary.Exclude);
                inverted.Add(newRange);
                prev = focus;
            }

            if (prev.MaxBound != Boundary.Infinite)
                inverted.Add(new Range<T>(prev.Max, prev.MaxBound == Boundary.Exclude ? Boundary.Include : Boundary.Exclude, prev.Max, Boundary.Infinite));

            return FromSortedAndSimplifiedRanges(inverted);
        }




        public static Range<T> operator &(Range<T> a, Range<T> b)
        {
            //First, check for the most common case: both ranges are simple.
            if (a.IsSimple && b.IsSimple)
            {
                if (!a.Overlaps(b)) return Empty;
                if (a.Brackets(b)) return b.Copy();
                if (b.Brackets(a)) return a.Copy();

                Range<T> result = new Range<T>();
                result.Min = a.Min.CompareTo(b.Min) < 0 ? b.Min : a.Min;
                result.MinBound = (CompareBoundsMin(a, b) < 0) ? b.MinBound : a.MinBound;
                result.Max = a.Max.CompareTo(b.Max) > 0 ? b.Max : a.Max;
                result.MaxBound = (CompareBoundsMax(a, b) > 0) ? b.MinBound : a.MinBound;
            }

            List<Range<T>> aRanges = a.GetFlattened();
            List<Range<T>> bRanges = b.GetFlattened();
            List<Range<T>> conjunction = new List<Range<T>>();
            
            int aIdx = 0, bIdx = 0;
            while (aIdx < aRanges.Count && bIdx < bRanges.Count)
            {
                Range<T> aRange = aRanges[aIdx], bRange = bRanges[bIdx];

                int comparison;
                if (!aRange.Overlaps(bRange, out comparison))
                {
                    if (aRange.Max.CompareTo(bRange.Min) < 0) aIdx++;
                    if (bRange.Min.CompareTo(aRange.Max) > 0) bIdx++;
                    continue;
                }

                T newMin = (aRange.Min.CompareTo(bRange.Min) < 0) ? bRange.Min : aRange.Min;
                T newMax = (aRange.Max.CompareTo(bRange.Max) < 0) ? aRange.Max : bRange.Max;
                bool newIncludeMin = aRange.Includes(newMin) && bRange.Includes(newMin);
                bool newIncludeMax = aRange.Includes(newMax) && bRange.Includes(newMax);

                conjunction.Add(new Range<T>(newMin, newIncludeMin, newMax, newIncludeMax));
            }

            conjunction.Sort(new RangeComparator());
            conjunction = GetSimplified(conjunction);
            return FromSortedAndSimplifiedRanges(conjunction);
        }

        public static Range<T> operator |(Range<T> a, Range<T> b)
        {
            //Branch #0 - edge cases.
            if (a.IsEmpty) return b.Copy();
            if (b.IsEmpty) return a.Copy();

            Range<T> copy = a.Copy();
            Range<T> bracketer = null;
            //Branch #1 - is 'b' simple?
            if (b.IsSimple)
            {

                //Branch #1A - it 'a' simple?  Two simple Ranges is the easiest possibility.
                if (a.IsSimple)
                {
                    //Update the mins.
                    int c = CompareBoundsMin(a, b);
                    if (c > 0)
                    {
                        copy.Min = a.Min.CompareTo(b.Min) < 0 ? a.Min : b.Min;  //Why another compare?  Cuz an Infinite  boundary return may mean the actual min isn't taken.
                        copy.MinBound = b.MinBound;
                    }

                    //Update the maxes
                    c = CompareBoundsMax(a, b);
                    if (c < 0)
                    {
                        copy.Max = b.Max.CompareTo(b.Max) > 0 ? b.Max : a.Max;
                        copy.MaxBound = b.MaxBound;
                    }
                    return copy;
                }

                //Branch #1B - 'b' is simple, but 'a' is not.  Check if there is a bracketer to simplify the inclusion.
                bracketer = copy.GetBracketer(b);

                //Branch #1B.1 - 'b' is simple, 'a' is not, and no bracketer can be found - do nothing.  Let the branch drop.
                if (bracketer == null) { }

                //Branch #1B.2 - 'b' is simple, 'a' is not, and a found bracketer is simple - no change can be made with this inclusion.
                else if (bracketer.IsSimple) return copy;

                //Branch #1B.3 - 'b' is simple, 'a' is not, found bracketer is not simple.
                else
                {
                    bool overlapLeft = bracketer.LeftChild.Overlaps(b);
                    bool overlapRight = bracketer.RightChild.Overlaps(b);

                    //Branch #1B.3a - 'b' is simple, 'a' is not, found bracketer is not simple, inclusion touches bracketer's LeftChild and RightChild.
                    if (overlapLeft && overlapRight)
                    {
                        //Branch #1B.3a.1 - both of bracketer's children are simple, so simply join them and make the bracketer simple.
                        if (bracketer.LeftChild.IsSimple && bracketer.RightChild.IsSimple)
                        {
                            bracketer.LeftChild = null;
                            bracketer.RightChild = null;
                        }
                        ///Branch #1B.3a.2 - one or both of bracketer's children are not simple, so will have to do a leaf-by-leaf rebuild.  Let the branch drop.
                    }

                    //Branch #1B.3b - 'b' is simple, 'a' is not, bracketer isn't simple, inclusion touches only bracketer's LeftChild.
                    else if (overlapLeft && bracketer.LeftChild.IsSimple)
                    {
                        bracketer.LeftChild.Max = b.Max;
                        bracketer.LeftChild.MaxBound = b.MaxBound;
                    }

                    //Branch #1B.3c - 'b' is simple, 'a' is not, bracketer isn't simple, inclusion touches only bracketer's RightChild.
                    else if (overlapRight && bracketer.RightChild.IsSimple)
                    {
                        bracketer.RightChild.Min = b.Min;
                        bracketer.RightChild.MaxBound = b.MaxBound;
                    }

                    //Branch #1B.3d - 'b' is simple, 'a' isn't simple, bracketer isn't simple, inclusion falls within gap between bracketer's children.  Leaf rebuild required.  Let 
                    //the branch drop.                    
                }
            }

            //Branch #2 - 'b' is not simple, so a bracketer hasn't been found yet.  Do that now.
            else
                bracketer = copy.GetBracketer(b);

            ///**This point can be reached only on 1) other is not simple; 2) other is simple, 'this' is not, but bracketer == null; 3) other is simple, 'this' is not, bracketer
            ///could be found and is not simple, but bracketer's children aren't simple either; or 4) other is simple, 'this' isn't simple, bracketer isn't simple,  and inclusion 
            ///falls in the gap between bracketer's children.

            //Branch #2A - 'b' isn't simple, no bracketer could be found; OR 'other' is simple, 'a' is not, no bracketer could be found.
            //Just add create a new Range head.
            if (bracketer == null)
            {
                //Create the new head, which will either have 'other' on the left of 'newThis', or vice versa.
                int c = copy.Max.CompareTo(b.Min);
                Range<T> newHead = (c < 0) ? new Range<T>(copy, b) : new Range<T>(b, copy);
                return newHead;
            }

            //Branch #2B - 'b' isn't simple; bracketer could be found; and bracketer's children aren't simple OR inclusion falls in between bracketer's children.
            //This is the leaf-by-leaf rebuild.
            else
                return FromCombinedRanges(bracketer, b);


        }

        public static Range<T> operator *(Range<T> a, Range<T> b)
        {
            return a & b;
        }
        public static Range<T> operator +(Range<T> a, Range<T> b)
        {
            return a | b;
        }

        public static Range<T> operator -(Range<T> a, Range<T> b)
        {
            if (!a.Overlaps(b)) return a.Copy();

            //Making the simple case fast.
            if (a.IsSimple && b.IsSimple)
            {
                int cMin = CompareBoundsMin(a, b), cMax = CompareBoundsMax(a, b);
                if (cMin > 0 && cMax < 0)       //a contains b, neither part of b is infinite.
                {
                    if (b.MinBound == Boundary.Infinite)
                        return new Range<T>(b.Max, b.MaxBound == Boundary.Exclude ? Boundary.Include : Boundary.Exclude, a.Max, a.MaxBound);
                    if (b.MaxBound == Boundary.Infinite)
                        return new Range<T>(a.Min, a.MinBound, b.Min, b.MaxBound == Boundary.Exclude ? Boundary.Include : Boundary.Exclude);
                    //The return result will not be  simple.
                    Range<T> result = new Range<T>(a.Min, a.MinBound, a.Max, a.MaxBound);
                    result.LeftChild = new Range<T>(a.Min, a.MinBound, b.Min, b.MinBound == Boundary.Exclude ? Boundary.Include : Boundary.Exclude);
                    result.RightChild = new Range<T>(b.Max, b.MaxBound == Boundary.Exclude ? Boundary.Include : Boundary.Exclude, a.Max, a.MaxBound);
                    return result;
                }
                else if (cMin <= 0 && cMax >= 0)  //b matches or contains a
                    return Empty;
                else if (cMin < 0)   //leaves only the min end of 'a'
                    return new Range<T>(a.Min, a.MinBound, b.Min, b.MinBound == Boundary.Exclude ? Boundary.Include : Boundary.Exclude);

                //leaves only the max end of 'b'
                return new Range<T>(b.Max, b.MaxBound == Boundary.Exclude ? Boundary.Include : Boundary.Exclude, a.Max, a.MaxBound);
            }


            int aIdx = 0, bIdx = 0;
            List<Range<T>> aFlattened = a.GetFlattened();
            List<Range<T>> bFlattened = b.GetFlattened();
            List<Range<T>> newList = new List<Range<T>>();
            while (aIdx < aFlattened.Count)
            {
                Range<T> aRange = aFlattened[aIdx];
                while (bIdx < bFlattened.Count)
                {
                    Range<T> bRange = bFlattened[bIdx];
                    Range<T> diff = aRange - bRange;
                    if (diff.IsEmpty) continue;
                    if (!diff.IsSimple)
                    {
                        aFlattened.InsertRange(++aIdx, diff.GetFlattened());    //Don't forget to increment past the 'a' range that is being split.
                        aRange = aFlattened[aIdx];
                    }
                    else
                        newList.Add(diff);
                    bIdx++;
                }
                aIdx++;
            }

            return FromRangeList(aFlattened);

        }

        /// <summary>
        /// This method returns a-b.  This method should only be called on ranges that are already IsSimple==true.  This method does no checking for compliance.  However, this 
        /// does not mean that 'a' will return simple after the operation of this method; if 'b' falls in the middle of 'a', 'a' will become non-simple.
        /// </summary>        
        protected static void Exclude(Range<T> a, Range<T> b)
        {
            if (a.IsEmpty || b.IsEmpty) return;
            if (!a.Overlaps(b)) return;

            int cMin = CompareBoundsMin(a, b), cMax = CompareBoundsMax(a, b);




        }

        #endregion
    }
}
