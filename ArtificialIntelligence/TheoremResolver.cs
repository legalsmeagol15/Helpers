using DataStructures;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.TheoremProving
{
    public sealed class TheoremResolver
    {

        private HashList<Sentence> _KnowledgeBase = new HashList<Sentence>();
        private Dictionary<object, HashSet<Sentence>> _Topics = new Dictionary<object, HashSet<Sentence>>();

        public bool Tell(Sentence s)
        {
            return _KnowledgeBase.Add(s);            
        }
        
        public Sentence Ask(Sentence proposition)
        {
            //STEP #0 - set up the data structures.  I need an empty pair heap, 
            Sentence[] contra = !proposition;
            int heapCapacity = (_KnowledgeBase.Count + contra.Length);
            heapCapacity *= heapCapacity;
            heapCapacity /= 2;
            Heap<SentencePair> queue = new Heap<SentencePair>((a, b) => Compare(a, b, proposition), heapCapacity);
            HashList<Sentence> kb = new HashList<Sentence>(_KnowledgeBase); //Temporary knowledge base, for purposes of this ask.  Temporary because stuff will be added.
            foreach (Sentence c in contra) kb.Add(c);

            //STEP #1 - prep the starting work queue.
            //Enqueue the contra comparisons first, which are likely to float to the top of the queue anyway.            
            for (int i = 0; i < contra.Length; i++)
            {
                contra[i].ResetParent();
                contra[i].SetSimilarity(proposition);
                for (int j = 0; j < kb.Count; j++)
                {
                    SentencePair sp = new SentencePair(contra[i], kb[j]);
                    queue.Enqueue(sp);                    
                }
            }
            //Add inferences within the knowledge base alone.                
            for (int i = 0; i < kb.Count - 1; i++)
            {
                kb[i].ResetParent();
                kb[i].SetSimilarity(proposition);
                for (int j = i + 1; j < kb.Count; j++)
                {
                    SentencePair sp = new SentencePair(kb[i], kb[j]);
                    queue.Enqueue(sp);                    
                }
            }
            kb[kb.Count - 1].ResetParent();

            //STEP #2 - add the contra proposition to the temp knowledge base.
            //Combine the contra with the temporary knowledge base.
            for (int i = 0; i < contra.Length; i++) kb.Add(contra[i]);

            //STEP #3 - Now, look for resolvents.
            while (queue.Count > 0)
            {

                //Dequeue a pair and check if it resolves.
                SentencePair pair = queue.Dequeue();
                Sentence resolvent = pair.A | pair.B;
                int resolvingPairs = resolvent.Resolve();
                if (resolvingPairs != 1) continue;      //If it doesn't resolve at all, nothing more to learn so throw away the pair.  If it resolves twice or more, it's a 
                                                        //tautology so throw away the pair.

                //Did the proposition get solved?
                if (resolvent.Count == 0) return resolvent;

                //From here, it's a valid non-empty resolvent that is either A) already known or B) new.

                //If an identical sentence already exists on kb, change the resolvent ref to that sentence.
                int knownIdx = kb.IndexOf(resolvent);
                int newDepth = Math.Max(pair.A.Depth, pair.B.Depth) + 1;
                if (knownIdx >= 0)
                {
                    resolvent = kb[knownIdx];                    
                    if (newDepth < resolvent.Depth)
                    {
                        resolvent.Depth = newDepth;
                        resolvent.ParentA = pair.A;
                        resolvent.ParentB = pair.B;
                    }
                }
                else
                {
                    resolvent.Depth = newDepth;
                    resolvent.ParentA = pair.A;
                    resolvent.ParentB = pair.B;
                    for (int i = 0; i < kb.Count; i++) queue.Enqueue(new SentencePair(resolvent, kb[i]));
                    kb.Add(resolvent);
                }               

            }

            //STEP #4 - all possible sentence pairs have been examined, with no luck.  Could not be proven.
            return null;

        }
        private int Compare(SentencePair pairA, SentencePair pairB, Sentence proposition)
        {

            //First heuristic:  sentences that have more in common with a proposition should come first, unless the difference is inconsequential.            
            if (pairA.CombinedSimilarity > pairB.CombinedSimilarity + 0.2f) return -1;
            else if (pairA.CombinedSimilarity < pairB.CombinedSimilarity - 0.2f) return 1;

            //Second heuristic:  shorter sentences go first.
            return pairA.ClauseCount.CompareTo(pairB.ClauseCount);
            
        }

        private struct SentencePair
        {
            public readonly Sentence A;
            public readonly Sentence B;
            public readonly int ClauseCount;
            public readonly double CombinedSimilarity;
            
            public SentencePair (Sentence a, Sentence b)
            {
                A = a;
                B = b;
                CombinedSimilarity = a.Similarity * b.Similarity;
                ClauseCount = a.Count + b.Count;
            }
            
        }
        
        
    }

    /// <summary>
    /// A structure representing a negateable variable tag.
    /// </summary>
    public struct Clause
    {
        public readonly bool IsPositive;
        public readonly object Tag;
        public Clause(object tag, bool isPositive = true)
        {
            IsPositive = isPositive;
            Tag = tag;
        }
    }


    /// <summary>
    /// A Sentence is a data object that uses hashing to identify the presence or absence of positive or negative literals.  The Sentence is immutable once constructed, 
    /// except for through the Resolve() method.
    /// </summary>
    public class Sentence : IEnumerable<Clause>
    {
        internal void ResetParent()
        {
            ParentA = null;
            ParentB = null;
            Depth = int.MaxValue;
        }
        internal Sentence ParentA, ParentB;
        internal int Depth;

        //Invariant rules:  Sentence must be immuteable once it has been created.
        private const int DEFAULT_CAPACITY = 3;
        private object[] _Literals;
        private Flag[] _Flags;

        /// <summary>
        /// Used for heuristic sorting of sentences.  Similarity is a float representing the percentage of literals held in common with a proposition given by the 
        /// SetProposition(Sentence) method.
        /// </summary>
        internal float Similarity { get; private set; } = float.NaN;
        internal float SetSimilarity(Sentence proposition)
        {
            int shared = 0;
            for (int i = 0; i < _Literals.Length; i++) if (proposition.ContainsLiteral(_Literals[i])) shared++;
            Similarity = (float)shared / (float)_Literals.Length;
            return Similarity;            
        }

        public int Count { get; private set; }

        public Sentence() : this(DEFAULT_CAPACITY) { }

        public Sentence(IEnumerable<Clause> clauses) : this(clauses.Count())
        {
            foreach (Clause c in clauses) Add(c.Tag, c.IsPositive);
            _HashCode = CalculateHashCode();
        }
        public Sentence(IEnumerable<object> variables) : this(variables.Count())
        {
            foreach (object o in variables) Add(o, true);
            _HashCode = CalculateHashCode();
        }

        internal Sentence(int capacity)
        {
            int size = Mathematics.Primes.GetNextPrime(capacity - 1);
            _Literals = new object[size];
            _Flags = new Flag[size];
            Count = 0;
            _HashCode = 0;
            ResetParent();
        }
        internal Sentence Copy(int newCount = -1)
        {
            Sentence copy = new Sentence(_Literals.Length);
            for (int i = 0; i < _Literals.Length; i++)
            {
                copy._Literals[i] = _Literals[i];
                copy._Flags[i] = _Flags[i];
            }
            copy.Count = Count;
            copy._HashCode = _HashCode;
            return copy;
        }

        private int _HashCode;
        private int CalculateHashCode()
        {
            int result = 0;
            for (int i = 0; i < _Literals.Length; i++)
            {
                Flag f = _Flags[i];
                if (f == Flag.Deleted || f == Flag.Empty) continue;
                unchecked
                {
                    result += _Literals[i].GetHashCode();
                }
            }
            return Math.Abs(result);
        }

        public static Sentence Empty() { return new Sentence(); }
        public static Sentence FromClause(Clause c)
        {
            Sentence result = new Sentence();
            result.Add(c.Tag, c.IsPositive);
            return result;
        }



        #region Sentence contents manipulation

        /// <summary>
        /// Adds the given Clause to this Sentence.
        /// </summary>
        /// <returns>Returns true if the item did not already exist on the Sentence but was added; otherwise, returns false.</returns>
        private bool Add(Clause c)
        {
            return Add(c.Tag, c.IsPositive ? Flag.HasPositive : Flag.HasNegative);
        }
        /// <summary>
        /// Adds the given object as a variable tag to this Sentence.
        /// </summary>
        /// <param name="newObj">The variable tag.</param>
        /// <param name="isPositive">Optional.  Whether the literal to be added will represent the variable tag, or the negation of the variable tag.</param>
        /// <returns>Returns true if the item did not already exist on the Sentence but was added; otherwise, returns false.</returns>
        private bool Add(object newObj, bool isPositive = true)
        {
            return Add(newObj, isPositive ? Flag.HasPositive : Flag.HasNegative);
        }
        private bool Add(object newObj, Flag newFlag)
        {
            if (Count >= (_Literals.Length / 2))
                SetCapacity(Mathematics.Primes.GetNextPrime(_Literals.Length * 2));

            int i = GetInsertIndex(newObj);
            object oldObj = _Literals[i];
            Flag oldFlag = _Flags[i];
            if (oldObj.Equals(newObj) && newFlag == oldFlag) return false;

            _Literals[i] = newObj;
            _Flags[i] = newFlag;

            Count++;
            return true;
        }



        ///// <summary>
        ///// Changes this sentence into an empty sentence.  For purposes of a logical resolution theorem prover, this will signify a contradiction.
        ///// </summary>
        //public void Clear()
        //{         
        //    _Literals = new object[DEFAULT_CAPACITY];
        //    _Flags = new Flag[DEFAULT_CAPACITY];
        //    Count = 0;
        //    _HashCode = 0;
        //}

        /// <summary>
        /// Uses quadratic probing to finds the first index where the table is empty or the item is deleted, or the object already exists.
        /// </summary>
        private int GetInsertIndex(object o)
        {
            int hash = o.GetHashCode(), probe = 0, index;
            while (true)
            {
                index = (hash + (probe * probe++)) % _Literals.Length;
                if (_Literals[index].Equals(o)) return index;
                Flag f = _Flags[index];
                if (f == Flag.Deleted || f == Flag.Empty) return index;
                if (probe > (1 << 15)) throw new InvalidOperationException("Probing error - this should be impossible.");
            }
        }

        ///// <summary>
        ///// Removes the given Clause from this Sentence object.
        ///// </summary>
        ///// <returns>Returns true if the item was removed; false if the item never existed on this Sentence to begin with.</returns>
        //private bool Remove(Clause c)
        //{
        //    return Remove(c.Tag, c.IsPositive);
        //}
        ///// <summary>
        ///// Removes the given variable object and positive/negative aspect from this Sentence object.
        ///// </summary>
        ///// <param name="obj">The variable tag to remove from this Sentence.</param>
        ///// <param name="isPositive">The positive/negative aspect to remove.  For example, if both the positive and negative aspect of the char 'd' existed on this Sentence, 
        ///// removing the 'isPositive = true' aspect would leave the negative aspect in place.</param>
        ///// <returns>Returns true if the item was removed; false if the item never existed on this Sentence to begin with.</returns>
        //private bool Remove(object obj, bool isPositive = true)
        //{
        //    int i = GetIndex(obj);
        //    if (i < 0) return false;
        //    if (isPositive)
        //    {
        //        if ((_Flags[i] & Flag.HasPositive) == 0) return false;
        //        else if ((_Flags[i] & Flag.HasNegative) > 0) _Flags[i] = Flag.HasNegative;
        //        else _Flags[i] = Flag.Deleted;
        //    }
        //    else
        //    {
        //        if ((_Flags[i] & Flag.HasNegative) == 0) return false;
        //        else if ((_Flags[i] & Flag.HasPositive) > 0) _Flags[i] = Flag.HasPositive;
        //        else _Flags[i] = Flag.Deleted;
        //    }
        //    Count--;
        //    return true;
        //}

        /// <summary>
        /// Resolves this theorem by removing the first positive/negative literal pair.
        /// </summary>
        /// <returns>Returns either 0, 1 , or 2.  If zero, there was no resolvent produced.  If 1, a single pair was resolved.  If 2, then 2 or more pairs could be 
        /// resolved, so this Sentence is a tautology.</returns>
        public int Resolve()
        {
            int resolves = 0;
            int i = 0;
            while (i < _Literals.Length)
            {
                if (_Flags[i] == Flag.HasBoth)
                {
                    _Flags[i] = Flag.Deleted;
                    if (++resolves > 1) return resolves;                    
                }
                i++;
            }
            return resolves;
        }


        /// <summary>
        /// Creates new set storage tables with the given capacity, and copies the non-empty and non-deleted entries from the old to the new.
        /// </summary>        
        private void SetCapacity(int newCapacity)
        {
            object[] oldLiterals = _Literals;
            Flag[] oldFlags = _Flags;
            _Literals = new object[newCapacity];
            _Flags = new Flag[newCapacity];
            for (int i = 0; i < oldLiterals.Length; i++)
            {
                Flag oldF = oldFlags[i];
                if (oldF == Flag.Empty || oldF == Flag.Deleted) continue;
                object oldL = oldLiterals[i];
                int idx = GetInsertIndex(oldL);
                _Literals[idx] = oldL;
                _Flags[idx] = oldF;
            }
        }

        #endregion



        #region Sentence contents queries

        /// <summary>
        /// Returns whether the described literal is contained in this Sentence.
        /// </summary>
        /// <param name="obj">The variable tag.</param>
        /// <param name="isPositive">Optional.  Whether to search for the variable tag, or the negation of the variable tag.  If omitted, a positive literal is 
        /// assumed.</param>        
        public bool Contains(object obj, bool isPositive = true)
        {
            return Contains(obj, isPositive ? Flag.HasPositive : Flag.HasNegative);
        }
        internal bool Contains(object obj, Flag f)
        {
            int i = GetIndex(obj);
            if (i < 0) return false;
            return f == (_Flags[i] & f);
        }
        internal bool ContainsLiteral(object obj) { return GetIndex(obj) >= 0; }

        /// <summary>
        /// Returns whether all literals in this Sentence are also contained in the given Sentence.  Literals are deemed to be equal if their variable tags are equal according 
        /// to the Equals() method of the tag, and they are equivalent in terms of their positive/negative sign.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (!(obj is Sentence)) return false;
            Sentence os = (Sentence)obj;
            for (int i = 0; i < _Literals.Length; i++)
            {
                Flag f = _Flags[i];
                if (f == Flag.Deleted || f == Flag.Empty) continue;
                object l = _Literals[i];
                if (!os.Contains(l, f)) return false;
            }
            return true;
        }

        /// <summary>
        /// Returns the index of the given object.  If the object is not contained in this Sentence, returns -1.
        /// </summary>
        private int GetIndex(object o)
        {
            int hash = o.GetHashCode(), probe = 0, index;
            while (true)
            {
                index = (hash + (probe * probe++)) & _Literals.Length;
                Flag f = _Flags[index];
                if (_Literals[index].Equals(o))
                    return (f == Flag.HasPositive || f == Flag.HasNegative) ? index : -1;
                if (f == Flag.Empty) return -1;
            }
        }

        /// <summary>
        /// Returns an enumerator that steps through the clauses contained in this Sentence.  If a variable tag is present in both the positive and the negative aspects, returns 
        /// the positive first and then the negative.
        /// </summary>        
        public IEnumerator<Clause> GetEnumerator()
        {
            for (int i = 0; i < _Literals.Length; i++)
            {
                Flag f = _Flags[i];
                if (f == Flag.Deleted || f == Flag.Empty) continue;
                object l = _Literals[i];
                if ((f & Flag.HasPositive) > 0) yield return new Clause(l, true);
                if ((f & Flag.HasNegative) > 0) yield return new Clause(l, false);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns the hash code for this Sentence.  Because a Sentence is immutable, the hash code is cached internally upon creation.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return _HashCode;
        }

        //public IEnumerable<object> GetLiterals()
        //{
        //    for (int i = 0; i < Literals.Length; i++)
        //    {
        //        Flag f = Flags[i];
        //        if (f == Flag.Deleted || f == Flag.Empty) continue;
        //        yield return Literals[i];
        //    }
        //}

        [Flags]
        internal enum Flag
        {
            /// <summary>
            /// No object has ever been stored at this index.
            /// </summary>
            Empty = 0,

            /// <summary>
            /// An object once stored here has since been deleted in regar to both the positive and negative aspect.
            /// </summary>
            Deleted = 1,

            /// <summary>
            /// The variable tag is contained in this Sentence.
            /// </summary>
            HasPositive = 2,

            /// <summary>
            /// The negation of the variable tag is contained in this Sentence.
            /// </summary>
            HasNegative = 4,

            /// <summary>
            /// Both the variable tag and its negation are contained in this Sentence.
            /// </summary>
            HasBoth = 6
        }

        #endregion



        #region Sentence operators

        public static Sentence operator +(Sentence s, object obj)
        {
            Sentence copy = s.Copy();
            s.Add(obj, Flag.HasPositive);
            unchecked
            {
                copy._HashCode += obj.GetHashCode();
            }
            copy._HashCode = Math.Abs(copy._HashCode);
            return s;
        }
        public static Sentence operator +(Sentence s, Clause c)
        {
            Sentence copy = s.Copy();
            s.Add(c.Tag, Flag.HasPositive);
            unchecked
            {
                copy._HashCode += c.Tag.GetHashCode();
            }
            copy._HashCode = Math.Abs(copy._HashCode);
            return s;
        }
        public static Sentence operator +(Sentence a, Sentence b)
        {
            Sentence copy = a.Copy();
            if (copy.Count + b.Count >= copy._Literals.Length / 2)
                copy.SetCapacity(Mathematics.Primes.GetNextPrime(copy._Literals.Length * 2));

            int hashCode = copy._HashCode;
            for (int i = 0; i < b._Literals.Length; i++)
            {
                Flag f = b._Flags[i];
                if (f == Flag.Deleted || f == Flag.Empty) continue;
                object l = b._Literals[i];
                copy.Add(l, f);
                unchecked
                {
                    hashCode += l.GetHashCode();
                }
            }
            copy._HashCode = Math.Abs(hashCode);
            return copy;
        }

        public static Sentence operator |(Sentence a, Sentence b)
        {
            return a + b;
        }

        public static implicit operator Sentence(Clause c)
        {
            Sentence s = new Sentence();
            s.Add(c);
            s._HashCode = s.CalculateHashCode();
            return s;
        }

        public static Sentence[] operator !(Sentence original)
        {
            //Applies deMorgan's Law:  ~(a | b) = (~a & ~b);

            Sentence[] result = new Sentence[original.Count];
            int idx = 0;
            foreach (Clause c in original)
                result[idx++] = Sentence.FromClause(new Clause(c.Tag, !c.IsPositive));
            return result;
        }



        #endregion

    }




}
