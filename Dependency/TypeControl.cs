using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Dependency.Functions;

namespace Dependency
{
    public enum TypeFlags
    {
        /// <summary>This type would be an invalid value for any function</summary>
        None = 0,
        Null = 1 << 0,
        Integer = 1 << 1,
        RealAny = 1 << 2 | Integer | NegativeInfinity | PositiveInfinity,
        String = 1 << 10,
        Vector = 1 << 16 | Indexable,
        Boolean = 1 << 19,
        ComplexAny = 1 << 20 | RealAny,
        Range = 1 << 22 | Indexable,
        NegativeInfinity = 1 << 23,
        PositiveInfinity = 1 << 24,
        Infinite = NegativeInfinity | PositiveInfinity,
        Context = 1 << 28,
        Indexable = 1 << 29,
        Formula = 1 << 30,
        Error = 1 << 31,
        Any = ~0
    }


    /// <summary>
    /// An object which can determine type matching, given a CLR type whose inputs are specified by the 
    /// </summary>
    public sealed class TypeControl
    {
        private static readonly IDictionary<Type, TypeControl> _Catalogue = new Dictionary<Type, TypeControl>();
        public static TypeControl GetConstraints(Type type)
        {
            // If it's already in the catalogue, just return that.
            if (_Catalogue.TryGetValue(type, out TypeControl tc))
                return tc;

            // Otherwise, find all the possible constraints and build a new TypeControl, then add to the catalogue.
            List<Constraint> constraints = new List<Constraint>();
            foreach (Attribute attr in type.GetCustomAttributes(true))
            {
                if (attr is VariadicAttribute vAttr)
                    constraints.Add(new Constraint(vAttr.Index, true, vAttr.TypeFlags));
                else if (attr is NonVariadicAttribute nAttr)
                    constraints.Add(new Constraint(nAttr.Index, false, nAttr.TypeFlags));
            }
            tc = new TypeControl(type, constraints);
            _Catalogue[type] = tc;
            return tc;
        }

        /// <summary>The function with which these constraints are associated.</summary>
        public readonly Type FunctionType;
        private readonly List<Constraint> _Constraints;

        private TypeControl(Type functionType, List<Constraint> constraints)
        {
            this.FunctionType = functionType;
            constraints.Sort((a, b) => a.Index.CompareTo(b.Index));
            _Constraints = constraints;
        }

        /// <summary>
        /// Returns whether the given types match any of the constraint set.  If so, the <paramref name="bestIndex"/> 
        /// out variable will contain the index (and <paramref name="unmatchedArg"/> will be the size of the object 
        /// list).  If at least one constraint matched the <paramref name="objects"/> count, then the first non-
        /// matching argument index will be signalled in <paramref name="unmatchedArg"/>.  If not constraints matched 
        /// the <paramref name="objects"/> count, then both <paramref name="bestIndex"/> and 
        /// <paramref name="unmatchedArg"/> will be -1.
        /// </summary>
        /// <param name="objects">The evaluated objects we are trying to match to a type constraint.</param>
        /// <param name="bestIndex">The index of the best-matching type constraint.  If the <paramref name="objects"/> 
        /// count matches no <seealso cref="Constraint"/>, the returned result will be -1.</param>
        /// <param name="firstError">The first evaluated error from among the <paramref name="objects"/>.</param>
        /// <param name="unmatchedArg">The index of the first non-matching arg, if matching failed.  Will be -1 if 
        /// matching succeeded.</param>
        /// <returns>Returns true if a type constraint perfectly matched the given evaluated objects.</returns>
        public bool TryMatchTypes(IList<object> objects, out int bestIndex, out int unmatchedArg, out Error firstError)
        {
            bestIndex = -1;
            unmatchedArg = -1;
            firstError = null;

            if (_Constraints.Count == 0)
            {
                unmatchedArg = objects.Count + 1;
                return true;
            }

            // Convert the objects to controllable types.
            TypeFlags[] objTypes = new TypeFlags[objects.Count];
            for (int i = 0; i < objTypes.Length; i++)
            {
                object obj = objects[i];
                if (obj is Error err && firstError == null) firstError = err;
                if (obj is ITypeGuarantee itg)
                {
                    objTypes[i] = itg.TypeGuarantee;
                    // TODO:  see if the following can be removed, or possibly if the TypeFlags can be cached per Type.
                    if (obj is IContext) objTypes[i] |= TypeFlags.Context;
                    if (obj is IIndexable) objTypes[i] |= TypeFlags.Indexable;
                }
                else objTypes[i] = TypeFlags.Any;
            }


            // Now find a matching constraint for the object types, if one such constraint exists.
            int bestMatchedCount = 0;
            for (int constraintIdx = 0; constraintIdx <= _Constraints.Count; constraintIdx++)
            {
                Constraint constraint = _Constraints[constraintIdx];
                int thisMatchedCount = 0;
                int thisUnmatched = -1;
                if (!constraint.MatchesCount(objects.Count))
                    continue;

                // Check that each object matches the constraint.
                for (int objIdx = 0; objIdx < objects.Count; objIdx++)
                {
                    TypeFlags allowed = constraint[objIdx]; // This will return the correct TypeFlags even if Variadic
                    TypeFlags objType = objTypes[objIdx];

                    // Do we have a match that would fit this constraint better than other constraints?
                    if ((allowed & objType) != TypeFlags.None)
                    {
                        if (++thisMatchedCount > bestMatchedCount)
                        {
                            // If this is a new best constraint, then adopt the unmatched for this obj.
                            if (bestIndex != constraintIdx) unmatchedArg = thisUnmatched;
                            bestIndex = constraintIdx;
                            bestMatchedCount = thisMatchedCount;
                        }
                    }
                    else if (thisUnmatched < 0) 
                        thisUnmatched = objIdx;
                }

                // If we found the first perfect constraint match, return immediately.
                if (thisMatchedCount == objects.Count)
                    return true;
            }

            // No perfect match.
            return false;
        }



        private class Constraint : IEnumerable<TypeFlags>, IComparable<Constraint>
        {
            public readonly int Index;
            public readonly bool IsVariadic;
            private readonly TypeFlags[] _Allowed;

            internal Constraint(int index, bool isVariadic, params TypeFlags[] flags)
            {
                this.Index = index;
                this.IsVariadic = isVariadic;
                this._Allowed = flags;
            }

            /// <summary>
            /// Returns whether this constraint input count matches the given count.  This will take into account 
            /// whether the <see cref="Constraint"/> is variadic.
            /// </summary>
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

            private IEnumerable<TypeFlags> EnumerateAllowed()
            {
                foreach (TypeFlags flag in _Allowed) yield return flag;
                if (IsVariadic) yield return _Allowed[_Allowed.Length - 1];
                else yield break;
            }
            IEnumerator<TypeFlags> IEnumerable<TypeFlags>.GetEnumerator() => EnumerateAllowed().GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => EnumerateAllowed().GetEnumerator();

            public sealed override string ToString()
            {
                if (_Allowed.Length == 0) return "()";
                return string.Join(",", _Allowed.Select(tf => tf.ToString())) + (IsVariadic ? ".." : "");
            }

            int IComparable<Constraint>.CompareTo(Constraint other) => Index.CompareTo(other.Index);
        }

    }
}
