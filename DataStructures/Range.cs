using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{
    public class Range<T> where T :IComparable<T>
    {
        protected SkipList<Bound> Bounds { get; private set; }
        
        protected struct Bound 
        {

          
            public readonly T Value;
            public readonly bool IncludesSelf;
            public readonly bool IncludesPrev;
            public readonly bool IncludesNext;

            public Bound (T value, bool includesPrior, bool includesSelf, bool includesAfter)
            {
                if (!(includesSelf || includesAfter || includesPrior))
                    throw new ArgumentException("A Bound must have some inclusion.  Null bounds are not allowed.");
                if (value == null)
                    throw new ArgumentException("Null Bound values are not allowed.");
                Value = value;
                IncludesSelf = includesSelf;
                IncludesPrev = includesPrior;
                IncludesNext = includesAfter;
            }

            public static Bound NewNull()
            {
                return new Bound(default(T), false, false, false);
            }
            /// <summary>
            /// Returns true if all possible elements in relation to this Bound are included, or in other words, this Bound does not 
            /// function as a boundary at all.
            /// </summary>
            public bool IsInfinity { get { return IncludesPrev && IncludesNext && IncludesSelf; } }
            /// <summary>
            /// Returns true if no possible elements in relation to this Bound are included, or in other words, this Bound is simply 
            /// a nullity.
            /// </summary>
            public bool IsNull { get { return !(IncludesNext || IncludesPrev || IncludesSelf); } }
            /// <summary>
            /// Returns true if only a single item, equal to the Bound's value, is included in relation to this Bound.
            /// </summary>
            public bool IsSingleton { get { return !(IncludesPrev || IncludesNext) && IncludesSelf; } }
            /// <summary>
            /// Returns true if the two bounds are exactly congruent.
            /// </summary>
            public override bool Equals(object obj)
            {
                if (!(obj is Bound)) return false;
                Bound other = (Bound)obj;
                return Value.CompareTo(other.Value) == 0 && IncludesSelf == other.IncludesSelf && IncludesPrev == other.IncludesPrev 
                       && IncludesNext == other.IncludesNext;
            }
            /// <summary>
            /// A Bound's hash code will be equal to its Value, unless the Value is null.  In that case, the hash code is a bitwise value 
            /// based on whether it includes prior, self, and following.
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                if (Value != null) return Value.GetHashCode();
                int result = 0;
                if (IncludesPrev) result |= 4;
                if (IncludesSelf) result |= 2;
                if (IncludesNext) result |= 1;
                return result;
            }
           
           
            public static Bound NewSingleton(T item)
            {
                return new Bound(item, false, true, false);
            }
        }


        #region Range constructors

        public Range<T> Copy()
        {
            Range<T> result = new Range<T>();
            foreach (Bound b in Bounds) result.Bounds.Add(b);
            return result;
        }
        protected static Range<T> NewInfinity()
        {
            Range<T> result = new Range<T>();
            result.Bounds.Add(new Bound(default(T), true, true, true));
            return result;
        }

        #endregion



        #region Range contents queries

        public bool Contains(T item)
        {
            if (Bounds.Count < 1) return false;

            Bound prev, next, itemBound = Bound.NewSingleton(item);
            bool hasPrev = Bounds.TryGetBefore(itemBound, out prev), hasNext = Bounds.TryGetAfter(itemBound, out next);            
            if (hasPrev && hasNext) return prev.IncludesNext && next.IncludesPrev;
            if (hasPrev) return prev.IncludesNext;
            if (hasNext) return next.IncludesPrev;

            Bound onlyBound = Bounds.First();
            return onlyBound.IncludesSelf && onlyBound.Value.CompareTo(item) == 0;
        }

        public bool Contains(Range<T> other)
        {
            throw new NotImplementedException();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool IsEmpty { get { return Bounds.Count == 0; } }
        public bool IsInfinity { get { return Bounds.Count == 1 && Bounds.First().IsInfinity; } }
        public bool IsSingleton { get { return Bounds.Count == 1 && Bounds.First().IsSingleton; } }

        public override string ToString()
        {
            if (IsEmpty) return "{Empty}";
            if (IsInfinity) return "{Infinity}";

            StringBuilder sb = new StringBuilder();
            IEnumerator<Bound> iter = Bounds.GetEnumerator();
            iter.MoveNext();
            while (true)
            {
                Bound b = iter.Current;
                if (b.IncludesPrev)
                {
                    sb.Append((b.IncludesSelf ? "[" : "(") + b.Value.ToString() + "..");
                    continue;
                }
                else if (b.IncludesNext)                
                    sb.Append((b.IncludesSelf ? "]" : ")") + b.Value.ToString());                
                else sb.Append(b.Value.ToString());

                if (iter.MoveNext()) sb.Append("  ");
                else break;                
            }

            return sb.ToString();
        }

        #endregion


        

        #region Range helper methods

        //protected virtual bool AreConsecutive (Bound prior, Bound next)
        //{
        //    if (prior.Value.CompareTo(next.Value) == 0 && !(prior.IncludesNext || next.IncludesPrev))
        //        return prior.IncludesSelf ^ next.IncludesSelf;

        //    return false;
        //}

        //protected virtual double GetDifference(T a, T b)
        //{
        //    if (typeof(T) == typeof(int)) return int.Parse(a.ToString()) - int.Parse(b.ToString());
        //    int c = a.CompareTo(b);
        //    if (c > 0) return int.MinValue;
        //    if (c < 0) return int.MaxValue;
        //    return 0;
        //}

        /// <summary>
        /// Whenever a bound is created, this method is called to see if the bound can be simplified.  For some types of T, a simplified 
        /// bound may be returned.  For example, an int-type Bound that does not include itself or items past, but does include items 
        /// before, may have its value reduced be one so it will be a Bound that includes itself.
        /// <para/>In the base declaration, this method simply returns the original Bound given to it.
        /// </summary>
        protected virtual Bound GetSimplifiedBound(Bound original)
        {
            return original;
        }

     
        /// <summary>
        /// Tries to append the given bound to the given skip list in the Max position without checking that it is, indeed, a new Max.
        /// </summary>
        protected bool TryAppendBound(SkipList<Bound> existing, Bound newBound)
        {
            
            while (existing.Count > 0)
            {
                if (existing.Max.Value.CompareTo(newBound.Value) > 0) return false;
                Bound tryBound;
                bool overlap, combinable = TryCombineBounds(existing.Max, GetSimplifiedBound(newBound), out overlap, out tryBound);
                if (overlap) return false;
                if (!combinable) break;
                existing.Remove(existing.Max);
                newBound = tryBound;
            }            
            
            Bounds.Add(GetSimplifiedBound(newBound));
            return true;        
        }


        /// <summary>
        /// Attempts to combine two bounds into a single bound, if possible.  The base declaration handles situations of inclusion 
        /// consecutiveness.  For example, suppose that bound 'a' includes previous items but not itself, and bound 'b' has an 
        /// identical value but includes itself and not items following.  The two can be combined into a single bound in the 
        /// out variable, so the result bound will include all items before the shared value, plus the shared value itself.
        /// <para/>This method should be overridden for additional combinations in the case of discrete types of T.  For example, 
        /// two 'int' bounds may be combined even if they do not have the same value, such as the case of one bound immediately 
        /// succeeding another.
        /// <para/>When overridden, the following should cause a 'false' result:  1) 'a' and 'b' are in non-ascending order; 
        /// 2) something...
        /// </summary>
        /// <param name="a">The sequentially first bound to attempt to combine with bound 'b'.</param>
        /// <param name="b">The sequentially second bound to attempt to combine with the bound 'a'.</param>
        /// <param name="overlapped">A value indicating whether the two had any overlap.</param>
        /// <param name="result">The single bound that would result from combining 'a' and 'b'.  If the two input bounds cannot 
        /// be combined, the result variable should be set to a null Bound.</param>
        /// <returns>This method should return true if Bound 'b' can be appended legally after Bound 'a'; otherwise, it should 
        /// return false.</returns>
        protected virtual bool TryCombineBounds(Bound a, Bound b, out bool overlapped, out Bound result)
        {            
            int c = a.Value.CompareTo(b.Value);
            if (c == 0)
            {
                if  (a.IncludesNext || b.IncludesPrev)
                {
                    overlapped = true;
                    result = new Bound(a.Value, a.IncludesPrev || b.IncludesPrev, a.IncludesSelf || b.IncludesSelf, 
                        a.IncludesNext || b.IncludesNext);
                    return true;
                }
                if (a.IncludesSelf ^ b.IncludesSelf)
                {
                    overlapped = false;
                    result = new Bound(a.Value, a.IncludesPrev || b.IncludesPrev, true, a.IncludesNext || b.IncludesNext);
                    return true;
                }                
            }

            overlapped = c > 0 ? (a.IncludesNext || b.IncludesPrev) : (a.IncludesPrev || b.IncludesNext);
            result = Bound.NewNull();
            return false;
        }


        #endregion


        #region Range set contents manipulation

        public Range<T> GetUnion(Range<T> other)
        {
            IEnumerator<Bound> aIter = Bounds.GetEnumerator(), bIter = other.Bounds.GetEnumerator();
            SkipList<Bound> combined = new SkipList<Bound>();
            if (!aIter.MoveNext()) return other.Copy();
            if (!bIter.MoveNext()) return this.Copy();
            while (true)
            {
                Bound aBound = aIter.Current, bBound = bIter.Current;
                int c = aBound.Value.CompareTo(bBound.Value);
                if (c < 0)
                {
                    if (!TryAppendBound(combined, aBound)) throw new InvalidOperationException("Who knows.");
                }
            }

        }

        #endregion
    }
}
