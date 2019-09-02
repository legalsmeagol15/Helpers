using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency.Functions
{
    [TypeControl.NonVariadic(0, TypeFlags.RealAny)]
    public class Cos : NamedFunction, ICategorized
    {
        IList<string> ICategorized.Categories => new string[] { "Trigonometry" };

        protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
        {
            Number e = (Number)evaluatedInputs[0];
            return new Number(Math.Cos((double)e.CLR_Value));
        }
    }

    [TypeControl.NonVariadic(0, TypeFlags.RealAny)]
    public class Cosh : NamedFunction, ICategorized
    {
        IList<string> ICategorized.Categories => new string[] { "Trigonometry" };

        protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
        {
            Number e = (Number)evaluatedInputs[0];
            return new Number(Math.Cosh((double)e.CLR_Value));
        }
    }
    
    [TypeControl.NonVariadic(0, TypeFlags.RealAny)]
    public class Sin : NamedFunction, ICategorized
    {
        IList<string> ICategorized.Categories => new string[] { "Trigonometry" };

        protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
        {
            Number e = (Number)evaluatedInputs[0];
            return new Number(Math.Sin((double)e.CLR_Value));
        }
    }

    [TypeControl.NonVariadic(0, TypeFlags.RealAny)]
    public class Sinh : NamedFunction, ICategorized
    {
        IList<string> ICategorized.Categories => new string[] { "Trigonometry" };

        protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
        {
            Number e = (Number)evaluatedInputs[0];
            return new Number(Math.Sinh((double)e.CLR_Value));
        }
    }

    [TypeControl.NonVariadic(0, TypeFlags.RealAny)]
    public class Tan : NamedFunction, ICategorized
    {
        IList<string> ICategorized.Categories => new string[] { "Trigonometry" };

        protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
        {
            Number e = (Number)evaluatedInputs[0];
            return new Number(Math.Tan((double)e.CLR_Value));
        }
    }

    [TypeControl.NonVariadic(0, TypeFlags.RealAny)]
    public class Tanh : NamedFunction
    {
        protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
        {
            Number e = (Number)evaluatedInputs[0];
            return new Number(Math.Tanh((double)e.CLR_Value));
        }
    }


}
