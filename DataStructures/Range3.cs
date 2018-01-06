using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{
    //public class Range3<T> where T : IComparable<T>
    //{
    //    protected Bound MinBound { get; private set; }
    //    protected Bound MaxBound { get; private set; }
    //    protected T Min { get; private set; }
    //    protected T Max { get; private set; }
    //    protected Range3<T> LeftChild { get; private set; }
    //    protected Range3<T> RightChild { get; private set; }

    //    /// <summary>
    //    /// The magnitude distance comparing a to b.  If a precedes b, a negative number will return..  If there is no discrete 
    //    /// relationship between a and b, this method should return a positive of negative number according to their comparison 
    //    /// whose magnitude is not equal to 1.  For example, since there is no discrete relationship between two real numbers 
    //    /// (ie, doubles or decimals), a>b may return 10, but should not return 1.
    //    /// </summary> 
    //    protected virtual int GetDistance(T a, T b)
    //    {
    //        int c = a.CompareTo(b);
    //        if (c > 0) return int.MaxValue;
    //        if (c < 0) return int.MinValue;
    //        return c;            
    //    }



    //    #region Range constructors

    //    public Range3(T min, T max) :this(min, Bound.Include, max, Bound.Include) { }
    //    protected enum Bound { Exclude=0, Include=1, Infinite = 2}

    //    protected Range3() { }
        
    //    protected Range3 (T min, Bound minBound, T max, Bound maxBound)
    //    {
    //        Min = min;
    //        MinBound = minBound;
    //        Max = max;
    //        MaxBound = maxBound;            
    //    }
    //    protected Range3 (Range3<T> leftChild, Range3<T> rightChild) : this(leftChild.Min, leftChild.MinBound, rightChild.Max, rightChild.MaxBound)
    //    {
    //        LeftChild = leftChild;
    //        RightChild = rightChild;            
    //    }
    //    protected static Range3<T> FromRangeList(IEnumerable<Range3<T>> rangeList)
    //    {
    //        if (rangeList.Count() == 0) return NewEmpty();

    //        //Returns a range representing the head of a balanced, filled left-to-right tree.
    //        Queue<Range3<T>> queueA = new Queue<Range3<T>>(rangeList);
    //        Queue<Range3<T>> queueB = new Queue<Range3<T>>();
    //        while (queueA.Count > 1)
    //        {
    //            Range3<T> left = queueA.Dequeue();
    //            Range3<T> right = queueA.Dequeue();
    //            Range3<T> newRange = new Range3<T>(left, right);
    //            queueB.Enqueue(newRange);
    //            if (queueA.Count == 1) queueB.Enqueue(queueA.Dequeue());
    //            if (queueA.Count == 0)
    //            {
    //                //Switch the references.
    //                Queue<Range3<T>> temp = queueA;
    //                queueA = queueB;
    //                queueB = temp;
    //            }
    //        }
    //        return queueA.Peek();
    //    }
    //    protected static Range3<T> NewSingleton(T item)
    //    {
    //        return new Range3<T>(item, Bound.Include, item, Bound.Include);
    //    }


    //    protected static Range3<T> NewEmpty() { return new Range3<T>(default(T), Bound.Exclude, default(T), Bound.Exclude); }
    //    protected static Range3<T> NewInfinity() { return new Range3<T>(default(T), Bound.Infinite, default(T), Bound.Infinite);}
    //    public Range3<T> Copy()
    //    {
    //        Range3<T> copy = new Range3<T>(Min, MinBound, Max, MaxBound);
    //        if (LeftChild != null) copy.LeftChild = LeftChild.Copy();
    //        if (RightChild != null) copy.RightChild = RightChild.Copy();
    //        return copy;
    //    }


    //    protected static Range3<T> NewMinTail(T value) { return new Range3<T>(value, Bound.Infinite, value, Bound.Exclude); }
    //    protected static Range3<T> NewMaxTail(T value) { return new Range3<T>(value, Bound.Exclude, value, Bound.Infinite); }
    //    #endregion




    //    #region Range contents queries

       

    //    /// <summary>
    //    /// Returns whether the given item is contained in this range.  An item is contained if its boundaries are equal to or within this 
    //    /// range.
    //    /// </summary>
    //    public bool Contains(T item)
    //    {  
    //        Range3<T> container = GetContainer(item);
    //        if (container == null) return false;
    //        return container.IsSimple;            
    //    }
    //    /// <summary>
    //    /// Returns whether all items in the given range are contained in this range.  An item is contained if its boundaries are equal to or 
    //    /// within this range, and not excluded.
    //    /// </summary>
    //    public bool Contains(Range3<T> other)
    //    {
    //        Range3<T> container = GetContainer(other);
    //        if (container == null) return false;
    //        if (container.IsSimple) return true;
    //        foreach (Range3<T> sub in other.GetFlattened())            
    //            if (container.GetContainer(sub) == null) return false;
    //        return true;
    //    }

       
    //    /// <summary>
    //    /// Returns whether this range contains no members.
    //    /// </summary>
    //    public bool IsEmpty { get { return (Min.Equals(default(T)) && Max.Equals(default(T))) || Min.CompareTo(Max) == 0; } }
    //    /// <summary>
    //    /// Returns whether this range is infinite in both directions.
    //    /// </summary>
    //    public bool IsInfinity { get { return IsSimple && MinBound == Bound.Infinite && MaxBound == Bound.Infinite; } }
    //    /// <summary>
    //    /// Returns whether this range has no child ranges.
    //    /// </summary>
    //    protected bool IsSimple { get { return LeftChild == null && RightChild == null; } }
    //    /// <summary>
    //    /// Returns whether this range contains only a single item.
    //    /// </summary>
    //    public bool IsSingleton { get { return MinBound == Bound.Include && MaxBound == Bound.Include && Min.CompareTo(Max) == 0; } }
    //    /// <summary>
    //    /// A tail is a range that includes to infinity above or below a certain value, but which does not include that particular 
    //    /// value.  
    //    /// </summary>
    //    protected bool IsTail { get { return Min.CompareTo(Max) == 0 && ((MinBound >= Bound.Infinite && MaxBound <= Bound.Exclude) 
    //                                                                     || (MinBound <= Bound.Exclude && MaxBound >= Bound.Infinite)); } }


    //    #endregion




    //    #region Range operators
    //    public static Range3<T> operator !(Range3<T> range)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public static Range3<T> operator -(Range3<T> a, Range3<T> b)
    //    {
    //        //Edge cases - one or the other is empty, or infinite, or there is no overlap.
    //        if (a.IsEmpty) return NewEmpty();
    //        if (b.IsEmpty) return a.Copy();
    //        if (b.IsInfinity) return NewEmpty();
    //        if (a.IsInfinity) return !b;
    //        int c = a.ComparePositionTo(b);
    //        if (c != 0) return a.Copy();

    //        ////Least common case - neither 'a' nor 'b' is simple.
    //        IEnumerator<Range3<T>> aFlattened = a.GetFlattened().GetEnumerator(),
    //                              bFlattened = b.GetFlattened().GetEnumerator();
    //        bFlattened.MoveNext();
    //        aFlattened.MoveNext();
    //        Range3<T> aFocus, bFocus;

    //        //zigzag from 'a' to 'b'
    //        List<Range3<T>> diffs = new List<Range3<T>>();
    //        while (true)
    //        {
    //            aFocus = aFlattened.Current;
    //            bFocus = bFlattened.Current;
    //            Range3<T> diff = GetDifferenceSimple(aFocus, bFocus);
    //            if (diff == null)
    //            {
    //                int cGap = a.ComparePositionTo(b);
    //                if (cGap > 0 && !bFlattened.MoveNext()) break;
    //            }
    //            else if (diff.IsSimple)
    //                diffs.Add(diff);
    //            else
    //            {
    //                diffs.Add(diff.LeftChild);
    //                diffs.Add(diff.RightChild);
    //            }
    //            if (!aFlattened.MoveNext()) break;
    //        }
    //        //There might be some left unexamined in 'a'.
    //        while (aFlattened.MoveNext())
    //        {
    //            Range3<T> diff = GetDifferenceSimple(aFlattened.Current, bFocus);
    //            if (diff != null) diffs.Add(diff);
    //        }
    //        //Or, there might be some left unexamined in 'b'.
    //        while (bFlattened.MoveNext())
    //        {
    //            Range3<T> diff = GetDifferenceSimple(bFlattened.Current, aFocus);
    //            if (diff != null) diffs.Add(diff);
    //        }

    //        //Sort is necessary because a subtraction in the middle of a simple range will create two leaf ranges.
    //        diffs.Sort((j, k) => j.CompareMinTo(k));
    //        return FromRangeList(GetSimplified(diffs));
    //    }
    //    public static Range3<T> operator -(Range3<T> range, T item)
    //    {
    //        if (range.IsEmpty) return NewEmpty();
    //        if (range.IsInfinity) return new Range3<T>(NewMinTail(item), NewMaxTail(item));

    //        Range3<T> result = range.Copy();

    //        //The root requires special handling, because if the item hits one or the other border but the bound is infinite, then 
    //        //tails must be created and the tail made subject to a new range, along with the de-infinited root.

    //        //First, compare to the item to ensure it occurs in the root.
    //        int cMin = result.CompareMinTo(item), cMax = result.CompareMaxTo(item);
    //        if (cMin > 0 || cMax < 0) return result;  //falls outside the root; subtraction does nothing.

    //        //Second, does it fall on the border of the root?
    //        if (cMin == 0 || cMax == 0)
    //        {
    //            if (cMin == 0)
    //            {
    //                //Matches the min border - is that min border infinite?
    //                if (result.MinBound >= Bound.Infinite)
    //                {
    //                    Range3<T> oldResult = result;
    //                    result = new Range3<T>(NewMinTail(item), result);
    //                    oldResult.SetMin(item, Bound.Exclude);
    //                }
    //                else
    //                    result.SetMin(item, Bound.Exclude);
    //            }
    //            if (cMax == 0)
    //            {
    //                //Matches the max border - is that max border infinite?
    //                if (result.MaxBound >= Bound.Infinite)
    //                {
    //                    Range3<T> oldResult = result;
    //                    result = new Range3<T>(result, NewMaxTail(item));
    //                    oldResult.SetMax(item, Bound.Exclude);
    //                }
    //                else
    //                    result.SetMax(item, Bound.Exclude);
    //            }
    //            return result;
    //        }



    //        Range3<T> bracketer = result;
    //        //Find the bracketer, which here means the highest Range either whose border lands on the given item, or is the simple container.            
    //        while (true)
    //        {
    //            if (bracketer.IsSimple)
    //            {
    //                bracketer.LeftChild = new Range3<T>(bracketer.Min, bracketer.MinBound, item, Bound.Exclude);
    //                bracketer.RightChild = new Range3<T>(item, Bound.Exclude, bracketer.Max, bracketer.MaxBound);
    //                return result;
    //            }

    //            //The item is contained on a non-simple range, but doesn't coincide with the border.
    //            int c = bracketer.LeftChild.CompareMaxTo(item);
    //            if (c > 0)
    //            {
    //                //LeftChild is the bracketer.
    //                cMax = c;
    //                bracketer = bracketer.LeftChild;
    //                continue;
    //            }
    //            else if (c == 0)
    //            {
    //                bracketer.LeftChild.SetMax(item, Bound.Exclude);
    //                return result;
    //            }
    //            else
    //            {
    //                //RightChild is the bracketer.
    //                c = bracketer.RightChild.CompareMinTo(item);
    //                if (c < 0)
    //                {
    //                    cMin = c;
    //                    bracketer = bracketer.RightChild;
    //                    continue;
    //                }
    //                else if (c == 0)
    //                {
    //                    bracketer.RightChild.SetMin(item, Bound.Exclude);
    //                    return result;
    //                }
    //                else
    //                    //Fell in the gap - no change.
    //                    return result;
    //            }
    //        }
    //    }


    //    public static Range3<T> operator &(Range3<T> a, Range3<T> b)
    //    {
    //        //Case #0 - empties.
    //        if (a.IsEmpty || b.IsEmpty) return NewEmpty();

    //        //Case #1 - no overlap.
    //        int distance = a.ComparePositionTo(b);
    //        if (distance != 0) return NewEmpty();

    //        //Case #2 - two simple ranges.
    //        if (a.IsSimple && b.IsSimple)
    //        {
    //            Range3<T> overlap = GetIntersectionSimple(a, b);
    //            if (overlap == null) return NewEmpty();
    //            return overlap;
    //        }

    //        List<Range3<T>> overlaps = new List<Range3<T>>();
    //        //Case #3 - only 'a' is simple.
    //        if (a.IsSimple)
    //        {
    //            foreach (Range3<T> bRange in b.GetFlattened())
    //            {
    //                Range3<T> overlap = GetIntersectionSimple(a, bRange);
    //                if (overlap != null) overlaps.Add(overlap);
    //            }                
    //        }

    //        //Case #4 - only 'b' is simple.
    //        else if (b.IsSimple)
    //        {
    //            foreach (Range3<T> aRange in a.GetFlattened())
    //            {
    //                Range3<T> overlap = GetIntersectionSimple(aRange, b);
    //                if (overlap != null) overlaps.Add(overlap);
    //            }                
    //        }

    //        //Case #5 - neither  'a' nor 'b' are simple.
    //        else
    //        {


    //            ////Least common case - neither 'a' nor 'b' is simple.
    //            IEnumerator<Range3<T>> aFlattened = a.GetFlattened().GetEnumerator(), bFlattened = b.GetFlattened().GetEnumerator();
    //            bFlattened.MoveNext();
    //            aFlattened.MoveNext();
    //            Range3<T> aFocus, bFocus;

    //            //zigzag from 'a' to 'b'
    //            while (true)
    //            {
    //                aFocus = aFlattened.Current;
    //                bFocus = bFlattened.Current;
    //                Range3<T> overlap = GetIntersectionSimple(aFocus, bFocus);
    //                if (overlap == null)
    //                {
    //                    int cGap = a.ComparePositionTo(b);
    //                    if (cGap > 0 && !bFlattened.MoveNext()) break;
    //                }
    //                else
    //                    overlaps.Add(overlap);
    //                if (!aFlattened.MoveNext()) break;
    //            }
    //            //There might be some left unexamined in 'a'.
    //            while (aFlattened.MoveNext())
    //            {
    //                Range3<T> overlap = GetIntersectionSimple(aFlattened.Current, bFocus);
    //                if (overlap != null) overlaps.Add(overlap);
    //            }
    //            //Or, there might be some left unexamined in 'b'.
    //            while (bFlattened.MoveNext())
    //            {
    //                Range3<T> overlap = GetIntersectionSimple(bFlattened.Current, aFocus);
    //                if (overlap != null) overlaps.Add(overlap);
    //            }

    //            //The algorithm might be sorting anyway - is the sort call necessary?
    //            //overlaps.Sort((j, k) => CompareMinBounds(j, k));  
    //        }

    //        return FromRangeList(overlaps);
    //    }

    //    public static Range3<T> operator | (Range3<T> a, Range3<T> b)
    //    {
    //        if (a.IsEmpty) return b.Copy();
    //        if (b.IsEmpty) return a.Copy();
    //        if (a.IsInfinity || b.IsInfinity) return NewInfinity(); //This might be handled below.
    //        if (a.IsSimple && b.IsSimple) return GetUnionSimple(a, b);

    //        ////Least common case - neither 'a' nor 'b' is simple.
    //        IEnumerator<Range3<T>> aFlattened = a.GetFlattened().GetEnumerator(), bFlattened = b.GetFlattened().GetEnumerator();
    //        List<Range3<T>> newFlats = new List<Range3<T>>();
    //        bFlattened.MoveNext();
    //        aFlattened.MoveNext();
    //        Range3<T> aFocus, bFocus;

    //        //zigzag from 'a' to 'b'
    //        while (true)
    //        {
    //            aFocus = aFlattened.Current;
    //            bFocus = bFlattened.Current;
    //            Range3<T> combined = GetUnionSimple(aFocus, bFocus);
    //            if (combined.IsSimple)
    //                newFlats.Add(combined);
    //            else
    //            {
    //                newFlats.Add(combined.LeftChild);
    //                newFlats.Add(combined.RightChild);
    //            }
    //            if (aFocus.CompareMinTo(bFocus) > 0 && !bFlattened.MoveNext()) break;  //If a is equal or ahead, move b forward.
    //            if (!aFlattened.MoveNext()) break;
    //        }
    //        //There might be some left unexamined in 'a'.
    //        while (aFlattened.MoveNext())
    //        {
    //            Range3<T> combined = GetUnionSimple(aFlattened.Current, bFocus);
    //            if (combined.IsSimple)
    //                newFlats.Add(combined);
    //            else
    //            {
    //                newFlats.Add(combined.LeftChild);
    //                newFlats.Add(combined.RightChild);
    //            }
    //        }
    //        //Or, there might be some left unexamined in 'b'.
    //        while (bFlattened.MoveNext())
    //        {
    //            Range3<T> combined = GetUnionSimple(bFlattened.Current, aFocus);
    //            if (combined.IsSimple)
    //                newFlats.Add(combined);
    //            else
    //            {
    //                newFlats.Add(combined.LeftChild);
    //                newFlats.Add(combined.RightChild);
    //            }
    //        }

    //        //Is the sort call necessary?  Yes - any time a non-simple item is added, it may (likely) bugger up the sorting.
    //        newFlats.Sort((j, k) => j.CompareMinTo(k));  
    //        return FromRangeList(GetSimplified(newFlats));
    //    }

    //    public static Range3<T> operator | (Range3<T> range, T item)
    //    {
    //        if (range.IsEmpty) return NewSingleton(item);
    //        if (range.IsInfinity) return NewInfinity();

    //        //First, the special handling for the possible infinites of the head range.
    //        Range3<T> copy = range.Copy();
    //        int cMin = copy.CompareMinTo(item);
    //        if (cMin > 0)
    //        {
    //            if (copy.MinBound >= Bound.Infinite) return copy;
    //            return new Range3<T>(NewSingleton(item), copy);
    //        }
    //        if (cMin == 0) return copy;

    //        int cMax = copy.CompareMaxTo(item);
    //        if (cMax < 0)
    //        {
    //            if (copy.MaxBound >= Bound.Infinite) return copy;
    //            return new Range3<T>(copy, NewSingleton(item));
    //        }
    //        if (cMax == 0) return copy;

    //        Range3<T> bracketer = copy;
    //        while (true)
    //        {
    //            if (cMin == 0)
    //            {
    //                bracketer.SetMin(Bound.Include);
    //                return copy;
    //            }
    //            if (cMax == 0)
    //            {
    //                bracketer.SetMax(Bound.Include);
    //                return copy;
    //            }
    //            if (bracketer.IsSimple) return copy;

                
    //            int cMaxLeft = bracketer.LeftChild.CompareMaxTo(item), cMinRight = bracketer.RightChild.CompareMinTo(item);                                
    //            if (cMaxLeft < 0)
    //            {
    //                if (cMinRight == 0)
    //                {
    //                    bracketer.LeftChild = null;
    //                    bracketer.RightChild = null;
    //                    return copy;
    //                }
    //                cMax = cMaxLeft;
    //                bracketer = bracketer.LeftChild;
    //                continue;
    //            }

    //            else   //cMinRight > 0
    //            {
    //                if (cMaxLeft == 0)
    //                {
    //                    bracketer.LeftChild = null;
    //                    bracketer.RightChild = null;
    //                    return copy;
    //                }
    //                cMin = cMinRight;
    //                bracketer = bracketer.RightChild;
    //                continue;
    //            }
    //        }
    //    }


    //    public sealed override bool Equals(object obj)
    //    {
    //        Range3<T> other = obj as Range3<T>;
    //        if (other == null) return false;
    //        return this == other;
    //    }
    //    /// <summary>
    //    /// Returns the hash code for this Range.  A hash code for a Range will contain 30 salient bits, divided in half.  The first 
    //    /// 15 bits will be given by the value of the Min, if the MinBound is not infinite; otherwise, the bits will be determined 
    //    /// by the LeftChild's Min (or if there is no LeftChild, all 0's).  By analogy, the second half will come from the Max or 
    //    /// the RightChild's Max.
    //    /// <para/>Some edge cases are of note.  Empty ranges will return int.MaxValue, while Infinite ranges will return 0.
    //    /// </summary>        
    //    public override int GetHashCode()
    //    {
    //        if (IsEmpty) return int.MaxValue;


    //        // 2^15 = 32,768.
    //        int leftHalf = 0, rightHalf = 0;

    //        if (MinBound < Bound.Infinite) leftHalf = Min.GetHashCode() & 32768;
    //        else if (LeftChild != null) leftHalf = LeftChild.Min.GetHashCode() & 32768;

    //        if (MaxBound < Bound.Infinite) rightHalf = Min.GetHashCode();
    //        else if (RightChild != null) rightHalf = RightChild.Min.GetHashCode();

    //        return leftHalf | rightHalf;
    //    }
    //    public static bool operator ==(Range3<T> a, Range3<T> b)
    //    {
    //        if (object.ReferenceEquals(a, null)) return object.ReferenceEquals(b, null);
    //        if (object.ReferenceEquals(b, null)) return false;

    //        if (a.IsEmpty) return b.IsEmpty;
    //        if (b.IsEmpty) return a.IsEmpty;
    //        if (a.IsInfinity) return b.IsInfinity;
    //        if (b.IsInfinity) return a.IsInfinity;            

    //        Stack<Range3<T>> stackA = new Stack<Range3<T>>(), stackB = new Stack<Range3<T>>();
    //        stackA.Push(a);
    //        stackB.Push(b);
    //        while (stackA.Count > 0 && stackB.Count > 0)
    //        {
    //            Range3<T> aRange = stackA.Pop(), bRange = stackB.Pop();
    //            if (aRange.CompareMinTo(bRange) != 0)
    //                return false;
    //            if (aRange.CompareMaxTo(bRange) != 0)
    //                return false;
    //            if (aRange.LeftChild != null)
    //            {
    //                stackA.Push(aRange.LeftChild);
    //                stackA.Push(aRange.RightChild);
    //            }
    //            if (bRange.LeftChild != null)
    //            {
    //                stackB.Push(bRange.LeftChild);
    //                stackB.Push(bRange.RightChild);
    //            }
    //        }
    //        return stackA.Count == stackB.Count;
    //    }
    //    public static bool operator !=(Range3<T> a, Range3<T> b)
    //    {
    //        return !(a == b);
    //    }


    //    #endregion



    //    #region Range helpers


        


    //    /// <summary>
    //    /// Returns a bracketing comparison.  If this range brackets the other, returns 1 (as if this is greater than other).  If the other 
    //    /// brackets this range, returns a -1.  If there is no bracketing relationship, returns 0.
    //    /// </summary> 
    //    protected int CompareBracketingTo(Range3<T> other)
    //    {
    //        int cMin = CompareMinTo(other), cMax = CompareMaxTo(other);
    //        if (cMin < 0 && cMax > 0) return 1;
    //        if (cMin > 0 && cMax < 0) return -1;
    //        return 0;
    //    }
    //    /// <summary>
    //    /// Returns a containment comparison.  If this range contains the other, returns 1 (as if this is greater than other).  If the other 
    //    /// contains this range, returns a -1.  If there is no containment, returns 0.
    //    /// </summary> 
    //    protected int CompareContainmentTo(Range3<T> other)
    //    {
    //        int cMin = CompareMinTo(other), cMax = CompareMaxTo(other);
    //        if (cMin <= 0 && cMax >= 0) return 1;
    //        if (cMin >= 0 && cMax <= 0) return -1;
    //        return 0;
    //    }
    //    /// <summary>
    //    /// Compares the relative positions of two ranges.  A positive result signifies that the other range precedes this range, a 
    //    /// negative result signifies that this precedes the other, and a 0 signifies no gap exists.  If type T is a discrete type, 
    //    /// this method should return the magnitude of the difference between the max of the smaller and the min of the greater 
    //    /// range.
    //    /// </summary>
    //    protected int ComparePositionTo(Range3<T> other)
    //    {
    //        //other precedes this?
    //        int d = GetDistance(Min, other.Max);
    //        if (d >= 0)
    //        {
    //            //other precedes this?
    //            if (MinBound >= Bound.Infinite || other.MaxBound >= Bound.Infinite) return 0;
    //            if (d == 0 && MinBound >= Bound.Include && other.MaxBound >= Bound.Include) return 0;

    //            //Definitely a gap.
    //            if (MaxBound <= Bound.Exclude) d += (d > 0) ? 1 : -1;
    //            if (other.MinBound <= Bound.Exclude) d += (d > 0) ? 1 : -1;
    //            return d;
    //        }

    //        d = GetDistance(Max, other.Min);
    //        if (d <= 0)
    //        {
    //            //This precedes other?
    //            if (MaxBound >= Bound.Infinite || other.MinBound >= Bound.Infinite) return 0;
    //            if (d == 0 && MaxBound >= Bound.Include && other.MaxBound >= Bound.Include) return 0;

    //            //Definitely a gap.
    //            if (MaxBound <= Bound.Exclude) d += (d > 0) ? 1 : -1;
    //            if (other.MinBound <= Bound.Exclude) d += (d > 0) ? 1 : -1;
    //            return d;
    //        }

    //        //Definitely no gap from here.
    //        return 0;
    //    }



    //    /// <summary>
    //    /// Returns which boundary is lesser in relative terms.  An infinite boundary will be lesser than a finite boundary, a smaller Min 
    //    /// will be lesser than a lesser Min value, and an Included Min value will be lesser than an Excluded Min value.  This method tests 
    //    /// for consecutiveness as well, such that if an excluded Min immediately precedes and included Men, the two will reflect 
    //    /// equality.
    //    /// </summary>
    //    protected int CompareMinTo(Range3<T> other)
    //    {
    //        if (other.MinBound >= Bound.Infinite) return MinBound >= Bound.Infinite ? 0 : 1;
    //        if (MinBound >= Bound.Infinite) return -1;
    //        int d = GetDistance(Min, other.Min);
    //        if (d < -1) return -1;  //Non-consecutive.
    //        if (d > 1) return 1;    //Non-consecutive.
    //        if (d == -1) return MinBound <= Bound.Exclude && other.MinBound >= Bound.Include ? 0 : -1;  //Consecutive masking equal?
    //        if (d == 1) return MinBound >= Bound.Include && other.MinBound <= Bound.Exclude ? 0 : 1;    //Consecutive masking equal?
    //        //d==0
    //        return -MinBound.CompareTo(other.MinBound);     //Inverted cuz a higher bound level indicates preceding position.
    //    }
    //    /// <summary>
    //    /// Returns how the given item compares to the Min boundary in absolute terms.
    //    protected int CompareMinTo(T item)
    //    {            
    //        int d = GetDistance(Min, item);
    //        if (d < -1) return -1;
    //        if (d > 0) return (MinBound >= Bound.Infinite) ? -1 : 1;            
    //        if (d == -1) return MinBound <= Bound.Exclude ? 0 : -1;            
    //        //d==0
    //        return MinBound <= Bound.Exclude ? 1 : 0;
    //    }


    //    /// <summary>
    //    /// Returns which boundary is lesser in relative terms.  An infinite boundary will be greater than a finite boundary, a 
    //    /// larger Max will by greater than a small Min value, and in included Max will be greater than an Excluded Max value.  This 
    //    /// method tests for consecutiveness as well, such that if an included Max abuts the next Max which is excluded, the two will 
    //    /// reflect equality.
    //    /// </summary> 
    //    protected int CompareMaxTo(Range3<T> other)
    //    {
    //        if (other.MaxBound >= Bound.Infinite) return MaxBound >= Bound.Infinite ? 0 : -1;
    //        if (MaxBound >= Bound.Infinite) return 1;
    //        int d = GetDistance(Max, other.Max);
    //        if (d > 1) return 1;
    //        if (d < -1) return -1;
    //        if (d == 1) return MaxBound <= Bound.Exclude && other.MaxBound >= Bound.Include ? 0 : 1;
    //        if (d == -1) return MaxBound >= Bound.Include && other.MaxBound <= Bound.Exclude ? 0 : -1;
    //        //d==0
    //        return MaxBound.CompareTo(other.MaxBound);
    //    }
    //    /// <summary>
    //    /// Returns how the given item compares to the Max boundary in absolute terms.
    //    /// </summary>
    //    protected int CompareMaxTo(T item)
    //    {            
    //        int d = GetDistance(Max, item);
    //        if (d > 1) return 1;
    //        if (d < 0) return MaxBound >= Bound.Infinite ? 1 : -1;
    //        if (d == 1) return MaxBound <= Bound.Exclude ? 0 : 1;
    //        //d==0
    //        return MaxBound <= Bound.Exclude ? -1 : 0;
    //    }



      
        

    //    /// <summary>
    //    /// Returns the containing range within this overall range, if it exists.  If there is no such range, returns null.
    //    /// <para/>A containing range will have boundaries equal to or outside those of the given range.
    //    /// </summary>
    //    protected Range3<T> GetContainer(T item)
    //    {
    //        Stack<Range3<T>> stack = new Stack<Range3<T>>();
    //        Range3<T> result = null;
    //        stack.Push(this);
    //        while (stack.Count > 0)
    //        {
    //            Range3<T> focus = stack.Pop();
    //            int cMin = focus.CompareMinTo(item), cMax = focus.CompareMaxTo(item);
    //            if (cMin > 0 || cMax < 0) continue;
    //            if (focus.IsSimple) return focus;
    //            stack.Push(focus.RightChild);
    //            stack.Push(focus.LeftChild);
    //            result = focus;
    //        }
    //        return result;
    //    }
    //    /// <summary>
    //    /// Returns the containing range within this overall range, if it exists.  If there is no such range, returns null.
    //    /// <para/>A containing range will have boundaries equal to or outside those of the given range.
    //    /// </summary>
    //    protected Range3<T> GetContainer(Range3<T> other)
    //    {
    //        Stack<Range3<T>> stack = new Stack<Range3<T>>();
    //        Range3<T> result = null;
    //        stack.Push(this);
    //        while (true)
    //        {
    //            Range3<T> focus = stack.Pop();
    //            if (focus.CompareContainmentTo(other) != 1) return result;
    //            if (focus.IsSimple) return focus;
    //            stack.Push(focus.RightChild);
    //            stack.Push(focus.LeftChild);
    //            result = focus;
    //        }

    //    }


    //    protected static Range3<T> GetDifferenceSimple(Range3<T> a, Range3<T> b)
    //    {
    //        int d = a.ComparePositionTo(b);
    //        if (d != 0) return a.Copy();    //No overlap.

    //        int cMin = a.CompareMinTo(b), cMax = a.CompareMaxTo(b);

    //        //Look for containment.
    //        if (cMin >= 0 && cMax <= 0) return null;    //a contains b.
    //        if (cMin <= 0 && cMax >= 0)                 //b contains a.
    //        {
    //            Range3<T> left = new Range3<T>(a.Min, a.MinBound, b.Min, b.MinBound <= Bound.Exclude ? Bound.Include : Bound.Exclude);
    //            Range3<T> right = new Range3<T>(b.Max, b.MaxBound <= Bound.Exclude ? Bound.Include : Bound.Exclude, a.Max, a.MaxBound);
    //            return new Range3<T>(left, right);
    //        }

    //        //Nothing is infinite from here, otherwise one would have contained the other.
    //        if (cMin < 0)
    //        {
    //            int cMaxMin = a.Max.CompareTo(b.Min);
    //            if (cMaxMin==0) return new Range3<T>(a.Min, a.MinBound, a.Max, b.MinBound >= Bound.Include ? Bound.Exclude : a.MaxBound);
    //            return new Range3<T>(a.Min, a.MinBound, b.Min, b.MinBound >= Bound.Include ? Bound.Exclude : Bound.Include);
    //        }
    //        else if (cMin > 0)
    //        {
    //            int cMinMax = a.Min.CompareTo(b.Max);
    //            if (cMinMax == 0) return new Range3<T>(a.Min, b.MaxBound >= Bound.Include ? Bound.Exclude : Bound.Include, a.Max, a.MaxBound);
    //            return new Range3<T>(b.Max, b.MaxBound >= Bound.Include ? Bound.Exclude : Bound.Include, a.Max, a.MaxBound);
    //        }
    //        else  //cMin == 0
    //        {
    //            return new Range3<T>(b.Max, b.MaxBound >= Bound.Include ? Bound.Exclude : Bound.Include, a.Max, a.MaxBound);
    //        }
    //    }


    //    protected List<Range3<T>> GetFlattened()
    //    {
    //        List<Range3<T>> result = new List<Range3<T>>();
    //        Stack<Range3<T>> stack = new Stack<Range3<T>>();
    //        stack.Push(this);
    //        while (stack.Count > 0)
    //        {
    //            Range3<T> focus = stack.Pop();
    //            if (focus.IsSimple) result.Add(focus);
    //            else
    //            {
    //                stack.Push(focus.RightChild);
    //                stack.Push(focus.LeftChild);
    //            }
    //        }
    //        return result;
    //    }
    //    /// <summary>
    //    /// Returns the intersection of two simple ranges, or {a and b}.  If there is no intersection, the result will be null, but 
    //    /// otherwise the result will always be simple.
    //    /// </summary>
    //    protected static Range3<T> GetIntersectionSimple(Range3<T> a, Range3<T> b)
    //    {

    //        if (a.ComparePositionTo(b) != 0) return null;
    //        int c = a.CompareContainmentTo(b);
    //        if (c > 0) return b.Copy();
    //        if (c < 0) return a.Copy();

    //        Range3<T> result = new Range3<T>();
    //        result.Min = GetMax(a.Min, b.Min);
    //        result.MinBound = GetMin(a.MinBound, b.MinBound);
    //        result.Max = GetMin(a.Max, b.Max);
    //        result.MaxBound = GetMin(a.MaxBound, b.MaxBound);

    //        return result;
    //    }
    //    //protected enum Overlap { A_precedes_B, A_laps_B, A_contains_B, B_contains_A, B_laps_A, B_precedes_A, Invalid};


    //    /// <summary>
    //    /// Returns the larger of the two tvalues, or 'a' if they are equal.
    //    /// </summary> 
    //    protected static T GetMax(T a, T b)
    //    {
    //        return a.CompareTo(b) >= 0 ? a : b;
    //    }
    //    protected static Bound GetMax(Bound a, Bound b)
    //    {
    //        return a >= b ? a : b;
    //    }
    //    /// <summary>
    //    /// Returns the smaller of the two values, or 'a' if they are equal.
    //    /// </summary>
    //    protected static T GetMin(T a, T b)
    //    {
    //        return a.CompareTo(b) <= 0 ? a : b;
    //    }
    //    protected static Bound GetMin(Bound a, Bound b)
    //    {
    //        return a <= b ? a : b;
    //    }


    //    protected static IEnumerable<Range3<T>> GetSimplified(IEnumerable<Range3<T>> rangeList)
    //    {
    //        List<Range3<T>> simples = new List<Range3<T>>();
    //        Range3<T> constructing = null;
    //        foreach (Range3<T> leaf in rangeList)
    //        {
    //            if (constructing == null)
    //            {
    //                constructing = leaf.Copy();
    //                continue;
    //            }
    //            int cPos = constructing.ComparePositionTo(leaf);
    //            if (cPos == 0)
    //            {
    //                int cMin = constructing.CompareMinTo(leaf), cMax = constructing.CompareMaxTo(leaf);
    //                if (cMin > 0)
    //                {
    //                    constructing.Min = leaf.Min;
    //                    constructing.MinBound = leaf.MinBound;
    //                }
    //                else if (cMin == 0)
    //                {
    //                    if (constructing.Min.CompareTo(leaf.Min) > 0)
    //                    {
    //                        constructing.Min = leaf.Min;
    //                        constructing.MinBound = leaf.MinBound;
    //                    }
    //                }
    //                if (cMax < 0)
    //                {
    //                    constructing.Max = leaf.Max;
    //                    constructing.MaxBound = leaf.MaxBound;
    //                }
    //                else if (cMax == 0)
    //                {
    //                    if (constructing.Max.CompareTo(leaf.Max) < 0)
    //                    {
    //                        constructing.Max = leaf.Max;
    //                        constructing.MinBound = leaf.MinBound;
    //                    }
    //                }
    //            }

    //            else
    //            {
    //                simples.Add(constructing);
    //                constructing = leaf.Copy();
    //            }
    //        }

    //        if (constructing != null) simples.Add(constructing);
            
    //        return simples;
    //    }
        

    //    /// <summary>
    //    /// For two simple Range objects, returns {a or b}.  The result will never be null, but in the case of two non-overlapping ranges, 
    //    /// the result will not be simple.
    //    /// </summary>        
    //    protected static Range3<T> GetUnionSimple(Range3<T> a, Range3<T> b)
    //    {
    //        int c = a.ComparePositionTo(b);
    //        if (c < -1) return new Range3<T>(a.Copy(), b.Copy());
    //        if (c > 1) return new Range3<T>(b.Copy(), a.Copy());
    //        if (c == -1)
    //        {
    //            if (a.MaxBound >= Bound.Infinite || b.MinBound >= Bound.Infinite) return new Range3<T>(a.Min, a.MinBound, b.Max, b.MaxBound);
    //            return new Range3<T>(a.Copy(), b.Copy());
    //        }
    //        if (c == 1)
    //        {
    //            if (a.MinBound >= Bound.Infinite || b.MaxBound >= Bound.Infinite) return new Range3<T>(b.Min, b.MinBound, a.Max, a.MaxBound);
    //            return new Range3<T>(b.Copy(), a.Copy());
    //        }
    //        return new Range3<T>(GetMin(a.Min, b.Min), GetMax(a.MinBound, b.MinBound), GetMax(a.Max, b.Max), GetMax(a.MaxBound, b.MaxBound));
    //    }

    //    /// <summary>
    //    /// Ensures the given range and all its right descendants have an identical max boundary.
    //    /// </summary>
    //    private void SetMax(T newMax, Bound newMaxBound)
    //    {
    //        Range3<T> focus = this;
    //        while (focus != null)
    //        {
    //            focus.Max = newMax;
    //            focus.MaxBound = newMaxBound;
    //            focus = focus.RightChild;
    //        }
    //    }
    //    /// <summary>
    //    /// Ensures the given range and all its right descendants have an identical max boundary type.
    //    /// </summary>
    //    private void SetMax(Bound newMaxBound)
    //    {
    //        Range3<T> focus = this;
    //        while (focus != null)
    //        {             
    //            focus.MaxBound = newMaxBound;
    //            focus = focus.RightChild;
    //        }
    //    }

    //    /// <summary>
    //    /// Ensures the given Range and all its left descendants have an identical min boundary.
    //    /// </summary>
    //    private void SetMin(T newMin, Bound newMinBound)
    //    {
    //        Range3<T> focus = this;
    //        while (focus != null)
    //        {
    //            focus.Min = newMin;
    //            focus.MinBound = newMinBound;                
    //            focus = focus.LeftChild;
    //        }
    //    }
    //    // <summary>
    //    /// Ensures the given Range and all its left descendants have an identical min boundary type.
    //    /// </summary>
    //    private void SetMin(Bound newMinBound)
    //    {
    //        Range3<T> focus = this;
    //        while (focus != null)
    //        {                
    //            focus.MinBound = newMinBound;
    //            focus = focus.LeftChild;
    //        }
    //    }
      


    //    public override string ToString()
    //    {
    //        if (IsEmpty) return "{Empty}";
    //        if (IsInfinity) return "{Infinity}";

    //        StringBuilder sb = new StringBuilder();
    //        IEnumerator<Range3<T>> iterator = GetFlattened().GetEnumerator();
    //        iterator.MoveNext();
    //        while (true)
    //        {
    //            sb.Append(iterator.Current.ToStringSimple());
    //            if (iterator.MoveNext()) sb.Append("  ");
    //            else break;
    //        }
    //        return sb.ToString();
    //    }
    //    protected string ToStringSimple()
    //    {
    //        return (MinBound == Bound.Exclude ? "(" : MinBound == Bound.Include ? "[" : "{")
    //               + Min.ToString()
    //               + ","
    //               + Max.ToString()
    //               + (MaxBound == Bound.Exclude ? ")" : MaxBound == Bound.Include ? "]" : "}");
    //    }

    //    /// <summary>
    //    /// Creates a graph object structuring all included sub-ranges of this Range, into a binary tree.  The head of the tree is returned.
    //    /// </summary>
    //    /// <returns></returns>
    //    public Graphs.Vertex<Range3<T>> ToGraph()
    //    {     
    //        Dictionary<Range3<T>, Graphs.Vertex<Range3<T>>> vertices = new Dictionary<Range3<T>, Graphs.Vertex<Range3<T>>>();
    //        Graphs.Vertex<Range3<T>> result = new Graphs.Vertex<Range3<T>>(this);          
    //        vertices.Add(this, result);
    //        Stack<Range3<T>> toGraph = new Stack<Range3<T>>();
    //        toGraph.Push(this);
    //        while (toGraph.Count > 0)
    //        {
    //            Range3<T> origin = toGraph.Pop();
    //            if (origin.IsSimple) continue;

    //            Graphs.Vertex<Range3<T>> originVertex = vertices[origin];                
    //            Graphs.Vertex<Range3<T>> leftVertex = new Graphs.Vertex<Range3<T>>(origin.LeftChild);
    //            Graphs.Vertex<Range3<T>> RightVertex = new Graphs.Vertex<Range3<T>>(origin.RightChild);
    //            vertices.Add(origin.LeftChild, leftVertex);
    //            vertices.Add(origin.RightChild, RightVertex);
    //            originVertex.Edges.Add(new Graphs.Edge<Range3<T>>(originVertex, leftVertex));
    //            originVertex.Edges.Add(new Graphs.Edge<Range3<T>>(originVertex, RightVertex));

    //            toGraph.Push(origin.RightChild);
    //            toGraph.Push(origin.LeftChild);
    //        }
    //        return result;
    //    }

    //    #endregion




    //}

}
