using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Dependency.TypeControl;

namespace Dependency.Functions
{
    public abstract class Function : IFunction, IDynamicItem
    {
        internal IDynamicItem Parent { get; set; }
        private IList<IEvaluateable> _Inputs;
        protected internal IList<IEvaluateable> Inputs
        {
            get => _Inputs;
            internal set
            {
                _Inputs = value;
                foreach (var iev in value)
                    if (iev is IDynamicItem ide)
                        ide.Parent = this;
            }
        }

        internal IEvaluateable Recalculate()
        {
            throw new NotImplementedException();
        }

        IList<IEvaluateable> IFunction.Inputs => Inputs;

        public IEvaluateable Value { get; private set; }
        IDynamicItem IDynamicItem.Parent { get => Parent; set => Parent = value; }

        public bool Update()
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

            if (newValue.Equals(Value)) return false;
            Value = newValue;
            return true;
        }
        
        /// <summary>
        /// A function will call this method to evaluate the given evaluated inputs.
        /// </summary>
        /// <param name="evaluatedInputs"></param>
        /// <param name="constraintIndex"></param>
        /// <returns></returns>
        protected abstract IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex);
        
    }
}
