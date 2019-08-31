using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Dependency.TypeControl;

namespace Dependency.Functions
{
    public abstract class Function : IFunction, IDynamicEvaluateable
    {
        internal IDynamicEvaluateable Parent { get; set; }
        private IList<IEvaluateable> _Inputs;
        protected internal IList<IEvaluateable> Inputs
        {
            get => _Inputs;
            internal set
            {
                _Inputs = value;
                Update();
                foreach (var iev in value) if (iev is IDynamicEvaluateable ide) ide.Parent = this;
            }
        }

        IList<IEvaluateable> IFunction.Inputs => Inputs;

        public IEvaluateable Value { get; private set; }
        IDynamicEvaluateable IDynamicEvaluateable.Parent { get => Parent; set => Parent = value; }

        public IEvaluateable Update()
        {
            IEvaluateable[] evalInputs = _Inputs.Select(s => s.Value).ToArray();

            TypeControl tc;
            if (this is ICacheValidator icv) tc = icv.TypeControl ?? (icv.TypeControl = TypeControl.GetConstraints(this.GetType()));
            else tc = TypeControl.GetConstraints(this.GetType());

            IEvaluateable newValue;
            if (tc.TryMatchType(evalInputs, out int bestConstraint, out int unmatchedArg))
                newValue = Evaluate(evalInputs, bestConstraint);
            else if (bestConstraint < 0)
                newValue = new InputCountError(this, evalInputs, tc);
            else
                newValue = new TypeMismatchError(this, evalInputs, bestConstraint, unmatchedArg, tc);

            if (newValue.Equals(Value)) return Value;
            Value = newValue;
            if (Parent != null) Parent.Update();
            return Value;
        }
        
        protected abstract IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex);
        
    }
}
