using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dependency
{
    public enum TypeFlags
    {
        Null = 1<<0,        
        Integer = 1<< 1,
        RealAny = 1 <<2 | Integer,
        String = 1 << 10,
        VectorInteger = 1 << 16 | Indexable,
        VectorReal = 1 << 17 | VectorInteger,
        VectorAny = 1 << 18 | VectorReal,
        Boolean = 1 << 19,
        ComplexAny = 1<<20 | RealAny,
        Range = 1 << 22 | Indexable,        
        Indexable = 1 << 29,
        Formula = 1 << 30,
        Error = 1 << 31,
        Any = ~0
    }
        
    public sealed class TypeControl 
    {
        internal static readonly IDictionary<Type, TypeControl> Catalogue = new Dictionary<Type, TypeControl>();
        public static TypeControl GetConstraints(Type type)
        {
            // If it's already in the catalogue, just return that.
            if (Catalogue.TryGetValue(type, out TypeControl tc))
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
            Catalogue[type] = tc;
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
        /// out variable will contain the index (and <paramref name="unmatchedArg"/> will be -1).  If at least one 
        /// constraint matched the <paramref name="objects"/> count, then the first non-matching argument index will 
        /// be signalled in <paramref name="unmatchedArg"/>.  If not constraints matched the <paramref name="objects"/>
        /// count, then both <paramref name="bestIndex"/> and <paramref name="unmatchedArg"/> will be -1.
        /// </summary>
        public bool TryMatchType(IList<object> objects, out int bestIndex, out int unmatchedArg)
        {
            bestIndex = -1;
            unmatchedArg = -1;

            if (_Constraints.Count == 0) return true;


            foreach (Constraint constraint in _Constraints)
            {
                if (!constraint.MatchesCount(objects.Count))
                    continue;

                for (int argIdx = 0; argIdx < objects.Count; argIdx++)
                {
                    TypeFlags allowed = constraint[argIdx];
                    if (objects[argIdx] is ITypeGuarantee itg)
                    {
                        TypeFlags objType = itg.TypeGuarantee;
                        if ((allowed & objType) != objType)
                        {
                            if (argIdx <= unmatchedArg) continue;
                            unmatchedArg = argIdx;
                            bestIndex = constraint.Index;
                            break;
                        }
                        else
                        {
                            bestIndex = constraint.Index;
                            unmatchedArg = -1;
                            return true;
                        }
                    }
                    else if (allowed != TypeFlags.Any)
                        break;
                }
            }
            return false;
        }

        [AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
        public sealed class VariadicAttribute : Attribute
        {
            public readonly int Index;
            public TypeFlags[] TypeFlags;
            public VariadicAttribute(int index, params TypeFlags[] typeFlags) { this.Index = index; this.TypeFlags = typeFlags; }
            public VariadicAttribute(params TypeFlags[] typeFlags) : this(0, typeFlags) { }
        }

        [AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
        public sealed class NonVariadicAttribute : Attribute
        {
            public readonly int Index;
            public TypeFlags[] TypeFlags;
            public NonVariadicAttribute(int index, params TypeFlags[] typeFlags) { this.Index = index; this.TypeFlags = typeFlags; }
            public NonVariadicAttribute(params TypeFlags[] typeFlags) : this(0, typeFlags) { }
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
