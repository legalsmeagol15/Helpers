using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency.Functions
{
    
    [NonVariadic(0, TypeFlags.RealAny)]
    public class Abs : NamedFunction
    {
        protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
        {
            Number e = (Number)evaluatedInputs[0];
            return new Number(Math.Abs(e.CLR_Value));
        }
    }

    [NonVariadic(0, TypeFlags.RealAny)]
    public class Acos : NamedFunction
    {
        protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
        {
            Number e = (Number)evaluatedInputs[0];
            return new Number(Math.Acos((double)e.CLR_Value));
        }
    }

    [NonVariadic(0, TypeFlags.RealAny)]
    public class Asin : NamedFunction
    {
        protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
        {
            Number e = (Number)evaluatedInputs[0];
            return new Number(Math.Asin((double)e.CLR_Value));
        }
    }

    [NonVariadic(0, TypeFlags.RealAny)]
    public class Atan : NamedFunction
    {
        protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
        {
            Number e = (Number)evaluatedInputs[0];
            return new Number(Math.Atan((double)e.CLR_Value));
        }
    }

    [NonVariadic(0, TypeFlags.RealAny)]
    public class Exp : NamedFunction
    {
        protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
        {
            Number e = (Number)evaluatedInputs[0];
            return new Number(Math.Exp((double)e.CLR_Value));
        }
    }

    [NonVariadic(0, TypeFlags.RealAny)]
    public class Int : NamedFunction
    {
        protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
        {
            Number e = (Number)evaluatedInputs[0];
            return new Number((int)e.CLR_Value);
        }
    }

    [NonVariadic(0, TypeFlags.RealAny, TypeFlags.Any)]
    public class Log : NamedFunction
    {
        protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
        {
            Number a = (Number)evaluatedInputs[0], b =(Number)evaluatedInputs[0];
            return new Number(Math.Log((double)a.CLR_Value, (double)b.CLR_Value));
        }
    }

    [NonVariadic(0, TypeFlags.RealAny)]
    public class Log10 : NamedFunction
    {
        protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
        {
            Number e = (Number)evaluatedInputs[0];
            return new Number(Math.Log10((double)e.CLR_Value));
        }
    }

    [Variadic(0, TypeFlags.RealAny)]
    public class Max : NamedFunction
    {
        protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
        {
            Number a = (Number)evaluatedInputs[0];
            for (int i = 1; i < evaluatedInputs.Length; i++)
            {
                Number b = (Number)evaluatedInputs[i];
                if (a.CLR_Value < b.CLR_Value) a = b;
            }
            return a;
        }
    }

    [Variadic(0, TypeFlags.RealAny)]
    public class Min : NamedFunction
    {
        protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
        {
            Number a = (Number)evaluatedInputs[0];
            for (int i = 1; i < evaluatedInputs.Length; i++)
            {
                Number b = (Number)evaluatedInputs[i];
                if (a.CLR_Value > b.CLR_Value) a = b;
            }
            return a;
        }
    }

    [NonVariadic(0, TypeFlags.RealAny, TypeFlags.RealAny)]
    public class Mod : NamedFunction
    {
        protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
        {
            Number a = (Number)evaluatedInputs[0], b = (Number)evaluatedInputs[1];
            return new Number(a.CLR_Value % b.CLR_Value);
        }
    }

    [NonVariadic(0, TypeFlags.RealAny, TypeFlags.RealAny)]
    public class Pow : NamedFunction
    {
        protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
        {
            Number a = (Number)evaluatedInputs[0], b = (Number)evaluatedInputs[1];
            return new Number(Math.Pow((double)a.CLR_Value, (double)b.CLR_Value));
        }
    }

    [NonVariadic(0, TypeFlags.RealAny, TypeFlags.RealAny)]
    public class Round : NamedFunction
    {
        protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
        {
            Number a = (Number)evaluatedInputs[0], b = (Number)evaluatedInputs[1];
            return new Number(Math.Round((decimal)a, (int)b));
        }
    }

    [NonVariadic(0, TypeFlags.RealAny)]
    public class Sign : NamedFunction
    {
        protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
        {
            Number e = (Number)evaluatedInputs[0];
            if (e.CLR_Value < 0) return new Number(-1);
            else if (e.CLR_Value > 0) return Number.One;
            else return Number.Zero;
        }
    }

    [NonVariadic(0, TypeFlags.RealAny)]
    public class Sqrt : NamedFunction
    {
        protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
        {
            Number e = (Number)evaluatedInputs[0];
            return new Number(Math.Sqrt((double)e.CLR_Value));
        }
    }
}
