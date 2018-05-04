using DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.Parsing.Functions
{

    /// <summary>
    /// A special type of function typically written with a special relationship to its inputs.  For 
    /// example, addition could be written "add(a,b)", instead we use "a + b" with a special symbol 
    /// in between its two inputs.
    /// </summary>
    internal abstract class Operator : Function
    {
        public override string Name => base.Name + "&Operator";

        protected internal override IEvaluatable Parse(DynamicLinkedList<object>.Node node) => Parse(node, 1, 1);


    }


    internal sealed class Addition : Operator
    {
        protected internal override ParsingPriority Priority => ParsingPriority.Addition;

        protected internal override IEvaluatable EvaluateFunction(IList<IEvaluatable> evaluatedInputs)
        {
            decimal m = 0.0m;
            List<IEvaluatable> expressions = new List<IEvaluatable>();
            foreach (IEvaluatable input in evaluatedInputs)
            {
                if (input is Number n) m += n.Value;
                else expressions.Add(input);
            }
            if (expressions.Count > 0) throw new NotImplementedException();
            return new Number(m);
        }
        protected internal override string ToString(Clause n) => string.Join(" + ", (IEnumerable<IEvaluatable>)n.Inputs);
    }


    internal sealed class And : Operator
    {
        protected internal override ParsingPriority Priority => ParsingPriority.And;

        protected internal override IEvaluatable EvaluateFunction(IList<IEvaluatable> evaluatedInputs)
        {
            bool b = true;
            List<IEvaluatable> expressions = new List<IEvaluatable>();
            foreach (IEvaluatable input in evaluatedInputs)
            {
                if (input is Boolean i) b &= i;
                else expressions.Add(input);
            }
            if (expressions.Count > 0) throw new NotImplementedException();
            return new Boolean(b);
        }
        protected internal override string ToString(Clause n) => string.Join(" & ", (IEnumerable<IEvaluatable>)n.Inputs);
    }


    internal sealed class Concatenation : Operator
    {
        protected internal override ParsingPriority Priority => ParsingPriority.Concatenation;

        protected internal override IEvaluatable EvaluateFunction(IList<IEvaluatable> evaluatedInputs)
        {
            StringBuilder sb = new StringBuilder();
            List<IEvaluatable> expressions = new List<IEvaluatable>();
            foreach (IEvaluatable input in evaluatedInputs) sb.Append(input.ToString());
            if (expressions.Count > 0) throw new NotImplementedException();
            return new String(sb.ToString());
        }
        protected internal override string ToString(Clause n) => string.Join(" & ", (IEnumerable<IEvaluatable>)n.Inputs);
    }


    internal sealed class Division : Operator
    {
        protected internal override ParsingPriority Priority => ParsingPriority.Division;

        protected internal override IEvaluatable EvaluateFunction(IList<IEvaluatable> evaluatedInputs)
        {
            if (evaluatedInputs.Count != 2) return new Error("Division must have exactly two inputs.");
            if (evaluatedInputs[0] is Number a && evaluatedInputs[1] is Number b) return a / b;
            throw new NotImplementedException();
        }
        protected internal override string ToString(Clause n) => string.Join(" / ", (IEnumerable<IEvaluatable>)n.Inputs);
    }


    internal sealed class Exponentiation : Operator
    {
        protected internal override ParsingPriority Priority => ParsingPriority.Exponentiation;

        protected internal override IEvaluatable EvaluateFunction(IList<IEvaluatable> evaluatedInputs)
        {
            decimal m = 0.0m;
            List<IEvaluatable> expressions = new List<IEvaluatable>();
            foreach (IEvaluatable input in evaluatedInputs)
            {
                if (input is Number n) m ^= n;
                else expressions.Add(input);
            }
            if (expressions.Count > 0) throw new NotImplementedException();
            return new Number(m);
        }
        protected internal override string ToString(Clause n) => string.Join(" ^ ", (IEnumerable<IEvaluatable>)n.Inputs);
    }


    internal sealed class Multiplication : Operator
    {
        protected internal override ParsingPriority Priority => ParsingPriority.Multiplication;

        protected internal override IEvaluatable EvaluateFunction(IList<IEvaluatable> evaluatedInputs)
        {
            decimal m = 0.0m;
            List<IEvaluatable> expressions = new List<IEvaluatable>();
            foreach (IEvaluatable input in evaluatedInputs)
            {
                if (input is Number n) m *= n;
                else expressions.Add(input);
            }
            if (expressions.Count > 0) throw new NotImplementedException();
            return new Number(m);
        }
        protected internal override string ToString(Clause n) => string.Join(" * ", (IEnumerable<IEvaluatable>)n.Inputs);
    }


    internal sealed class Negation : Operator
    {
        protected internal override ParsingPriority Priority => ParsingPriority.Negation;

        protected internal override IEvaluatable Parse(DynamicLinkedList<object>.Node node) => Parse(node, 0, 1);

        protected internal override IEvaluatable EvaluateFunction(IList<IEvaluatable> evaluatedInputs)
        {
            if (evaluatedInputs.Count != 1) return new Error("Negation must have exactly one input.");
            switch (evaluatedInputs[0])
            {
                case Number n: return -n;
                case Boolean b: return !b;
            }
            throw new NotImplementedException();
        }


        protected internal override string ToString(Clause clause)
        {
            switch (clause.Inputs[0])
            {
                case Number n: return "-" + n.ToString();
                case Boolean b: return "!" + b.ToString();
            }
            return base.ToString();
        }
    }


    internal sealed class Or : Operator
    {
        protected internal override ParsingPriority Priority => ParsingPriority.Or;

        protected internal override IEvaluatable EvaluateFunction(IList<IEvaluatable> evaluatedInputs)
        {
            bool b = false;
            List<IEvaluatable> expressions = new List<IEvaluatable>();
            foreach (IEvaluatable input in evaluatedInputs)
            {
                if (input is Boolean i) b |= i;
                else expressions.Add(input);
            }
            if (expressions.Count > 0) throw new NotImplementedException();
            return new Boolean(b);
        }
        protected internal override string ToString(Clause n) => string.Join(" & ", (IEnumerable<IEvaluatable>)n.Inputs);
    }


    internal sealed class Range : Function
    {
        protected internal override ParsingPriority Priority => ParsingPriority.Range;

        protected internal override IEvaluatable EvaluateFunction(IList<IEvaluatable> evaluatedInputs)
        {
            throw new NotImplementedException();
        }
    }


    internal sealed class Relation : Function
    {
        protected internal override ParsingPriority Priority => ParsingPriority.Relation;

        protected internal override IEvaluatable EvaluateFunction(IList<IEvaluatable> evaluatedInputs)
        {
            throw new NotImplementedException();
        }
    }


    internal sealed class Subtraction : Operator
    {
        protected internal override ParsingPriority Priority => ParsingPriority.Subtraction;

        protected internal override IEvaluatable EvaluateFunction(IList<IEvaluatable> evaluatedInputs)
        {
            if (evaluatedInputs.Count != 2) return new Error("Subtraction must have exactly two inputs.");
            if (evaluatedInputs[0] is Number a && evaluatedInputs[1] is Number b) return a - b;
            throw new NotImplementedException();
        }
        protected internal override string ToString(Clause n) => string.Join(" - ", (IEnumerable<IEvaluatable>)n.Inputs);
    }
}
