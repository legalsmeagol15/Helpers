using DataStructures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing.Functions
{

    /// <summary>
    /// A special type of function typically written with a special relationship to its inputs.  For 
    /// example, addition could be written "add(a,b)", instead we use "a + b" with a special symbol 
    /// in between its two inputs.
    /// </summary>
    internal abstract class Operator : Function
    {
        protected internal Operator(params IEvaluatable[] inputs) : base(inputs) { }
       
        protected internal override void ParseNode(DynamicLinkedList<object>.Node node) => ParseNode(node, 1, 1);

        protected abstract string Symbol { get; }

        public override string ToString() =>
            (Opener != "" ? Opener + " " : "") + string.Join(" " + Symbol + " ", (IEnumerable<IEvaluatable>)Inputs) + (Closer != "" ? " " + Closer : "");
    }


    internal sealed class Addition : Operator, IDerivable
    {
        protected override string Symbol => "+";
        protected internal override ParsingPriority Priority => ParsingPriority.Addition;
        //internal Addition(params IEvaluatable[] inputs) : base(inputs) { }
        protected internal override IEvaluatable Evaluate(params IEvaluatable[] evaluatedInputs)
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


        IEvaluatable IDerivable.GetDerivative(params IDerivable[] inputs)
        {
            switch (inputs.Length)
            {
                case 0: return InputCountError(inputs.OfType<IEvaluatable>().ToList(), 2);
                case 1: return inputs[0];
                case 2: Addition a = new Addition(); a.Inputs = inputs.ToArray(); return a;
                default:
                    int end = inputs.Length - 2;
                    Addition rhs = new Addition();
                    rhs.Inputs = new IEvaluatable[] { inputs[end], inputs[end + 1] };
                    for (int i = end - 3; i >= 0; i--) { rhs = new Addition(); rhs.Inputs = new IEvaluatable[] { inputs[i], rhs }; }                        
                    return rhs;
            }

        }

        IDerivable IEvaluatable<IDerivable>.Evaluate() => (IDerivable)base.Evaluate();

        IEvaluatable IEvaluatable.Evaluate() => base.Evaluate();

    }


    internal sealed class And : Operator
    {
        protected internal override ParsingPriority Priority => ParsingPriority.And;

        protected internal override IEvaluatable Evaluate(params IEvaluatable[] evaluatedInputs)
        {
            bool b = true;
            List<IEvaluatable> expressions = new List<IEvaluatable>();
            foreach (IEvaluatable input in evaluatedInputs)
            {
                if (input is Boolean i) b &= i;
                else expressions.Add(input);
            }
            if (expressions.Count > 0) throw new NotImplementedException();
            return b ? Boolean.True : Boolean.False;
        }
        protected override string Symbol => "&";
    }

    

    internal sealed class Division : Operator, IDerivable
    {

        protected override string Symbol => "/";

        protected internal override ParsingPriority Priority => ParsingPriority.Division;

        protected internal override IEvaluatable Evaluate(params IEvaluatable[] evaluatedInputs)
        {
            if (evaluatedInputs.Length != 2) return InputCountError(evaluatedInputs, 2);
            if (evaluatedInputs[0] is Number a && evaluatedInputs[1] is Number b) if (b == Number.Zero) return new Error("Division by zero."); else return a / b;

            throw new NotImplementedException();
        }

        IDerivable IEvaluatable<IDerivable>.Evaluate() => (IDerivable)base.Evaluate();

        IEvaluatable IEvaluatable.Evaluate() => base.Evaluate();

        IEvaluatable IDerivable.GetDerivative(params IDerivable[] inputs)
        {
            throw new NotImplementedException();
        }
    }


    internal sealed class Exponentiation : Operator
    {
        protected internal override ParsingPriority Priority => ParsingPriority.Exponentiation;

        protected internal override IEvaluatable Evaluate(params IEvaluatable[] evaluatedInputs)
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
        protected override string Symbol => "^";
    }


    internal sealed class Multiplication : Operator
    {
        protected internal override ParsingPriority Priority => ParsingPriority.Multiplication;

        protected internal override IEvaluatable Evaluate(params IEvaluatable[] evaluatedInputs)
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
        protected override string Symbol => "*";
    }


    internal sealed class Negation : Operator
    {
        protected internal override ParsingPriority Priority => ParsingPriority.Negation;

        protected internal override void ParseNode(DynamicLinkedList<object>.Node node) => ParseNode(node, 0, 1);

        protected internal override IEvaluatable Evaluate(params IEvaluatable[] evaluatedInputs)
        {
            if (evaluatedInputs.Length != 1) return InputCountError(evaluatedInputs, 1);
            switch (evaluatedInputs[0])
            {
                case Number n: return -n;
                case Boolean b: return !b;
            }
            return InputTypeError(evaluatedInputs, 0, typeof(Number), typeof(Boolean));
        }

        protected override string Symbol => "-";
        public override string ToString() => (Opener != "" ? Opener + " " : "") + "-" + Inputs[0].ToString() + (Closer != "" ? " " + Closer : "");
    }


    internal sealed class Or : Operator
    {
        protected internal override ParsingPriority Priority => ParsingPriority.Or;

        protected internal override IEvaluatable Evaluate(params IEvaluatable[] evaluatedInputs)
        {
            bool b = false;
            List<IEvaluatable> expressions = new List<IEvaluatable>();
            foreach (IEvaluatable input in evaluatedInputs)
            {
                if (input is Boolean i) b |= i;
                else expressions.Add(input);
            }
            if (expressions.Count > 0) throw new NotImplementedException();
            return Boolean.FromBool(b);
        }
        protected override string Symbol => "|";
    }


    internal sealed class Range : Operator
    {
        protected internal override ParsingPriority Priority => ParsingPriority.Range;

        protected internal override IEvaluatable Evaluate(params IEvaluatable[] evaluatedInputs)
        {
            throw new NotImplementedException();
        }
        protected override string Symbol => ":";
    }


    internal sealed class Relation : Operator
    {
        protected internal override ParsingPriority Priority => ParsingPriority.Relation;

        protected internal override IEvaluatable Evaluate(params IEvaluatable[] evaluatedInputs)
        {
            throw new NotImplementedException();
        }
        protected override string Symbol => ".";
    }


    internal sealed class Subtraction : Operator
    {
        protected internal override ParsingPriority Priority => ParsingPriority.Subtraction;

        protected internal override IEvaluatable Evaluate(params IEvaluatable[] evaluatedInputs)
        {
            if (evaluatedInputs.Length != 2) return InputCountError(evaluatedInputs, 2);
            if (evaluatedInputs[0] is Number a && evaluatedInputs[1] is Number b) return a - b;
            throw new NotImplementedException();
        }
        protected override string Symbol => "-";
    }

    #region Comparison operators

    internal abstract class Comparison : Function, IEvaluatable<Boolean>
    {
        protected abstract string Symbol { get; }

        protected internal sealed override IEvaluatable Evaluate(params IEvaluatable[] evaluatedInputs)
        {
            if (evaluatedInputs.Length != 2) InputCountError(evaluatedInputs, 2);
            return EvaluateComparison(evaluatedInputs[0], evaluatedInputs[1]);
        }

        protected abstract Boolean EvaluateComparison(IEvaluatable a, IEvaluatable b);

        Boolean IEvaluatable<Boolean>.Evaluate() => (Boolean)base.Evaluate();

        IEvaluatable IEvaluatable.Evaluate() => base.Evaluate();

    }

    
    internal sealed class EqualTo : Comparison
    {
        protected override string Symbol => "=";
        protected override Boolean EvaluateComparison(IEvaluatable a, IEvaluatable b)
        {
            if (a is Number nA && b is Number nB) return nA.Value == nB.Value;
            if (a is Boolean bA && b is Boolean bB) return bA.Value == bB.Value;
            return Boolean.False;
        }
    }

    internal sealed class GreaterThan : Comparison
    {
        protected override string Symbol => ">";

        protected override Boolean EvaluateComparison(IEvaluatable a, IEvaluatable b)
        {
            if (a is Number nA && b is Number nB) return nA > nB;
            return Boolean.False;
        }
    }

    internal sealed class LessThan : Comparison
    {
        protected override string Symbol => ">";

        protected override Boolean EvaluateComparison(IEvaluatable a, IEvaluatable b)
        {
            if (a is Number nA && b is Number nB) return nA < nB;
            return Boolean.False;
        }
    }

    internal sealed class NotEqualTo : Comparison
    {
        protected override string Symbol => "!=";
        protected override Boolean EvaluateComparison(IEvaluatable a, IEvaluatable b)
        {
            if (a is Number nA && b is Number nB) return nA.Value != nB.Value;
            if (a is Boolean bA && b is Boolean bB) return bA.Value != bB.Value;
            return Boolean.True;
        }
    }


    #endregion
}
