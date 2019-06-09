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
        Vector = 1 << 17 | Indexable,
        Boolean = 1 << 18,
        Imaginary = 1 << 19,
        ComplexAny = NumberAny | Imaginary,
        Indexable = 1 << 20,
        Range = 1 << 21 | Indexable,
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
        /// constraint matched the <paramref name="objects"/> count, then the first non-matching argument index will 
        /// be signalled in <paramref name="unmatchedArg"/>.  If not constraints matched the <paramref name="objects"/>
        /// count, then both <paramref name="bestIndex"/> and <paramref name="unmatchedArg"/> will be -1.
        /// </summary>
        internal static bool TryMatch(IList<TypeConstraint> constraints, IList<object> objects, out int bestIndex, out int unmatchedArg)
        {
            bestIndex = -1;
            unmatchedArg = -1;
            if (!constraints.Any())
                return true;
            for (int constraintIdx = 0; constraintIdx < constraints.Count; constraintIdx++)
            {
                foreach (TypeConstraint constraint in constraints)
                {
                    if (!constraint.MatchesCount(objects.Count))
                        continue;

                    for (int argIdx = 0; argIdx < objects.Count; argIdx++)
                    {
                        TypeFlags allowed = constraint[argIdx];
                        if (objects[argIdx] is ITypeFlag itf)
                        {
                            TypeFlags objType = itf.Flags;
                            if ((allowed & objType) != objType)
                            {
                                if (argIdx <= unmatchedArg) continue;
                                unmatchedArg = argIdx;
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
                        else if (allowed != TypeFlags.Any)
                            break;
                    }
                    constraintIdx++;
                }
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

            public bool MatchesCount(int count) => IsVariadic ? count >= _Allowed.Length : count == _Allowed.Length;
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

            public sealed override string ToString()
            {
                if (_Allowed.Length == 0) return "()";
                return string.Join(",", _Allowed.Select(tf => tf.ToString())) + (IsVariadic ? ".." : "");
            }
        }
    }

    

}
