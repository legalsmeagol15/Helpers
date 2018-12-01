using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parsing.Dependency;

namespace Parsing.Functions
{
    [Serializable]
    internal sealed class Abs : Function
    {
        internal Abs() : base() { }
        public override IEvaluateable Evaluate(params IEvaluateable[] evaluatedInputs)
        {
            if (evaluatedInputs.Length != 1) return InputCountError(evaluatedInputs, 1);
            switch (evaluatedInputs[0])
            {
                case Number n: return (n.Value < 0) ? new Number(-n.Value) : n;
                case Clause c when c is Negation: return c.Inputs[0];
            }          

            return InputTypeError(evaluatedInputs, 0, typeof(Number));            
        }

        protected override IEvaluateable GetDerivative(Variable v)
        {
            Hybrid p = new Hybrid();
            p.AddPortion(decimal.MinValue, true, 0m, false, new Negation(Differentiate(Inputs[0], v)));
            p.AddPortion(0m, false, decimal.MaxValue, true, Differentiate(Inputs[0], v));
            return p;
        }
    }

    [Serializable]
    internal sealed class Hybrid : Function
    {
        private class DomainInterval
        {
            public bool IncludeFrom = true;
            public decimal From;
            public decimal To;
            public bool IncludeTo = false;
            public IEvaluateable Evaluator;
        }

        
        private List<DomainInterval> Intervals = new List<DomainInterval>();

        public bool AddPortion(decimal from , bool includeFrom, decimal to, bool includeTo, IEvaluateable evaluator)
        {
            DomainInterval di = new DomainInterval();
            di.From = from;
            di.To = to;
            di.IncludeFrom = includeFrom;
            di.IncludeTo = includeTo;
            di.Evaluator = evaluator;
            this.Intervals.Add(di);
            return true;
        }
        public override IEvaluateable Evaluate(params IEvaluateable[] evaluatedInputs)
        {
            if (evaluatedInputs.Length != 1) return InputCountError(evaluatedInputs, 1);
            if (evaluatedInputs[0] is Number n)
            {
                IEvaluateable e = null;
                foreach (DomainInterval interval  in Intervals)
                {
                    if (n > interval.From && n < interval.To) { e = interval.Evaluator; break; }
                    else if (n == interval.From && interval.IncludeFrom) { e = interval.Evaluator; break; }
                    else if (n == interval.To && interval.IncludeTo) { e = interval.Evaluator; break; }
                }
                if (e != null)
                {
                    if (e is Function f) return f.Evaluate(evaluatedInputs);
                    return e;
                }
                return new EvaluationError("Undefined.");
            }
            else return InputTypeError(evaluatedInputs, 0, typeof(Number));

        }

        protected override IEvaluateable GetDerivative(Variable v)
        {
            throw new NotImplementedException();
        }
    }

    internal sealed class Max : Function
    {
        internal Max() : base() { }
        public override IEvaluateable Evaluate(params IEvaluateable[] inputs)
        {
            if (inputs.Length != 2) return InputCountError(inputs, 2);
            if (inputs[0] is Number a && inputs[1] is Number b) return new Number(Math.Max(a.Value, b.Value));
            if (inputs[0] is Number) return InputTypeError(inputs, 1, typeof(Number));
            return InputTypeError(inputs, 0, typeof(Number));
        }
    }
    internal sealed class Min : Function
    {
        internal Min() : base() { }
        public override IEvaluateable Evaluate(params IEvaluateable[] inputs)
        {
            if (inputs.Length != 2) return InputCountError(inputs, 2);
            if (inputs[0] is Number a && inputs[1] is Number b) return new Number(Math.Min(a.Value, b.Value));
            if (inputs[0] is Number) return InputTypeError(inputs, 1, typeof(Number));
            return InputTypeError(inputs, 0, typeof(Number));
        }
    }

    internal sealed class Sqrt : Function
    {
        internal Sqrt() : base() { }
        public override IEvaluateable Evaluate(params IEvaluateable[] inputs)
        {
            if (inputs.Length != 1) return InputCountError(inputs, 1);
            if (inputs[0] is Number n)
            {
                if (n.Value < 0) return new EvaluationError("Cannot take square root of a negative number.");
                return new Number(Math.Sqrt((double)n.Value));
            }
            return InputTypeError(inputs, 0, typeof(Number));
        }
    }
}
