using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency
{
    internal enum TypeFlags
    {
        Zero = 0,
        Number = 1 << 0,
        Positive = 1 << 1,
        Negative = 1 << 2,
        NonInteger = 1 << 3,
        Integer = 1 << 4,
        IntegerAny = Number | Zero | Positive | Negative | Integer,
        NumberAny = Number | Zero | Positive | Negative | NonInteger | Integer,
        String = 1 << 16,
        StringAny = String | Zero,
        Vector = 1 << 17,
        Boolean = 1 << 18,
        Imaginary = 1 << 19,
        ComplexAny = NumberAny | Imaginary,
        Indexable = 1 << 20,
        Range = 1 << 21,
        Empty = 1 << 28,
        Null = 1 << 29,
        ZeroNullEmpty = Zero | Null | Empty,
        Formula = 1 << 30,
        Error = 1 << 31,
        Any = ~Zero
    }

    public static class TypeControl
    {
        /// <summary>
        /// Returns whether the given types match any of the constraint set.  If so, the <paramref name="bestIndex"/> 
        /// out variable will contain the index (and <paramref name="unmatchedArg"/> will be -1).  If at least one 
        /// constraint matched the type count
        /// </summary>
        /// <param name="constraints"></param>
        /// <param name="types"></param>
        /// <param name="bestIndex"></param>
        /// <param name="unmatchedArg"></param>
        /// <returns></returns>
        internal static bool TryMatch(IEnumerable<TypeConstraint> constraints, IList<TypeFlags> types, out int bestIndex, out int unmatchedArg)
        {
            bestIndex = -1;
            unmatchedArg = -1;
            if (!constraints.Any())
                return true;
            int constraintIdx = 0;
            foreach (TypeConstraint constraint in constraints)
            {
                if (!constraint.MatchesCount(types.Count)) continue;
                
                int i;
                for (i = 0; i < types.Count; i++)
                {
                    TypeFlags inputType = types[i];
                    if ((constraint[i] & inputType) != inputType)
                    {
                        if (i <= unmatchedArg) continue;
                        unmatchedArg = i;
                        bestIndex = constraintIdx;
                        break;
                    }
                    else
                    {
                        bestIndex = constraintIdx;
                        unmatchedArg = -1;
                        return true;
                    }
                }
                constraintIdx++;
            }
            return false;            
        }

        public class TypeConstraint : IEnumerable<TypeFlags>
        {
            public readonly bool IsVariadic;

            private readonly TypeFlags[] _Allowed;

            private TypeConstraint(bool isVariadic, params TypeFlags[] flags) { this.IsVariadic = isVariadic; this._Allowed = flags; }
            internal static TypeConstraint Variadic(params TypeFlags[] flags) => new TypeConstraint(true, flags);
            internal static TypeConstraint Nonvariadic(params TypeFlags[] flags) => new TypeConstraint(false, flags);

            public bool MatchesCount(int count)
                => IsVariadic ? count >= _Allowed.Length : count == _Allowed.Length;
            internal TypeFlags this[int index]
            {
                get
                {
                    if (index < _Allowed.Length) return _Allowed[index];
                    else if (IsVariadic) return _Allowed[_Allowed.Length - 1];
                    throw new IndexOutOfRangeException();
                }
            }

            private IEnumerable<TypeFlags> Allowed()
            {
                foreach (TypeFlags flag in _Allowed) yield return flag;
                if (IsVariadic) yield return _Allowed[_Allowed.Length - 1];
                else yield break;
            }
            IEnumerator<TypeFlags> IEnumerable<TypeFlags>.GetEnumerator() => Allowed().GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => Allowed().GetEnumerator();
        }
    }

    

}
