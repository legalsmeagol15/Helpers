using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Parsing.Functions
{
    internal sealed class Simplify : Function
    {
        protected override IEvaluatable GetDerivative(Variable v)
        {
            throw new NotImplementedException();
        }

        protected internal override IEvaluatable Evaluate(params IEvaluatable[] evaluatedInputs)
        {
            throw new NotImplementedException();
            
        }
    }
}
