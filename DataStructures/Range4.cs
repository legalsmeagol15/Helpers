using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{
    public class Range4<T> where T : IComparable<T>
    {

        #region Range properties

        protected Bound Min { get; private set; }
        protected Bound Max { get; private set; }
       
        protected Range4<T> LeftChild { get; private set; } = null;
        protected Range4<T> RightChild { get; private set; } = null;



        public bool IsEmpty { get { return Min.Status == Inclusion.Exclude && Max.Status == Inclusion.Exclude; } }
        protected bool IsSimple { get { return LeftChild == null && RightChild == null; } }
        public bool IsInfinite { get { return Min.IsInfiniteBefore && Max.IsInfiniteAfter && Min.IsIncluded && Max.IsIncluded; } }
        public bool IsSingleton { get { return Min.Value != null && Max.Value != null && Min.CompareTo(Max) == 0 
                                               && Min.Status == Inclusion.Singleton && Max.Status == Inclusion.Singleton; } }

        #endregion


        #region Range constructors

        static Range4()
        {
            Bound.GetValueDistance = (T a, T b) =>
                                               {
                                                   if (a is Int32 && b is Int32) return int.Parse(a.ToString()) - int.Parse(b.ToString());
                                                   int c = a.CompareTo(b);
                                                   if (c < 0) return int.MinValue;
                                                   if (c > 0) return int.MaxValue;
                                                   return 0;
                                               };

            //TODO:  Range static ctor  - remove the special handling of int, which is only there for development purposes.
        }
       
        public Range4(T min, T max) 
        {
            if (min.CompareTo(max) == 0)
            {
                Min = new Bound(min, Inclusion.Singleton);
                this.Max = new Bound(max, Inclusion.Singleton);

            }
            else
            {
                this.Min = new Bound(min, Inclusion.IncludesAfter | Inclusion.IncludesBound);
                Max = new Bound(max, Inclusion.IncludesBefore | Inclusion.IncludesBound);
            }
            
        }
        protected Range4(T min, Inclusion minBound, T max, Inclusion maxBound)
        {
            this.Min = new Bound(min, minBound);
            this.Max = new Bound(max, maxBound);            
        }
        public static Range4<T> NewSingleton(T item)
        {
            if (item == null) throw new ArgumentException("Range<T> singleton item cannot be null.");
            return new Range4<T>(item, Inclusion.Singleton, item, Inclusion.Singleton);
        }
        public static Range4<T> NewEmpty()
        {
            return new Range4<T>(default(T), Inclusion.Exclude, default(T), Inclusion.Exclude);
        }
        public static Range4<T> NewInfinity()
        {
            return new Range4<T>(default(T), Inclusion.IncludesInfiniteBefore, default(T), Inclusion.IncludesInfiniteAfter);
        }
        public Range4<T> Copy()
        {
            Range4<T> copy = new Range4<T>(Min.Value, Min.Status, Max.Value, Max.Status);
            if (LeftChild != null) copy.LeftChild = LeftChild.Copy();
            if (RightChild != null) copy.RightChild = RightChild.Copy();
            return copy;
        }
        protected Range4(Range4<T> leftChild, Range4<T> rightChild)
        {
            this.Min = new Bound(leftChild.Min.Value, leftChild.Min.Status);
            this.Max = new Bound(rightChild.Max.Value, rightChild.Max.Status);
            this.LeftChild = leftChild;
            this.RightChild = rightChild;
        }

        protected static Range4<T> FromRangeList(IEnumerable<Range4<T>> rangeList)
        {
            if (rangeList.Count() == 0) return NewEmpty();

            //Returns a range representing the head of a balanced, filled left-to-right tree.
            Queue<Range4<T>> queueA = new Queue<Range4<T>>(rangeList);
            Queue<Range4<T>> queueB = new Queue<Range4<T>>();
            while (queueA.Count > 1)
            {
                Range4<T> left = queueA.Dequeue();
                Range4<T> right = queueA.Dequeue();
                Range4<T> newRange = new Range4<T>(left, right);
                queueB.Enqueue(newRange);
                if (queueA.Count == 1) queueB.Enqueue(queueA.Dequeue());
                if (queueA.Count == 0)
                {
                    //Switch the references.
                    Queue<Range4<T>> temp = queueA;
                    queueA = queueB;
                    queueB = temp;
                }
            }
            return queueA.Peek();
        }

        #endregion



        #region Range contents queries

        public bool Contains(T item)
        {
            Range4<T> container = GetContainer(item);
            return container != null;
        }
        public Range4<T> GetBracketer(T item)
        {
            if (item.CompareTo(Min.Value) < 0 || Max.Value.CompareTo(item) < 0) return null;

            Range4<T> focus = this;
            while (true)
            {                
                if (focus.IsSimple) return focus;
                int cMin = item.CompareTo(focus.LeftChild.Max.Value), cMax = focus.RightChild.Min.Value.CompareTo(item);
                //if (cMin == 0 && cMax == 0) return focus;
                if (cMin <= 0) focus = focus.LeftChild;
                else if (cMax <= 0) focus = focus.RightChild;
                else return focus;
            }            
        }
        public Range4<T> GetContainer(T item)
        {
            Range4<T> result = null, focus = this;
            while (true)
            {
                if (item < focus.Min || focus.Max < item) break;
                if (focus.IsSimple) return focus;
                result = focus;
                if (focus.LeftChild.Max >= item) focus = focus.LeftChild;
                else if (focus.RightChild.Min <= item) focus = focus.RightChild;
                else return null;

            }
            return result;
        }

        #endregion



        #region Range operators

        public override int GetHashCode()
        {
            throw new NotImplementedException("Unless GetHashCode() is overridden in an inheriting class, Ranges are not hashable.");
        }
        public override bool Equals(object obj)
        {
            Range4<T> other = obj as Range4<T>;
            if (other == null) return false;
            return other == this;
        }

        public static bool operator ==(Range4<T> a, Range4<T> b)
        {
            if (ReferenceEquals(a, null)) return ReferenceEquals(b, null);
            if (ReferenceEquals(b, null)) return false;

            IEnumerator<Bound> aBounds = a.GetAllBounds().GetEnumerator(), bBounds = b.GetAllBounds().GetEnumerator();
            while (true)
            {
                bool aMoved = aBounds.MoveNext();
                bool bMoved = bBounds.MoveNext();
                if (aMoved ^ bMoved) return false;  //different count in the two sets.
                if (!aMoved) return true;           //same count, but now we're done with the sets.
                if (aBounds.Current != bBounds.Current) return false;
            }
        }
        public static bool operator !=(Range4<T> a, Range4<T> b)
        {
            return !(a == b);
        }

        public static Range4<T> operator |(Range4<T> range, T item)
        {
            range = range.Copy();

            if (item < range.Min)
            {
                int d = Bound.GetValueDistance(item, range.Min.Value);
                if (d == 0)
                {
                    range.SetMin(item, Inclusion.IncludesBound);
                    return range;
                }
                if (d != -1 || !range.Min.IsIncluded) return new Range4<T>(NewSingleton(item), range);
                range.SetMin(item, range.Min.Status);
                return range;
            }
            if (range.Max < item)
            {
                int d = Bound.GetValueDistance(range.Max.Value, item);
                if (d == 0)
                {
                    range.SetMax(item, Inclusion.IncludesBound);
                    return range;
                }
                if (d != -1 || !range.Max.IsIncluded) return new Range4<T>(range, NewSingleton(item));
                range.SetMax(item, range.Max.Status);
                return range;
            }

            Range4<T> bracketer = range.GetBracketer(item);
            int dMin = Bound.GetValueDistance(item, bracketer.Min.Value), dMax = Bound.GetValueDistance(bracketer.Max.Value, item);
            if (dMin == 0 && !bracketer.Min.IsIncluded) bracketer.SetMin(item, Inclusion.IncludesBound);
            if (dMax == 0 && !bracketer.Max.IsIncluded) bracketer.SetMax(item, Inclusion.IncludesBound);
            if (bracketer.IsSimple) return range;
            throw new NotImplementedException();
        }

        public static Range4<T> operator |(Range4<T> a, Range4<T> b)
        {
            List<Bound> allBounds = a.GetAllBounds();
            allBounds.AddRange(b.GetAllBounds());            
            allBounds.Sort((j, k) => j.CompareTo(k));
            IEnumerator<Bound> iterator = allBounds.GetEnumerator();

            
            List<Range4<T>> overlapping = new List<Range4<T>>();
            if (!iterator.MoveNext()) return NewEmpty();
            Range4<T> constructing = new Range4<T>(iterator.Current.Value, iterator.Current.Status, 
                                                 iterator.Current.Value, Inclusion.InfiniteAfter);
            int lapping = 1;
            while (iterator.MoveNext())
            {
                constructing.Min = new Bound(GetMin(constructing.Min.Value, iterator.Current.Value), constructing.Min.Status);
                constructing.Max = new Bound(GetMax(constructing.Max.Value, iterator.Current.Value), constructing.Max.Status);
                if (!iterator.Current.IsInfiniteBefore && !iterator.Current.IncludesBefore) lapping++;
                if (!iterator.Current.IsInfiniteAfter && !iterator.Current.IncludesAfter && --lapping == 0)
                {
                    constructing.Max = new Bound(constructing.Max.Value, iterator.Current.Status);
                    if (!iterator.MoveNext()) break;

                    //Contiguous?
                    int d = Bound.GetValueDistance(constructing.Max.Value, iterator.Current.Value);
                    if (d == -1 && constructing.Max.IsIncluded && iterator.Current.IsIncluded)
                    {
                        constructing.Min = new Bound(GetMin(constructing.Min.Value, iterator.Current.Value), constructing.Min.Status);
                        constructing.Max = new Bound(GetMax(constructing.Max.Value, iterator.Current.Value), constructing.Max.Status);
                    }
                    else
                    {
                        overlapping.Add(constructing);
                        constructing = new Range4<T>(iterator.Current.Value, iterator.Current.Status,
                                                    iterator.Current.Value, Inclusion.InfiniteAfter);
                    }
                    lapping = 1;
                }
            }
            //Is the following .Max assignment needed?
            constructing.Max = new Bound(GetMax(constructing.Max.Value, allBounds.Last().Value), allBounds.Last().Status);
            overlapping.Add(constructing);
            
            return FromRangeList(overlapping);
        }

        #endregion



        #region Range boundary members

        protected struct Bound : IComparable<Bound>, IComparable<T>
        {
            public T Value { get; private set; }
            public Inclusion Status { get; private set; }

            /// <summary>
            /// Creates a new Bound with the given value and inclusion status.
            /// </summary>
            public Bound(T value, Inclusion includes)
            {
                Value = value;
                Status = includes;
            }

            public bool IsInfiniteAfter { get { return (Status & Inclusion.InfiniteAfter) > 0; } }
            public bool IsInfiniteBefore { get { return (Status & Inclusion.InfiniteBefore) > 0; } }
            public bool IsIncluded { get { return (Status & Inclusion.IncludesBound) > 0; } }
            public bool IncludesBefore { get { return (Status & Inclusion.IncludesBefore) > 0; } }
            public bool IncludesAfter { get { return (Status & Inclusion.IncludesAfter) > 0; } }
            
            public static Func<T,T,int> GetValueDistance { get; set; }



            #region Bound-to-T comparison

            public int CompareTo(T other)
            {
                int c = Value.CompareTo(other);
                if (c < 0) return (IsInfiniteAfter || IncludesAfter) ? 0 : -1;
                if (c > 0) return (IsInfiniteBefore || IncludesBefore) ? 0 : 1;

                //c==0
                if (IsIncluded) return 0;
                if (IsInfiniteBefore) return -1;
                if (IsInfiniteAfter) return 1;
                if (IncludesBefore) return -1;
                if (IncludesAfter) return 1;

                throw new InvalidOperationException("Should never get to this point.");
            }

            public static bool operator >(Bound bound, T item)
            {
                return bound.CompareTo(item) > 0;
            }
            public static bool operator <(Bound bound, T item)
            {
                return bound.CompareTo(item) < 0;
            }
            public static bool operator <(T item, Bound bound)
            {
                return bound > item;
            }
            public static bool operator >(T item, Bound bound)
            {
                return bound < item;
            }
            public static bool operator ==(Bound bound, T item)
            {
                return bound.CompareTo(item) == 0;
            }
            public static bool  operator !=(Bound bound, T item)
            {
                return bound.CompareTo(item) != 0;
            }
            public static bool  operator ==(T item,  Bound bound)
            {
                return bound == item;
            }
            public static bool operator !=(T item, Bound bound)
            {
                return bound != item;
            }
            public static bool operator >=(T item, Bound bound)
            {
                return bound <= item;
            }
            public static bool operator  <=(T item, Bound bound)
            {
                return bound >= item;
            }
            public static bool operator >=(Bound bound, T item)
            {
                return bound.CompareTo(item) >= 0;
            }
            public static bool operator <=(Bound bound, T item)
            {
                return bound.CompareTo(item) <= 0;
            }
            public bool Includes(T item)
            {
                return CompareTo(item) == 0;
            }
            

            #endregion



            #region Bound-to-Bound comparison

            public int CompareTo(Bound other)
            {
                int valueDistance = GetValueDistance(this.Value, other.Value);

                //Case #1
                if (valueDistance <= -1)
                {
                    if (valueDistance == -1)
                    {
                        if (!other.IsIncluded) return IsIncluded ? 0 : 1;
                        if (!IsIncluded) return 0;
                    }
                    if (other.IsInfiniteBefore) return IsInfiniteAfter ? 0 : 1;
                    if (IsInfiniteAfter) return 1;
                    return -1;
                }

                //Case #2
                if (valueDistance >= 1)
                {
                    if (valueDistance == 1)
                    {
                        if (!other.IsIncluded) return IsIncluded ? 0 : -1;
                        if (!IsIncluded) return 0;
                    }
                    if (other.IsInfiniteAfter) return IsInfiniteBefore ? 0 : -1;
                    if (IsInfiniteBefore) return -1;
                    return 1;
                }

                //Case #3  - value distance == 0
                Inclusion diff = Status ^ other.Status;   //returns 'a' XOR 'b', meaning all the bound chars where only one or the other has it.
                if ((diff & Inclusion.InfiniteAfter) > 0) return IsInfiniteAfter ? 1 : -1;
                if ((diff & Inclusion.InfiniteBefore) > 0) return IsInfiniteBefore ? -1 : 1;
                if ((diff & Inclusion.IncludesBound) == 0) return 0;

                //From here, one bound is included but the other is excluded.
                if (IncludesBefore) return other.IncludesBefore ? 0 : -1;
                if (IncludesAfter) return other.IncludesAfter ? 0 : 1;

                return 0;
            }

            public static bool operator >(Bound a, Bound b)
            {
                return a.CompareTo(b) > 0;
            }


            public static bool operator <(Bound a, Bound b)
            {
                return b.CompareTo(a) > 0;
            }
            public static bool operator ==(Bound a, Bound b)
            {
                return a.CompareTo(b) == 0;
            }
            public static bool operator !=(Bound a, Bound b)
            {
                return a.CompareTo(b) != 0;
            }

            #endregion



            public override bool Equals(object obj)
            {
                if (obj is Bound) return (Bound)obj == this;
                if (obj is T) return this.Status == Inclusion.IncludesBound && ((T)obj).CompareTo(Value) == 0;
                return false;
            }
            public override int GetHashCode()
            {
                throw new InvalidOperationException("Because two operators with unequal values may still allow Bounds to be equal, there is no"
                                                    + " way to accurately hash two Bounds.");
            }

            public override string ToString()
            {
                if (IncludesAfter)
                {
                    if (IsInfiniteBefore) return "{" + Value.ToString();
                    if (IsIncluded) return "[" + Value.ToString();
                    return "(" + Value.ToString();
                }
                else if (IncludesBefore)
                {
                    if (IsInfiniteAfter) return Value.ToString() + "}";
                    if (IsIncluded) return Value.ToString() + "]";
                    return Value.ToString() + ")";
                }
                return Value.ToString();
            }

        }
        

        [Flags]
        protected enum Inclusion
        {
            Exclude = 0,
            IncludesBound = 1,
            IncludesBefore = 2,
            IncludesAfter = 4,
            InfiniteBefore = 8,
            InfiniteAfter = 16,

            RangeMin = 5,
            RangeMax = 3,
            IncludesInfiniteBefore = 9,
            IncludesInfiniteAfter = 17,
            Singleton = 1,
            Infinite = 31
        }

        #endregion



        #region Range helpers

        

        protected List<Bound> GetAllBounds()
        {
            Stack<Range4<T>> stack = new Stack<Range4<T>>();
            stack.Push(this);
            List<Bound> result = new List<Bound>();
            while (stack.Count > 0)
            {
                Range4<T> focus = stack.Pop();
                if (focus.IsSimple)
                {
                    result.Add(focus.Min);
                    result.Add(focus.Max);
                    continue;
                }
                stack.Push(focus.RightChild);
                stack.Push(focus.LeftChild);
            }
            return result;
        }

     
        

        protected List<Range4<T>> GetFlattened()
        {
            List<Range4<T>> result = new List<Range4<T>>();
            Stack<Range4<T>> stack = new Stack<Range4<T>>();
            stack.Push(this);
            while (stack.Count > 0)
            {
                Range4<T> focus = stack.Pop();
                if (focus.IsSimple) result.Add(focus);
                else
                {
                    stack.Push(focus.RightChild);
                    stack.Push(focus.LeftChild);
                }
            }
            return result;
        }


        public static T GetMin(T a, T b)
        {
            return a.CompareTo(b) <= 0 ? a : b;
        }
        public static T GetMax(T a, T b)
        {
            return a.CompareTo(b) >= 0 ? a : b;
        }
        
        protected void SetMin(T value, Inclusion status)
        {
            Range4<T> focus = this;
            while (focus!= null)
            {
                focus.Min = new Bound(value, status);
                focus = focus.LeftChild;
            }
        }
        protected void SetMax(T value, Inclusion status)
        {
            Range4<T> focus = this;
            while (focus != null)
            {
                focus.Max = new Bound(value, status);
                focus = focus.RightChild;
            }
        }


        public override string ToString()
        {            
            if (IsEmpty) return "{Empty}";
            if (IsInfinite) return "{Infinity}";

            StringBuilder sb = new StringBuilder();
            IEnumerator<Range4<T>> iterator = GetFlattened().GetEnumerator();
            iterator.MoveNext();
            while (true)
            {
                sb.Append(iterator.Current.Min.ToString() + "," + iterator.Current.Max.ToString());
                if (iterator.MoveNext()) sb.Append("  ");
                else break;
            }
            return sb.ToString();
        }
        

        #endregion
    }
}
