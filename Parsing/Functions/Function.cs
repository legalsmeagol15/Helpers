using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Dependency.TypeControl;

namespace Dependency.Functions
{
    public abstract class Function : IFunction
    {
        protected internal IList<IEvaluateable> Inputs { get; internal set; }
        IList<IEvaluateable> IFunction.Inputs => Inputs;

        private IEvaluateable _Value = null;

        IEvaluateable IEvaluateable.Value => _Value;

        IEvaluateable IEvaluateable.UpdateValue()
        {
            IEvaluateable[] evaluatedInputs = new IEvaluateable[Inputs.Count];
            for (int i = 0; i < Inputs.Count; i++)
            {
                evaluatedInputs[i] = Inputs[i].UpdateValue();
                if (evaluatedInputs[i] is EvaluationError err) return err;
            }

            TypeControl tc;
            if (this is ICacheValidator icv) tc = icv.TypeControl ?? (icv.TypeControl = TypeControl.GetConstraints(this.GetType()));
            else tc = TypeControl.GetConstraints(this.GetType());
            if (tc.TryMatchType(evaluatedInputs, out int bestConstraint, out int unmatchedArg))
                return _Value = Evaluate(evaluatedInputs, bestConstraint);
            else if (bestConstraint < 0)
                return new InputCountError(this, evaluatedInputs, tc);
            else
                return new TypeMismatchError(this, evaluatedInputs, bestConstraint, unmatchedArg, tc);
        }

        protected abstract IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex);

        protected string ToExpression(IContext perspective) => this.ToString();
        string IEvaluateable.ToExpression(IContext perspective) => ToExpression(perspective);
    }
}
