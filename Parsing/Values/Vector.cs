using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency
{
    public class Vector : Function, IEvaluateable, IIndexable, IContext
    {
        internal Vector(IEvaluateable[] contents) { Inputs = contents; }

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

        

        public int Size => Inputs.Length;

        public IEvaluateable MaxIndex => new Number(Inputs.Length - 1);

        public IEvaluateable MinIndex => Number.Zero;

        IContext IContext.Parent => throw new NotImplementedException();

        internal bool TryOrdinalize(out Number[] ordinals)
        {
            ordinals = new Number[this.Inputs.Length];
            for (int i = 0; i < this.Inputs.Length; i++)
            {
                IEvaluateable iev = this.Inputs[i].Value;
                if (iev is Number n) ordinals[i] = n;
                else return false;
            }
            return true;
        }

        protected override IEvaluateable Evaluate(IEvaluateable[] inputs) { return new Vector(inputs); }

        bool IContext.TryGetSubcontext(string token, out IContext ctxt) { ctxt = null; return false; }

        bool IContext.TryGetVariable(string token, out IVariable var) { var = null; return false; }

        bool IContext.TryGetConstant(string token, out IEvaluateable k)
        {
            switch (token.ToLower())
            {
                case "size":
                case "length": k = new Number(Inputs.Length); return true;
                default: k = null; return false;
            }
        }
    }
}
