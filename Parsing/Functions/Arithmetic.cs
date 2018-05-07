using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing.Functions
{
    internal sealed class Abs : Function
    {
        protected internal override IEvaluatable Evaluate(params IEvaluatable[] evaluatedInputs)
        {
            if (evaluatedInputs.Length != 1) return InputCountError(evaluatedInputs, 1);
            switch (evaluatedInputs[0])
            {
                case Number n: return (n.Value < 0) ? new Number(-n.Value) : n;
                case Clause c when c is Negation: return c.Inputs[0];
            }           

            return InputTypeError(evaluatedInputs, 0, typeof(Number));            
        }
    }

}
