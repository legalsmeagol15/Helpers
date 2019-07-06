using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dependency.Functions;

namespace Dependency
{
    public sealed class Vector : Function, IEvaluateable, IIndexable, IContext, ILiteral<object[]>, ITypeGuarantee
    {
        internal Vector(IEvaluateable[] contents) { Inputs = contents; }
        public Vector() : this(new IEvaluateable[0]) { }


        public IEvaluateable this[params Number[] indices]
        {
            get
            {
                IEvaluateable result = Inputs[indices[0]];
                if (indices.Length == 1)
                    return result;
                else if (result is IIndexable idxable)
                    return idxable[indices.Skip(1).ToArray()];
                else
                    return new IndexingError(this, indices.OfType<IEvaluateable>(), "More ordinal indices than dimensions.");
            }
        }

        

        public int Size => Inputs.Count;

        public IEvaluateable MaxIndex => new Number(Inputs.Count - 1);

        public IEvaluateable MinIndex => Number.Zero;

        public IContext Parent { get; } = null;

        internal bool TryOrdinalize(out Number[] ordinals)
        {
            ordinals = new Number[this.Inputs.Count];
            for (int i = 0; i < this.Inputs.Count; i++)
            {
                IEvaluateable iev = this.Inputs[i].Value;
                if (iev is Number n) ordinals[i] = n;
                else return false;
            }
            return true;
        }

        protected override IEvaluateable Evaluate(IEvaluateable[] inputs, int constraintIdx) => (inputs.Length == 1) ? inputs[0] : new Vector(inputs);

        bool IContext.TryGetSubcontext(string token, out IContext ctxt) { ctxt = null; return false; }

        bool IContext.TryGetProperty(string token, out IEvaluateable src) { src = null; return false; }
        

        public static bool operator ==(Vector a, Vector b)
        {
            if (a.Inputs.Count != b.Inputs.Count) return false;
            for (int i = 0; i < a.Inputs.Count; i++) if (a != b) return false;
            return true;
        }
        public static bool operator !=(Vector a, Vector b) => !(a == b);
        
        object[] ILiteral<object[]>.CLRValue => Inputs.ToArray();
        TypeFlags ITypeGuarantee.TypeGuarantee => TypeFlags.VectorReal;
    }
}
