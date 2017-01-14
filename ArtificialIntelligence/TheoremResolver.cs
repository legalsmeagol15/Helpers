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
        Queue<Tuple<Sentence, Sentence>> _Work = new Queue<Tuple<Sentence, Sentence>>();
        private Dictionary<object, HashSet<Sentence>> _Topics = new Dictionary<object, HashSet<Sentence>>();

        public TheoremResolver()
        {

        }

        public bool Tell(Sentence s)
        {
            if (_KnowledgeBase.Contains(s)) return false;

            HashSet<Sentence> onQueue = new HashSet<Sentence>();
            Queue<Sentence> queue = new Queue<Sentence>();

            //First, figure out what is topical that is already on the knowledge base.            
            foreach (Clause c in s)
            {
                //Is this clause a new topic?
                HashSet<Sentence> topic;
                if (!_Topics.TryGetValue(c, out topic))
                {
                    _Topics.Add(c, new HashSet<Sentence>());
                    continue;
                }

                //If the queue doesn't already have relevant sentences in this topic, add them on.
                foreach (Sentence sentence in topic)
                    if (onQueue.Add(sentence))
                        queue.Enqueue(sentence);
            }

            //Now, work through all the relevant sentences in the queue.            
            while (true)
            {

                //Find all non-tautological resolvents.
                List<Sentence> toAdd = new List<Sentence>();
                while (queue.Count > 0)
                {
                    Sentence resolution = queue.Dequeue().Resolve(s);

                    if (resolution != null)
                    {
                        if (resolution.Count == 0) throw new InvalidOperationException("Cannot add resolvent because it contradicts the known knowledge base.");
                        else if (!resolution.IsTautological) toAdd.Add(resolution);
                    }
                }

                //Add the new resolvents to the knowledge base and to the known topics.
                if (toAdd.Count == 0) break;
                foreach (Sentence addSentence in toAdd)
                {
                    foreach (Clause c in addSentence)
                    {
                        if (_KnowledgeBase.Add(addSentence))
                        {
                            _Topics[c].Add(addSentence);
                            if (onQueue.Add(addSentence)) queue.Enqueue(addSentence);
                        }
                    }
                }
            }

            //Finally, add the new sentence.
            foreach (Clause c in s)
                _Topics[c].Add(s);

            _KnowledgeBase.Add(s);
            return true;
        }
        
        public Proof Prove(Sentence s)
        {
            throw new NotImplementedException();
        }

        private IEnumerable<Sentence> GetTopical(Sentence s)
        {
            HashSet<Sentence> topical = new HashSet<Sentence>();
            foreach (Clause c in s)            
                foreach (Sentence relevant in _Topics[c])                
                    topical.Add(relevant);
            return topical;
        }

        public enum Proof
        {
            Disproven,
            Proven,
            Unproven
        }
    }



    public struct Clause
    {
        public readonly bool @Bool;
        public object Tag;

        public Clause(object tag) : this(tag, true) { }
        private Clause(object tag, bool @bool)
        {
            if (tag == null) throw new ArgumentException("Clause tag cannot be null.");
            Bool = @bool;
            Tag = tag;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Clause)) return false;
            Clause other = (Clause)obj;
            return Tag.Equals(other.Tag) && Bool.Equals(other.Bool);
        }
        public override int GetHashCode()
        {
            return Tag.GetHashCode();
        }

        public static Clause operator ~(Clause a)
        {
            return new Clause(a.Tag, !a.Bool);
        }
    }

    /// <summary>
    /// A collection of disjoint clauses form a sentence in conjunctive normal form.
    /// </summary>
    public sealed class Sentence : IEnumerable<Clause>
    {
        private const int DEFAULT_SIZE = 3;
        private Clause[] _Clauses;
        private bool[] _Present;
        private int _HashCode;

        /// <summary>
        /// Whether or not this clause is tautological, meaning it contains both a clause and its inverse.
        /// </summary>
        public bool IsTautological { get; private set; }

        /// <summary>
        /// The number of clauses contained in this sentence.
        /// </summary>
        public int Count { get; private set; }

        private Sentence(int capacity)
        {
            Reset(capacity);
        }
        public Sentence() : this(DEFAULT_SIZE) { }
        public Sentence Copy()
        {
            Sentence newSentence = new Sentence(_Clauses.Length);
            for (int i = 0; i < _Clauses.Length; i++)
            {
                newSentence._Clauses[i] = _Clauses[i];
                newSentence._Present[i] = _Present[i];
                newSentence.IsTautological = IsTautological;
                newSentence.Count = Count;
            }
            return newSentence;
        }

        public Sentence Resolve(Sentence other)
        {
            foreach (Clause c in this)
            {
                int otherIdx = other.GetIndex(c, false);
                if (otherIdx < 0) continue;

                int thisIdx = GetIndex(c);
                Sentence resolution = new Sentence(Mathematics.Primes.GetNextPrime(_Clauses.Length + other._Clauses.Length - 2));
                for (int i = 0; i < thisIdx; i++)
                    if (_Present[i]) resolution.Add(_Clauses[i]);
                for (int i = thisIdx + 1; i < _Clauses.Length; i++)
                    if (_Present[i]) resolution.Add(_Clauses[i]);
                for (int i = 0; i < otherIdx; i++)
                    if (other._Present[i]) resolution.Add(other._Clauses[i]);
                for (int i = otherIdx + 1; i < other._Clauses.Length; i++)
                    if (other._Present[i]) resolution.Add(other._Clauses[i]);
                return resolution;
            }

            return null;

        }

        #region Sentence contents manipulation

        /// <summary>
        /// Adds the given clause to this sentence.
        /// </summary>
        /// <returns>Returns true if the clause did not exist on the sentence before, was added.  Otherwise, returns false.</returns>
        public bool Add(Clause c)
        {
            if (Count >= _Clauses.Length / 2) IncreaseCapacity();
            int hashCode = c.Tag.GetHashCode();
            hashCode %= _Clauses.Length;

            int start = c.Tag.GetHashCode() % _Clauses.Length, offset = 0, i = start;
            int index = -1;
            while (true)
            {
                Clause existing = _Clauses[i];
                if (existing.Tag == null)
                {
                    if (index < 0) index = i;
                    break;
                }
                else if (!_Present[i]) index = i;
                else if (existing.Tag.Equals(c.Tag))
                {
                    if (existing.Bool != c.Bool) IsTautological = true;
                    else return false;
                }
                i = (start + (++offset * offset)) % _Clauses.Length;  //Quadratic probing
            }

            Count++;
            _Clauses[index] = c;
            _Present[index] = true;
            unchecked
            {
                _HashCode += c.Tag.GetHashCode();
            }
            return true;
        }

        /// <summary>
        /// Empties this sentence of all clauses.
        /// </summary>
        public void Clear()
        {
            Reset(DEFAULT_SIZE);
        }

        private void Reset(int capacity)
        {
            _Clauses = new Clause[capacity];
            _Present = new bool[capacity];
            Count = 0;
            IsTautological = false;
            _HashCode = 0;
        }

        private void IncreaseCapacity()
        {
            int newSize = Mathematics.Primes.GetNextPrime(_Clauses.Length);
            Clause[] newClauses = new Clause[newSize];
            bool[] newPresent = new bool[newSize];
            for (int i = 0; i < _Clauses.Length; i++)
            {
                if (!_Present[i]) continue;
                Clause c = _Clauses[i];
                int start = c.Tag.GetHashCode() % newClauses.Length, offset = 0, index = start;
                while (newPresent[index])
                    index = (start + (++offset * offset)) % newClauses.Length;  //Quadratic probing

                newClauses[index] = c;
                newPresent[index] = true;
            }
            _Clauses = newClauses;
            _Present = newPresent;

        }

        /// <summary>
        /// Removes the given clause from this sentence.
        /// </summary>        
        /// <returns>Returns true if the clause existed in this sentence and was successfully removed; otherwise, returns false.</returns>
        public bool Remove(Clause c)
        {
            int start = c.Tag.GetHashCode() % _Clauses.Length, offset = 0, i = start;
            while (true)
            {
                Clause existing = _Clauses[i];
                if (existing.Tag == null) return false;
                if (existing.Equals(c))
                {
                    IsTautological = false;
                    _Present[i] = false;
                    Count--;
                    unchecked
                    {
                        _HashCode -= c.Tag.GetHashCode();
                    }
                    return true;
                }
                i = (start + (++offset * offset)) % _Clauses.Length;  //Quadratic probing
            }
        }

        #endregion


        #region Sentence contents queries

        /// <summary>
        /// Returns whether or not this sentence contains the given clause.
        /// </summary>
        public bool Contains(Clause c)
        {
            return GetIndex(c) >= 0;
        }
        /// <summary>
        /// Returns whether or not this sentence contains the inverse of the given clause.
        /// </summary>
        public bool ContainsInverse(Clause c)
        {
            return GetIndex(c, false) >= 0;
        }


        /// <summary>
        /// Returns the index of the given clause, for either the clause itself or its complement.
        /// </summary>
        /// <param name="c">The clause to search for.</param>
        /// <param name="same">Whether we're searching for the given clause, or its complement.  If true, seeking the clause.  If false, seeking its complement.</param>
        private int GetIndex(Clause c, bool same = true)
        {
            int start = c.Tag.GetHashCode() % _Clauses.Length, offset = 0, index = start;
            while (true)
            {
                Clause existing = _Clauses[index];
                if (existing.Tag == null) return -1;        //Used slots exhausted.  The given clause isn't here.
                if (existing.Tag.Equals(c.Tag) && (same ^ (existing.Bool ^ c.Bool)))
                    return _Present[index] ? index : -1;    //The given clause is still here (return the index), or it was here and was deleted.
                index = (index + (++offset * offset)) % _Clauses.Length;
            }
        }

        /// <summary>
        /// Finds the indices of the clause and its complement.  If either is missing, returns -1.
        /// </summary>
        private void GetIndices(Clause c, out int index, out int inverse)
        {
            index = -1;
            inverse = -1;
            int start = c.Tag.GetHashCode() & _Clauses.Length, offset = 0, i = start;
            while (true)
            {
                Clause existing = _Clauses[i];
                if (existing.Tag == null) return;
                if (existing.Tag.Equals(c.Tag))
                {
                    if (existing.Bool == c.Bool) index = i;
                    else inverse = i;
                    if (index >= 0 && inverse >= 0) return;
                }
                i = (i + (++offset * offset)) % _Clauses.Length;
            }
        }



        IEnumerator<Clause> IEnumerable<Clause>.GetEnumerator()
        {
            for (int i = 0; i < _Clauses.Length; i++) if (_Present[i]) yield return _Clauses[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            for (int i = 0; i < _Clauses.Length; i++) if (_Present[i]) yield return _Clauses[i];
        }

        #endregion


        #region Sentence operators

        public static implicit operator Sentence(Clause c)
        {
            Sentence s = new Sentence();
            s.Add(c);
            return s;
        }

        public static explicit operator Clause(Sentence s)
        {
            if (s.Count != 0) throw new InvalidOperationException("Cannot convert Sentence directly to a Clause unless the Sentence contains only a single Clause.");
            return (s.First());
        }

        public static Sentence operator +(Sentence s, Clause c)
        {
            Sentence result = s.Copy();
            result.Add(c);
            return result;
        }
        public static Sentence operator -(Sentence s, Clause c)
        {
            Sentence result = s.Copy();
            result.Remove(c);
            return result;
        }
        public static Sentence operator +(Sentence a, Sentence b)
        {
            Sentence result = a.Copy();
            foreach (Clause toCopy in b) result.Add(toCopy);
            return result;
        }
        public static Sentence operator -(Sentence a, Sentence b)
        {
            Sentence result = a.Copy();
            foreach (Clause toRemove in b) result.Remove(toRemove);
            return result;
        }
        public static Sentence operator ^(Sentence a, Sentence b)
        {
            Sentence result = new Sentence();
            foreach (Clause clauseA in a) result.Add(clauseA);
            foreach (Clause clauseB in b) if (!result.Add(clauseB)) result.Remove(clauseB);
            return result;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Sentence)) return false;
            Sentence other = (Sentence)obj;
            if (Count != other.Count) return false;
            foreach (Clause c in this) if (!other.Contains(c)) return false;
            return true;
        }

        public override int GetHashCode()
        {
            return Math.Abs(_HashCode);
        }

        #endregion

    }




}
