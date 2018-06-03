using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Parsing.Context;

namespace Parsing.Functions
{
    [Serializable]
    internal sealed class Simplify : Function
    {
        protected override IEvaluateable GetDerivative(Variable v)
        {
            throw new NotImplementedException();
        }

        public override IEvaluateable Evaluate(params IEvaluateable[] evaluatedInputs)
        {
            throw new NotImplementedException();
            
        }
    }
}
