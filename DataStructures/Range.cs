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

        public enum Boundary
        {
            Include,
            Exclude,
            Infinite
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
        public static Range<T> Empty = new Range<T>(default(T), Boundary.Exclude, default(T), Boundary.Exclude);

        public bool Exclude(Range<T> exclusion)
        {
            Range<T> bracketer = GetBracketer(exclusion);
            if (bracketer == null) return false;

            IList<Range<T>> flattened = bracketer.GetFlattened();
            
            int idx = 0;
            while (idx < flattened.Count)
            {
                Range<T> toCut = flattened[idx];

            }

        }

        public bool Include(Range<T> inclusion)
        {
            Range<T> bracketer = GetBracketer(inclusion);

            //If the bracketer is null, that means it doesn't fit in this Range as currently constituted.  'this' will be rebuilt.
            if (bracketer == null) bracketer = this;

            //If the bracketer is simple, it means it's already included - return false.
            else if (bracketer.IsSimple) return false;

            //Otherwise, the bracketer should have its structure redone.
            List<Range<T>> existingRanges = bracketer.GetFlattened();
            existingRanges.AddRange(inclusion.GetFlattened());
            existingRanges.Sort(new RangeComparator());
            existingRanges = GetSimplified(existingRanges);
            Range<T> newHead = GetBalancedRange(existingRanges);
            newHead.CopyRefsTo(bracketer);
            return true;
        }

        protected void CopyRefsTo(Range<T> other)
        {
            other.Min = Min;
            other.Max = Max;
            other.MinBound = MinBound;
            other.MaxBound = MaxBound;
            other.LeftChild = LeftChild;
            other.RightChild = RightChild;
        }
        public void CopyTo(Range<T> other)
        {
            other.Min = Min;
            other.Max = Max;
            other.MinBound = MinBound;
            other.MaxBound = MaxBound;
            if (LeftChild!= null)
            {
                other.LeftChild = new Range<T>();
                LeftChild.CopyTo(other.LeftChild);
            }
            if (RightChild!= null)
            {
                other.RightChild = new Range<T>();
                RightChild.CopyTo(other.RightChild);
            }            
        }


        public bool Includes(Range<T> range)
        {
            Range<T> bracketer = GetBracketer(range);
            if (bracketer == null) return false;
            if (bracketer.IsSimple) return true;

            IList<Range<T>> thisRanges = bracketer.GetFlattened();
            IList<Range<T>> compareRanges = range.GetFlattened();
            foreach (Range<T> inclusion in compareRanges)
            {
                bracketer = GetBracketer(inclusion);
                if (bracketer == null) return false;
                if (!bracketer.IsSimple) return false;
            }
            return true;
        }

        public bool Includes(T number)
        {
            int cMin = number.CompareTo(Min);
            if (cMin < 0) return false;
            if (cMin == 0) return MinBound != Boundary.Exclude;

            int cMax = number.CompareTo(Max);
            if (cMax > 0) return false;
            if (cMax == 0) return MaxBound != Boundary.Exclude;

            if (LeftChild == null && RightChild == null) return true;

            return (LeftChild != null && LeftChild.Includes(number)) || (RightChild != null && RightChild.Includes(number));
        }

        /// <summary>
        /// Returns true if there are no child ranges of this Range.
        /// </summary>
        protected bool IsSimple { get { return LeftChild == null && RightChild == null; } }
        /// <summary>
        /// Returns true if this Range reflects a single constant item.
        /// </summary>
        public bool IsSingleton { get { return Min.CompareTo(Max) == 0 && MinBound != Boundary.Exclude && MaxBound != Boundary.Exclude; } }
        /// <summary>
        /// Returns true if this Range reflects an empty state.
        /// </summary>
        public bool IsEmpty { get { return Min.Equals(default(T)) && Max.Equals(default(T)) && MinBound==Boundary.Exclude && MaxBound==Boundary.Exclude; } }
        

        /// <summary>
        /// Returns whether the boundaries of this Range are at or beyond those of the given other Range.  This does not examine whether the sub-inclusions 
        /// actually include the given range.
        /// </summary>
        protected bool IsBracketer(Range<T> range)
        {
            //Check the min boundary
            if (MinBound != Boundary.Infinite)
            {
                int cMin = this.Min.CompareTo(range.Min);
                if (cMin > 0) return false;
                if (cMin == 0 && MinBound == Boundary.Exclude && range.MinBound != Boundary.Exclude) return false;
            }

            //Check the max boundary
            if (MaxBound != Boundary.Infinite)
            {
                int cMax = this.Max.CompareTo(range.Max);
                if (cMax < 0) return false;
                if (cMax == 0 && MaxBound == Boundary.Exclude && range.MaxBound != Boundary.Exclude) return false;                
            }

            //Boundaries are at or beyond, so return true.
            return true;
        }

        /// <summary>
        /// Returns whether the boundaries of this Range will overlap with the other Range.
        /// </summary>
        protected bool IsOverlapper(Range<T> other)
        {
            int cMinToMin = Min.CompareTo(other.Min);
            int cMinToMax = Min.CompareTo(other.Max);
            int cMaxToMax = Max.CompareTo(other.Max);
            int cMaxToMin = Max.CompareTo(other.Min);

            if (cMaxToMin < 0) return false;
            if (cMaxToMin == 0 && MaxBound != Boundary.Exclude && other.MaxBound != Boundary.Exclude) return false;
            if (cMinToMax > 0) return false;
            if (cMinToMax == 0 && MinBound != Boundary.Exclude && other.MinBound != Boundary.Exclude) return false;
            
            return true;



        }


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


        /// <summary>
        /// Returns the smallest item that brackets the given range.  Note that this is distinct from containing the given range.
        /// </summary>
        protected Range<T> GetBracketer(Range<T> range)
        {
            if (!IsBracketer(range)) return null;

            Range<T> leftBracketer = (LeftChild == null) ? null : LeftChild.GetBracketer(range);
            if (leftBracketer != null) return leftBracketer;
            Range<T> rightBracketer = (RightChild == null) ? null : RightChild.GetBracketer(range);
            if (rightBracketer != null) return rightBracketer;
            return this;
        }



        protected static List<Range<T>> GetSimplified(List<Range<T>> flattened)
        {
            //Some edge cases - 
            if (flattened.Count == 0) return flattened.ToList();
            if (flattened.Count == 1)
            {
                if (flattened.First().IsSimple) return flattened.ToList();
                throw new InvalidOperationException("Range<T>.GetSimplified requires flattened range items.");
            }
            if (flattened.Count <= 1) return flattened.ToList();

            List<Range<T>> result = new List<Range<T>>();

            Range<T> constructing = null;
            foreach (Range<T> leaf in flattened)
            {
                if (!leaf.IsSimple) throw new InvalidOperationException("Range<T>.GetSimplified requires flattened range items.");

                //If there is nothing being constructed yet, the current leaf provides the basis.  Copy it and continue.        
                if (constructing == null)
                {
                    constructing = new Range<T>();
                    leaf.CopyTo(constructing);
                    continue;
                }

                int c = constructing.Max.CompareTo(leaf.Min);
                //TODO:  use IsOverlapping **********************************
                //If the constructing's max is smaller than the leaf's, add the constructing and continue.
                if (c < 0 || (c == 0 && !(constructing.IncludeMin || leaf.IncludeMin)))
                {
                    result.Add(constructing);
                    constructing = null;
                    continue;
                }

                //If the leaf's max is bigger than the constructing, extend the constructing.
                c = constructing.Max.CompareTo(leaf.Max);
                if (c < 0)
                {
                    constructing.Max = leaf.Max;
                    constructing.IncludeMax = leaf.IncludeMax;
                }
                else if (c == 0)
                    constructing.IncludeMax |= leaf.IncludeMax;
            }

            return result;

        }

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

        /// <summary>
        /// Creates a tree of Range objects whose head is the item returned.
        /// </summary>
        /// <param name="ranges">Ranges should be sorted according to their Min properties.</param>        
        protected static Range<T> GetBalancedRange(IEnumerable<Range<T>> ranges)
        {
            if (ranges.Count() == 0) return Empty;

            //Returns a range representing the head of a balanced, filled left-to-right tree.
            Queue<Range<T>> queue = new Queue<Range<T>>(ranges);
            while (queue.Count > 1)
            {
                Range<T> left = queue.Dequeue();
                Range<T> right = queue.Dequeue();
                Range<T> newRange = new Range<T>(left, right);
                queue.Enqueue(newRange);
            }
            return queue.Peek();
        }


        #region Range operators
        public static  Range<T> operator !(Range<T> range)
        {
            if (range.IsEmpty) ;
            List<Range<T>> flattened = range.GetFlattened();
            
            List<Range<T>> result = new List<Range<T>>();
            


        }
        public static Range<T> operator &(Range<T> a, Range<T> b)
        {
            List<Range<T>> aRanges = a.GetFlattened();
            List<Range<T>> bRanges = b.GetFlattened();
            List<Range<T>> result = new List<Range<T>>();

            //List<Range<T>> advancer = (a.Min.CompareTo(b.Min) <= 0) ? aRanges : bRanges;
            int aIdx = 0, bIdx = 0;

            while (aIdx < aRanges.Count && bIdx < bRanges.Count)
            {
                Range<T> aRange = aRanges[aIdx], bRange = bRanges[bIdx];
                if (!aRange.IsOverlapper(bRange))
                {
                    if (aRange.Min.CompareTo(bRange.Max) > 0) aIdx++;
                    if (bRange.Min.CompareTo(aRange.Max) > 0) bIdx++;
                    else bIdx++;
                    continue;
                }

                T newMin = (aRange.Min.CompareTo(bRange.Min) < 0) ? bRange.Min : aRange.Min;
                T newMax = (aRange.Max.CompareTo(bRange.Max) < 0) ? aRange.Max : bRange.Max;
                bool newIncludeMin = aRange.Includes(newMin) && bRange.Includes(newMin);
                bool newIncludeMax = aRange.Includes(newMax) && bRange.Includes(newMax);

                result.Add(new Range<T>(newMin, newIncludeMin, newMax, newIncludeMax));
            }

            result.Sort(new RangeComparator());
            result = GetSimplified(result);
            return GetBalancedRange(result);
        }

        public static Range<T> operator |(Range<T> a, Range<T> b)
        {
            List<Range<T>> result = a.GetFlattened();
            result.AddRange(b.GetFlattened());
            result.Sort(new RangeComparator());
            result = GetSimplified(result);
            return GetBalancedRange(result);
        }

        public static Range<T> operator *(Range<T> a, Range<T> b)
        {
            return a & b;
        }
        public static Range<T> operator +(Range<T> a, Range<T> b)
        {
            return a | b;
        }

        #endregion
    }
}
