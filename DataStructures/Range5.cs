using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{
    public class Range5<T> where T : IComparable<T>
    {

        protected SkipList<Bound> Bounds = new SkipList<Bound>();
        

        protected struct Bound : IComparable<Bound>
        {
            public T Value;
            public bool IncludesBefore;
            public bool IncludesAfter;
            public bool IsInfinity { get { return IncludesBefore && IncludesAfter; } }            
            public bool IsSingleton { get { return Value != null && !(IncludesBefore || IncludesAfter); } }

            public static Func<T, T, int> GetDistance { get; set; } = (a, b) =>
              {
                  if (typeof(T) == typeof(int))                  
                      return int.Parse(a.ToString()) - int.Parse(b.ToString());
                  
                  int c = a.CompareTo(b);
                  if (c < 0) return int.MinValue;
                  if (c > 0) return int.MaxValue;
                  return 0;
              };

            public Bound(T value)
            {
                Value = value;
                IncludesBefore = false;
                IncludesAfter = false;
            }
            public Bound(T value, bool includesBefore, bool includesAfter) 
            {
                Value = value;
                IncludesAfter = includesAfter;
                IncludesBefore = includesBefore;
            }

            [System.Diagnostics.DebuggerStepThrough]
            public int CompareTo(Bound other) { return Value.CompareTo(other.Value); }
            public static bool operator >(Bound a, Bound b) { return a.Value.CompareTo(b.Value) > 0; }
            public static bool operator <(Bound a, Bound b) { return a.Value.CompareTo(b.Value) < 0; }
            public static bool operator ==(Bound a, Bound b) { return a.Value.CompareTo(b.Value) == 0; }
            public static bool operator !=(Bound a, Bound b) { return a.Value.CompareTo(b.Value) != 0; }
            public static bool operator >=(Bound a, Bound b) { return a.Value.CompareTo(b.Value) >= 0; }
            public static bool operator <=(Bound a, Bound b) { return a.Value.CompareTo(b.Value) <= 0; }
            public static int  operator -(Bound a, Bound b) { return GetDistance(a.Value, b.Value); }
            

            public override bool Equals(object obj)
            {
                if (obj is Bound) return (Bound)obj == this;
                return false;
            }
            public override int GetHashCode()
            {
                return Value.GetHashCode() + (IncludesBefore ? 1 : 0) + (IncludesAfter ? 2 : 0);
            }

            public override string ToString()
            {
                return "Bound:" + (IncludesBefore ? "[" : "(") + Value.ToString() + (IncludesAfter ? "]" : ")");
            }
        }


        #region DiscreteRange constructors

        public void SetDistanceFunction(Func<T,T,int> func)
        {
            Bound.GetDistance = func;
        }

        public Range5(T min, T max) : this(new Bound(min, false, true), new Bound(max, true, false)) { }
        private Range5()
        {

        }
        protected Range5(Bound min, Bound max)
        {
            Bounds.Add(min);
            Bounds.Add(max);
        }
        private Range5(IEnumerable<Bound> bounds)
        {
            this.Bounds = new SkipList<Bound>(bounds);
        }


        public Range5<T> Copy()
        {
            Range5<T> result = new Range5<T>();
            foreach (Bound b in Bounds) result.Bounds.Add(b);
            return result;
        }
        public static Range5<T> NewEmpty() { return new Range5<T>(); }
        public static Range5<T> NewInfinity()
        {
            Range5<T> result = new Range5<T>();
            result.Bounds.Add(new Bound(default(T), true, true));
            return result;
        }
        public static Range5<T> NewSingleton(T item)
        {
            Range5<T> result = new Range5<T>();
            result.Bounds.Add(new Bound(item, false, false));
            return result;
        }
        private static  Range5<T> FromBoundList(IList<Bound> list)
        {
            Range5<T> result = new Range5<T>();            
            Bound prior = list[0];
            result.Bounds.Add(prior);
            for (int  i = 1; i<list.Count; i++)
            {
                Bound current = list[i];

                if (!prior.IncludesAfter || !current.IncludesBefore)
                {
                    if (current - prior == 1)
                    {
                        result.Bounds.Remove(prior);
                        if (!current.IncludesAfter) result.Bounds.Add(new Bound(current.Value, true, false));
                        continue;
                    }
                    else
                        result.Bounds.Add(current);
                }
            }
            return result;
        }

        #endregion



        #region Range contents queries

        public bool Contains(T item)
        {
            Bound itemBound = new Bound(item);
            if (Bounds.Contains(itemBound)) return true;
            Bound prior, next;
            bool bracketsLeft = Bounds.TryGetBefore(itemBound, out prior), bracketsRight = Bounds.TryGetAfter(itemBound, out next);
            if (!bracketsRight) return prior.IncludesAfter;
            if (!bracketsLeft) return next.IncludesBefore;
            return prior.IncludesAfter && next.IncludesBefore;
        }

        public bool IsEmpty { get { return Bounds.Count == 0; } }
        public bool IsInfinity { get { return Bounds.Count == 1 && Bounds.Min.IsInfinity; } }
        public bool IsSingleton { get { return Bounds.Count == 1 && Bounds.Min.IsSingleton; } }

        public override string ToString()
        {
            if (IsEmpty) return "{Empty}";
            if (IsInfinity) return "{Infinity}";
            if (IsSingleton) return "[" + Bounds.Min.Value.ToString() + "]";

            StringBuilder sb = new StringBuilder();
            if (Bounds.Min.IncludesBefore) sb.Append("{<");
            IEnumerator<Bound> iter = Bounds.GetEnumerator();
            iter.MoveNext();
            while (true)
            {
                Bound b = iter.Current;
                if (b.IsSingleton)
                    sb.Append("[" + b.Value.ToString() + "]");
                else if (b.IncludesAfter)
                    sb.Append((b.IncludesBefore ? "[" : "(") + b.Value.ToString() + ",");
                else if (b.IncludesBefore)
                {
                    sb.Append(b.Value.ToString() + (b.IncludesAfter ? "]" : ")"));
                    if (iter.MoveNext())
                    {
                        sb.Append("  ");
                        continue;
                    }                    
                }

                if (!iter.MoveNext()) break;


            }
            if (Bounds.Max.IncludesAfter) sb.Append(">");
            return sb.ToString();
            
        }
        #endregion



        #region Range set math members

        protected virtual bool GetEquals(Range5<T> other)
        {
            if (Bounds.Count != other.Bounds.Count) return false;
            IEnumerator<Bound> aIter = Bounds.GetEnumerator(), bIter = other.Bounds.GetEnumerator();

            while (true)
            {
                bool aAdvanced = aIter.MoveNext();
                bool bAdvanced = bIter.MoveNext();
                if (aAdvanced != bAdvanced) return false;
                if (!bAdvanced) return true;
                Bound aBound = aIter.Current, bBound = bIter.Current;
                if (aBound.Value.CompareTo(bBound.Value) != 0) return false;
                if (aBound.IncludesBefore != bBound.IncludesBefore) return false;
                if (aBound.IncludesAfter != bBound.IncludesAfter) return false;
            }
        }

        protected static T GetMin(T a, T b)
        {
            return a.CompareTo(b) <= 0 ? a : b;
        }
        protected static T GetMax(T a, T b)
        {
            return a.CompareTo(b) >= 0 ? a : b;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <remarks>This method is an O(n) method.</remarks>
        protected virtual Range5<T> GetIntersection(Range5<T> other)
        {
            if (IsEmpty || other.IsEmpty) return NewEmpty();
            if (other.IsInfinity && this.IsInfinity) return NewInfinity();
            if (other.IsInfinity) return this.Copy();
            if (IsInfinity) return other.Copy();

            IEnumerator<Bound> aIter = this.Bounds.GetEnumerator(), bIter = other.Bounds.GetEnumerator();
            List<Range5<T>> intersections = new List<Range5<T>>();
            Range5<T> constructing = new Range5<T>();

            aIter.MoveNext();
            bIter.MoveNext();
            Bound aBound = aIter.Current, bBound = bIter.Current;
            while (true)
            {                
                int c = aBound.CompareTo(bBound);
                if (c < 0)  //aBound < bBound.
                {
                    if (bBound.IncludesBefore) constructing.Bounds.Add(aBound);
                    if (!aIter.MoveNext())
                    {
                        if (aBound.IncludesAfter)
                            while (bIter.MoveNext()) constructing.Bounds.Add(bIter.Current);
                        break;
                    }
                    aBound = aIter.Current;
                    continue;
                }
                else if (c > 0) //aBound > bBound
                {
                    if (aBound.IncludesBefore) constructing.Bounds.Add(bBound);
                    if (!bIter.MoveNext())
                    {
                        if (bBound.IncludesAfter)
                            while (aIter.MoveNext()) constructing.Bounds.Add(aIter.Current);
                        break;
                    }

                    bBound = bIter.Current;
                    continue;
                }
                else  // aBound == bBound
                {
                    //Is it a singleton?                    
                    if (aBound.IsSingleton && bBound.IsSingleton)
                        constructing.Bounds.Add(aBound);
                    else
                    {
                        Bound newBound = new Bound(aBound.Value, aBound.IncludesBefore && bBound.IncludesBefore, 
                                                   aBound.IncludesAfter && bBound.IncludesAfter);
                        if (!newBound.IsSingleton) constructing.Bounds.Add(newBound);
                    }

                    if (aIter.MoveNext())
                        aBound = aIter.Current;
                    else
                    {
                        if (aBound.IncludesAfter)
                            while (bIter.MoveNext()) constructing.Bounds.Add(bIter.Current);
                        break;
                    }

                    if (bIter.MoveNext())
                        bBound = bIter.Current;
                    else
                    {
                        if (bBound.IncludesAfter)
                            while (aIter.MoveNext()) constructing.Bounds.Add(aIter.Current);
                        break;
                    }

                    continue;
                }
            }

            
            //Return the result.
            return constructing;
            

        }


        protected virtual Range5<T> GetUnion(T item)
        {

            Bound prior, next, itemBound = new Bound(item);
            bool foundPrior = Bounds.TryGetBefore(itemBound, out prior), foundNext = Bounds.TryGetAfter(itemBound, out next);

            if (foundPrior && foundNext)
            {
                //If it hits a bound, that's already included.
                if (item.CompareTo(prior.Value) == 0) return Copy();
                if (item.CompareTo(next.Value) == 0) return Copy();

                //If it hits in an included region, then obviously it's already included.
                if (prior.IncludesAfter && next.IncludesBefore) return Copy();

                //Since it falls in an excluded region, see if a boundary should be updated.
                int dPrior = Math.Abs(Bound.GetDistance(prior.Value, item)),
                    dNext = Math.Abs(Bound.GetDistance(item, next.Value));
                if (dPrior == 1 && dNext == 1)
                {
                    if (Bounds.Count == 2) return NewInfinity();
                    Range5<T> range = Copy();
                    range.Bounds.Remove(prior);
                    range.Bounds.Remove(next);
                    return range;
                }
                //Adjacnt to the prior boundary?
                else if (dPrior == 1)
                {
                    Range5<T> range = Copy();
                    range.Bounds.Remove(prior);
                    range.Bounds.Add(new Bound(item, prior.IncludesBefore, prior.IncludesAfter));
                    return range;
                }
                //Adjacent to the next boundary?
                else if (dNext == 1)
                {
                    Range5<T> range = Copy();
                    range.Bounds.Remove(next);
                    range.Bounds.Add(new Bound(item, next.IncludesBefore, next.IncludesAfter));
                    return range;
                }
                //A lonely singleton distance from the nearest boundaries?
                else
                {
                    Range5<T> range = Copy();
                    range.Bounds.Add(itemBound);
                    return range;
                }

            }
            else if (foundPrior)
            {
                if (item.CompareTo(prior.Value) == 0) return Copy();
                if (prior.IncludesAfter) return Copy();
                int dPrior = Math.Abs(Bound.GetDistance(prior.Value, item));
                if (dPrior == 1)
                {
                    Range5<T> range = Copy();
                    range.Bounds.Remove(prior);
                    range.Bounds.Add(new Bound(item, prior.IncludesBefore, prior.IncludesAfter));
                    return range;
                }
                else
                {
                    Range5<T> range = Copy();
                    range.Bounds.Add(itemBound);
                    return range;
                }
            }
            else if (foundNext)
            {
                if (item.CompareTo(next.Value) == 0) return Copy();
                if (next.IncludesBefore) return Copy();
                int dNext = Math.Abs(Bound.GetDistance(item, next.Value));
                if (dNext == 1)
                {
                    Range5<T> range = Copy();
                    range.Bounds.Remove(next);
                    range.Bounds.Add(new Bound(item, next.IncludesBefore, next.IncludesAfter));
                    return range;
                }
                else
                {
                    Range5<T> range = Copy();
                    range.Bounds.Add(itemBound);
                    return range;
                }
            }
            else
            {
                Range5<T> range = Copy();
                range.Bounds.Add(itemBound);
                return range;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <remarks>This method is an O(n log (n)) method.  TODO:  don't rely on a sorter, just crawling from one Bounds set to the 
        /// other.</remarks>
        protected virtual Range5<T> GetUnion(Range5<T> other)
        {
            if (IsEmpty && other.IsEmpty) return NewEmpty();
            if (IsEmpty) return other.Copy();
            if (other.IsEmpty) return Copy();
            if (other.IsInfinity || this.IsInfinity) return NewInfinity();

            
            List<Bound> simple = new List<Bound>();            
            int includes = 0;   //This variable will track the number of ranges started (0, 1, or 2 at any given point).

            IEnumerator<Bound> aIter = Bounds.GetEnumerator(), bIter = other.Bounds.GetEnumerator();
            aIter.MoveNext();
            bIter.MoveNext();            

            while (true)
            {
                Bound aBound = aIter.Current, bBound = bIter.Current;
                int c = aBound.CompareTo(bBound);
                if (c < 0)
                {
                    //Check if aBound should be added.
                    if (aBound.IncludesAfter)
                    {
                        if (includes++== 0) simple.Add(aBound);
                    }
                    else
                    {
                        if (--includes == 0) simple.Add(aBound);
                        else if (includes < 0)  //Can happen in the instance of a first-place infinite.
                        {
                            if (simple.Count == 0) simple.Add(aBound);
                            else simple[0] = new Bound(GetMax(aBound.Value, simple[0].Value), true, false);
                            includes = 0;
                        }
                    }
                    //Increment aIter.
                    if (!aIter.MoveNext())
                    {
                        if (!aBound.IncludesAfter)
                        {
                            simple.Add(bBound);
                            while (bIter.MoveNext()) simple.Add(bIter.Current);
                            break;
                        }
                    }                    
                }
                else if (c > 0)
                {
                    //Check if bBound should be added.
                    if (bBound.IncludesAfter)
                    {
                        if (includes++ ==0) simple.Add(bBound);
                    }
                    else
                    {
                        if (--includes == 0) simple.Add(bBound);
                        else if (includes < 0)  //Can happend  in the instance of a first-item low infinte.
                        {
                            if (simple.Count == 0) simple.Add(bBound);
                            else simple[0] = new Bound(GetMax(bBound.Value, simple[0].Value), true, false);
                            includes = 0;
                        }
                    }
                    //Increment bIter.
                    if (!bIter.MoveNext())
                    {
                        if (!bBound.IncludesAfter)
                        {
                            simple.Add(aBound);
                            while (aIter.MoveNext()) simple.Add(aIter.Current);
                            break;
                        }
                    }
                }
                else  //c == 0
                {                    
                    //Set  up a new bound?
                    includes += aBound.IncludesAfter ? 1 : -1;
                    includes += bBound.IncludesAfter ? 1 : -1;
                    if (includes == 0)          //This was a singleton in an empty field, or two ranges brought to an end.
                        simple.Add(aBound);  
                    else if (includes < 0)  //For the rare instance where we are just setting up after
                    {
                        if (simple.Count == 0) simple.Add(bBound);
                        else simple[0] = new Bound(GetMax(bBound.Value, simple[0].Value), true, false);
                        includes = 0;
                    }

                    //Increment - but which one?  'a' or 'b'?
                    if (bBound.IncludesAfter)
                    {
                        //Increment bIter.
                        if (!bIter.MoveNext())
                        {
                            if (!bBound.IncludesAfter)
                            {
                                simple.Add(aBound);
                                while (aIter.MoveNext()) simple.Add(aIter.Current);
                                break;
                            }
                        }
                    }
                    else 
                    {
                        //Increment aIter.
                        if (!aIter.MoveNext())
                        {
                            if (!aBound.IncludesAfter)
                            {
                                simple.Add(bBound);
                                while (bIter.MoveNext()) simple.Add(bIter.Current);
                                break;
                            }
                        }
                    }
                    
                }

            
            }

            return new Range5<T>(simple);
        }

        /// <summary>
        /// Attempts to append the given bound.  Returns true if the existing collection is modified in some way.  Also, checks for invalid 
        /// states.
        /// </summary>
        /// <param name="existing">The Skip List of existing bounds.</param>
        /// <param name="bound">The bounds attempting to append.</param>
        /// <returns></returns>
        private static bool TryAppend(SkipList<Bound> existing, Bound bound)
        {            
            if (existing.Count == 0)
            {
                existing.Add(bound);
                return true;
            }
            Bound prior = existing.Max;
            if (prior.IncludesAfter)
            {
                if (!bound.IncludesBefore) throw new InvalidOperationException("Attempted to add to a ] bound with a ( bound.");
                if (bound.IncludesAfter) return false;
                existing.Add(bound);
                return true;
            }
            else   ///prior.IncludesAfter = false
            {
                if (bound.IncludesBefore) throw new InvalidOperationException("Attempted to add to a ) bound with a [ bound.");
                if (bound-prior == 1)  //They are consecutive.
                {
                    //One way or the other, the prior must be removed.
                    existing.Remove(prior);

                    //Just extend the existing bound one spot?
                    if (!bound.IncludesAfter) existing.Add(new Bound(bound.Value, true, false));
                    
                }
            }
            throw new NotImplementedException();
        }


        //private static UnionSorter _UnionSorter = new UnionSorter();
        ///// <summary>
        ///// This comparer is design to ensure than when a==b, a Bound that has IncludesAfter==true will come first.  The point is to 
        ///// ensure that includes are counted up before they are counted down when a.Value==b.Value.
        ///// </summary>
        //protected class UnionSorter : IComparer<Bound>
        //{
        //    public virtual int Compare(Bound x, Bound y)
        //    {
        //        int c = x.Value.CompareTo(y.Value);
        //        if (c != 0) return c;
        //        if (x.IncludesAfter) return -1;
        //        if (y.IncludesAfter) return 1;
        //        return 0;
        //    }
        //}
       
   

        public virtual Range5<T> GetDifference(Range5<T> other)
        {
            throw new NotImplementedException();
        }

        public virtual Range5<T> GetInverse(Range5<T> other)
        {
            throw new NotImplementedException();
        }

        #endregion



        #region Range helpers


        protected virtual IEnumerable<Range5<T>> GetSeparated()
        {
            Range5<T> constructing = null;
            List<Range5<T>> result = new List<Range5<T>>();
            foreach (Bound b in Bounds)
            {
                if (constructing == null)
                {
                    constructing = new Range5<T>();
                    constructing.Bounds.Add(b);

                    if (!b.IncludesAfter)
                    {
                        result.Add(constructing);
                        constructing = null;
                    }
                    continue;
                }

                if (b.IncludesBefore)
                    constructing.Bounds.Add(b);

                if (!b.IncludesAfter)
                {
                    result.Add(constructing);
                    constructing = null;
                }
            }
            if (constructing != null) result.Add(constructing);
            return result;
        }

        protected virtual void SimplifyBounds()
        {

        }

        #endregion


        #region Range operator

        public override bool Equals(object obj)
        {
            if (obj is Range5<T>) return (Range5<T>)obj == this;
            return false;
        }
        public override int GetHashCode()
        {
            return Bounds.Min.GetHashCode() + Bounds.Max.GetHashCode();
        }
        public static bool operator ==(Range5<T> a, Range5<T> b)
        {
            return a.GetEquals(b);
        }
        public static bool operator !=(Range5<T> a, Range5<T> b)
        {
            return !(a == b);
        }

        public static Range5<T> operator |(Range5<T> a, Range5<T> b)
        {
            return a.GetUnion(b);
        }
        public static Range5<T> operator |(Range5<T> range, T item)
        {
            return range.GetUnion(item);
        }

        #endregion

    }
}
