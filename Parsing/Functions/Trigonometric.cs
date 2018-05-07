using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing.Functions
{
    internal sealed class Sin : Function
    {
        protected internal override IEvaluatable Evaluate(params IEvaluatable[] evaluatedInputs)
        {
            if (evaluatedInputs.Length != 1) return InputCountError(evaluatedInputs, 1);
            if (evaluatedInputs[0] is Number n) return new Number(Math.Sin(n));
            return InputTypeError(evaluatedInputs, 0, typeof(Number));
        }
    }

    internal sealed class Cos : Function
    {
        protected internal override IEvaluatable Evaluate(params IEvaluatable[] evaluatedInputs)
        {
            if (evaluatedInputs.Length != 1) return InputCountError(evaluatedInputs, 1);
            if (evaluatedInputs[0] is Number n) return new Number(Math.Cos(n));
            return InputTypeError(evaluatedInputs, 0, typeof(Number));
        }
    }

    internal sealed class Tan : Function
    {
        protected internal override IEvaluatable Evaluate(params IEvaluatable[] evaluatedInputs)
        {
            if (evaluatedInputs.Length != 1) return InputCountError(evaluatedInputs, 1);
            if (evaluatedInputs[0] is Number n) return new Number(Math.Tan(n));
            return InputTypeError(evaluatedInputs, 0, typeof(Number));
        }
    }
}
