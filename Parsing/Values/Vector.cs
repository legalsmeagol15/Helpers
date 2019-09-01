using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dependency.Functions;

namespace Dependency
{
    public sealed class Vector : IFunction, IEvaluateable, IIndexable, ILiteral<object[]>, ITypeGuarantee, IContext
        // Though a Vector has inputs, it CANNOT be a Function.
    {
        public IList<IEvaluateable> Inputs { get; internal set; }
        public Vector(params IEvaluateable[] contents) { Inputs = contents; }
        public Vector() { }
        
        IEvaluateable IEvaluateable.Value => this;

        public IEvaluateable this[IEvaluateable ordinal]
        {
            get
            {
                if (ordinal is Number n)
                {
                    return Inputs[(int)n];
                }
                else
                    return new IndexingError(this, this, ordinal);
            }
        }

        

        public int Size => Inputs.Count;

        public IEvaluateable MaxIndex => new Number(Inputs.Count - 1);

        public IEvaluateable MinIndex => Number.Zero;
        
        public static bool operator ==(Vector a, Vector b)
        {
            if (a.Inputs.Count != b.Inputs.Count) return false;
            for (int i = 0; i < a.Inputs.Count; i++) if (a != b) return false;
            return true;
        }
        public static bool operator !=(Vector a, Vector b) => !(a == b);
        public override bool Equals(object obj) => (obj is Vector other) && this == other;
        public override int GetHashCode() { unchecked { return Inputs.Sum(i => i.GetHashCode()); } }

        bool IContext.TryGetSubcontext(object path, out IContext ctxt) { ctxt = null; return false; }

        internal bool TryGetProperty(object path, out IEvaluateable source)
        {
            source = null;
            int idx = -1;
            if (path is string str)
            {
                str = str.ToLower();
                if (str == "size" || str == "length") source = new Number(Inputs.Count);
            }
            else if (path is int i)
                idx = i;
            if (idx >= 0)
            {
                source = idx < Inputs.Count ? Inputs[idx].Value : Dependency.Null.Instance;
            }
            return source != null;
        }
        bool IContext.TryGetProperty(object path, out IEvaluateable source) => this.TryGetProperty(path, out source);

        object[] ILiteral<object[]>.CLRValue => Inputs.ToArray();
        TypeFlags ITypeGuarantee.TypeGuarantee => TypeFlags.VectorReal;
    }
}
