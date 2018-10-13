using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing.Functions
{
    [Serializable]
    internal sealed class Sin : Function
    {
        internal Sin(params IEvaluateable[] inputs) : base(inputs) { }
        protected override IEvaluateable GetDerivative(Variable v) => ApplyChainRule(v, new Cos(Inputs), Inputs[0]);
        public override IEvaluateable Evaluate(params IEvaluateable[] evaluatedInputs)
        {
            if (evaluatedInputs.Length != 1) return InputCountError(evaluatedInputs, 1);
            if (evaluatedInputs[0] is Number n) return new Number(Math.Sin(n));
            return InputTypeError(evaluatedInputs, 0, typeof(Number));
        }
    }

    [Serializable]
    internal sealed class Cos : Function
    {
        internal Cos(params IEvaluateable[] inputs) : base(inputs) { }

        protected override IEvaluateable GetDerivative(Variable v) => ApplyChainRule(v, new Negation(new Sin(Inputs)), Inputs[0]);

        public override IEvaluateable Evaluate(params IEvaluateable[] evaluatedInputs)
        {
            if (evaluatedInputs.Length != 1) return InputCountError(evaluatedInputs, 1);
            if (evaluatedInputs[0] is Number n) return new Number(Math.Cos(n));
            return InputTypeError(evaluatedInputs, 0, typeof(Number));
        }
    }

    /// <summary>
    /// The standard trig functions...
    /// </summary>
    [Serializable]
    internal sealed class Tan : Function
    {
        internal Tan(params IEvaluateable[] inputs) : base(inputs) { }

        protected override IEvaluateable GetDerivative(Variable v) => throw new NotImplementedException("Derivative of TAN is SEC^2");

        public override IEvaluateable Evaluate(params IEvaluateable[] evaluatedInputs)
        {
            if (evaluatedInputs.Length != 1) return InputCountError(evaluatedInputs, 1);
            if (evaluatedInputs[0] is Number n) return new Number(Math.Tan(n));
            return InputTypeError(evaluatedInputs, 0, typeof(Number));
        }
    }
}
