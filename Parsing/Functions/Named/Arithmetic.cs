using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency.Functions
{
    [TypeControl.NonVariadic(0, TypeFlags.RealAny)]
    public class ABS : NamedFunction
    {
        protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
        {
            Number e = (Number)evaluatedInputs[0];
            return new Number(Math.Abs(e.Value));
        }
    }
}
