using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing.Values
{
    public class Vector : IEvaluateable, IIndexable
    {

        private readonly int _Rank;


        /// <summary>Returns the number of dimensions in this Vector (any number greater than 1 would therefore be an array).</summary>
        public int Rank => _Rank;


        /// <summary>Returns whether this vector is homogeneous, or in  other words, whether all sub-members are the same rank.</summary>
        private readonly bool _IsHomogeneous;

        
        private readonly IEvaluateable[] _ThisLevel;


        public Vector(Number n, params Number[] numbers)
        {
            _ThisLevel = new IEvaluateable[1 + numbers.Length];
            _ThisLevel[0] = n;
            numbers.CopyTo(_ThisLevel, 1);            
            _Rank = 1;
            _IsHomogeneous = true;
        }
        

        private Vector (Vector first, Vector second)
        {            
            _ThisLevel = new IEvaluateable[] { first, second };
            _Rank = first._Rank + 1;
            _IsHomogeneous = first._Rank==second._Rank && first._IsHomogeneous && second._IsHomogeneous;
        }

        private Vector(IEvaluateable[] existingSet, Vector addition)
        {
            
            if (existingSet[0] is Vector e_v)
            {
                _ThisLevel = existingSet.Concat(new Vector[] { addition }).ToArray();
                _Rank = addition._Rank + 1;
                _IsHomogeneous = addition._IsHomogeneous && existingSet.All(e => e is Vector v 
                                                                                 && v._IsHomogeneous 
                                                                                 && v._Rank == addition._Rank);
            }
            else throw new InvalidOperationException("Cannot combine dimensions.");
        }

        private bool IsBase => _ThisLevel.Length == 0 || !(_ThisLevel.First() is Vector);

        private bool IsHomogeneous => _IsHomogeneous;

        public int MaxIndex => _ThisLevel.Length - 1;

        public int MinIndex => 0;

        /// <summary>
        /// If two Vectors of equal dimension are combined, the resulting 'out' Vector will be one dimension higher with the given Vectors 
        /// as sub-Vectors.  If the vectors are one dimension different, the result 'out' will be a new Vector with all the sub-Vectors of 
        /// the larger, plus the smaller Vector, as sub-Vectors.
        /// </summary>
        /// <returns>Returns true if the vectors can be combined, with the combined vector in the 'out' variable.  Otherwise, returns 
        /// false, and the 'out' variable will be null.</returns>
        public static bool TryCombine(Vector a, Vector b, out Vector combined)
        {
            if (a._Rank == b._Rank) { combined = new Vector(a, b); return true; }
            else if (a.Rank == b._Rank + 1) { combined = new Vector(a._ThisLevel, b); return true; }
            else if (a._Rank + 1 == b._Rank) { combined = new Vector(b._ThisLevel, a); return true; }
            else { combined = null; return false; }
        }


        IEvaluateable IIndexable.this[IEvaluateable index]
        {
            get
            {
                if (index is Number n) return _ThisLevel[n];
                else if (index is Vector v)
                {
                    if (v._Rank != 1)
                        return new EvaluationError("Indexing must be done with a number or a single-dimension Vector.");
                    IEvaluateable sub = this;
                    for (int d = 0; d < v._ThisLevel.Length; d++)
                    {
                        if (sub is Vector host_v)
                        {
                            if (v._ThisLevel[d] is Number idx)
                            {
                                if (idx < host_v.MaxIndex || idx >= host_v.MinIndex)
                                    return new EvaluationError("Invalid index into Vector at dimension " + d + ": " + idx);
                                sub = host_v._ThisLevel[idx];
                                continue;
                            }
                        }
                        else  // A non-vector objects at this dimension.
                            return new EvaluationError("Cannot evaluate index into object: " + _ThisLevel[d].ToString() + " with given index vector TODO: more info helpful");

                    }
                    return sub;
                }
                else return new EvaluationError("Cannot index with object \"" + index.ToString() + "\"");
            }
        }

        IEvaluateable IEvaluateable.Evaluate() => this;


        public override string ToString() => "{" + string.Join<IEvaluateable>(IsBase ? ", " : "; ", _ThisLevel) + "}";


        public override bool Equals(object obj)
        {
            Vector other = obj as Vector;
            if (other == null) return false;
            if (_Rank != other._Rank) return false;
            if (_ThisLevel.Length != other._ThisLevel.Length) return false;
            for (int i = 0; i < _ThisLevel.Length; i++)            
                if (!_ThisLevel[i].Equals(other._ThisLevel[i])) return false;            
            return true;
        }

        public override int GetHashCode()=> unchecked(_ThisLevel.Sum(o => o.GetHashCode()));
    }
}
