using DataStructures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Parsing.Function;

namespace Parsing.Functions
{
    [Serializable]
    /// <summary>
    /// A special type of function typically written with a special relationship to its inputs.  For 
    /// example, addition could be written "add(a,b)", instead we use "a + b" with a special symbol 
    /// in between its two inputs.
    /// </summary>
    internal abstract class Operator : Function
    {
        protected internal Operator(params IEvaluateable[] inputs) : base(inputs) { }

        protected internal override void ParseNode(DynamicLinkedList<object>.Node node) => ParseNode(node, 1, 1);

        protected abstract string Symbol { get; }

        public override bool IsNaked => false;

        public override string ToString() =>
            (Opener != "" ? Opener + " " : "") + string.Join(" " + Symbol + " ", (IEnumerable<IEvaluateable>)Inputs) + (Closer != "" ? " " + Closer : "");
    }




    #region Arithmetic operators

    [Serializable]
    internal sealed class Addition : Operator
    {

        internal Addition(params IEvaluateable[] inputs) : base(inputs) { }

        protected override string Symbol => "+";
        protected internal override ParsingPriority Priority => ParsingPriority.Addition;


        public override IEvaluateable Evaluate(params IEvaluateable[] evaluatedInputs)
        {

            decimal m = 0.0m;
            List<IEvaluateable> expressions = new List<IEvaluateable>();
            foreach (IEvaluateable input in evaluatedInputs)
            {
                if (input is Number n) m += n.Value;
                else expressions.Add(input);
            }
            if (expressions.Count > 0) throw new NotImplementedException();
            return new Number(m);
        }

        
    }

    [Serializable]
    internal sealed class Division : Operator
    {

        internal Division(params IEvaluateable[] inputs) : base(inputs) { }
        protected override string Symbol => "/";

        protected internal override ParsingPriority Priority => ParsingPriority.Division;



        public override IEvaluateable Evaluate(params IEvaluateable[] evaluatedInputs)
        {
            if (evaluatedInputs.Length != 2) return InputCountError(evaluatedInputs, 2);
            if (evaluatedInputs[0] is Number a && evaluatedInputs[1] is Number b)
            {
                if (b == Number.Zero) return new EvaluationError("Division by zero.");
                else return a / b;
            }

            throw new NotImplementedException();
        }

        


    }

    [Serializable]
    internal sealed class Exponentiation : Operator
    {

        protected override string Symbol => "^";
        internal Exponentiation(params IEvaluateable[] inputs) : base(inputs) { }
        protected internal override ParsingPriority Priority => ParsingPriority.Exponentiation;

        public override IEvaluateable Evaluate(params IEvaluateable[] evaluatedInputs)
        {
            if (evaluatedInputs.Length < 1) return InputCountError(evaluatedInputs, 1, 1000000);
            Number b;
            if (!(evaluatedInputs[0] is Number)) return InputTypeError(evaluatedInputs, 0, typeof(Number));
            else b = (Number)evaluatedInputs[0];

            for (int i = 1; i < evaluatedInputs.Length; i++)
            {
                if (!(evaluatedInputs[i] is Number)) return InputTypeError(evaluatedInputs, i, typeof(Number));
                b ^= (Number)evaluatedInputs[i];
            }
            return b;
        }

        
    }

    [Serializable]
    internal sealed class Multiplication : Operator
    {

        internal Multiplication(params IEvaluateable[] inputs) : base(inputs) { }
        protected internal override ParsingPriority Priority => ParsingPriority.Multiplication;

        public override IEvaluateable Evaluate(params IEvaluateable[] evaluatedInputs)
        {
            decimal m = 1.0m;
            List<IEvaluateable> expressions = new List<IEvaluateable>();
            foreach (IEvaluateable input in evaluatedInputs)
            {
                if (input is Number n) m *= n;
                else expressions.Add(input);
            }
            if (expressions.Count > 0) throw new NotImplementedException();
            return new Number(m);
        }
        protected override string Symbol => "*";
       
    }

    [Serializable]
    internal sealed class Negation : Operator
    {

        internal Negation(params IEvaluateable[] input) : base(input) { }
        protected internal override ParsingPriority Priority => ParsingPriority.Negation;

        protected internal override void ParseNode(DynamicLinkedList<object>.Node node) => ParseNode(node, 0, 1);

        public override IEvaluateable Evaluate(params IEvaluateable[] evaluatedInputs)
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


        protected override IEvaluateable GetDerivative(Variable v)
        {
            if (Inputs.Length != 1) throw new NotImplementedException();
            return this;
        }
    }

    [Serializable]
    internal sealed class Subtraction : Operator
    {
        internal Subtraction(params IEvaluateable[] inputs) : base(inputs) { }
        protected internal override ParsingPriority Priority => ParsingPriority.Subtraction;

        public override IEvaluateable Evaluate(params IEvaluateable[] evaluatedInputs)
        {
            if (evaluatedInputs.Length != 2) return InputCountError(evaluatedInputs, 2);
            if (evaluatedInputs[0] is Number a && evaluatedInputs[1] is Number b) return a - b;
            throw new NotImplementedException();
        }
        protected override string Symbol => "-";

        protected override IEvaluateable GetDerivative(Variable v) => new Subtraction(Inputs.Select(i => Differentiate(i, v)).ToArray());

    }

    #endregion





    #region Comparison operators

    [Serializable]
    internal abstract class Comparison : Function, IEvaluateable<Boolean>
    {
        protected abstract string Symbol { get; }

        public override IEvaluateable Evaluate(params IEvaluateable[] evaluatedInputs)
        {
            if (evaluatedInputs.Length != 2) return InputCountError(evaluatedInputs, 2);
            return EvaluateComparison(evaluatedInputs[0], evaluatedInputs[1]);
        }

        protected abstract Boolean EvaluateComparison(IEvaluateable a, IEvaluateable b);

        Boolean IEvaluateable<Boolean>.Evaluate() => (Boolean)Evaluate(GetEvaluatedInputs());

        IEvaluateable IEvaluateable.Evaluate() => Evaluate(GetEvaluatedInputs());


    }

    [Serializable]
    internal sealed class EqualTo : Comparison
    {
        protected override string Symbol => "=";

        protected internal override ParsingPriority Priority => ParsingPriority.Function;

        protected override Boolean EvaluateComparison(IEvaluateable a, IEvaluateable b)
        {
            if (a is Number nA && b is Number nB) return nA.Value == nB.Value;
            if (a is Boolean bA && b is Boolean bB) return bA.Value == bB.Value;
            return Boolean.False;
        }

    }

    [Serializable]
    internal sealed class GreaterThan : Comparison
    {
        protected override string Symbol => ">";

        protected internal override ParsingPriority Priority => ParsingPriority.Function;

        protected override Boolean EvaluateComparison(IEvaluateable a, IEvaluateable b)
        {
            if (a is Number nA && b is Number nB) return nA > nB;
            return Boolean.False;
        }

    }

    [Serializable]
    internal sealed class LessThan : Comparison
    {
        protected override string Symbol => ">";

        protected internal override ParsingPriority Priority => ParsingPriority.Function;

        protected override Boolean EvaluateComparison(IEvaluateable a, IEvaluateable b)
        {
            if (a is Number nA && b is Number nB) return nA < nB;
            return Boolean.False;
        }
    }

    [Serializable]
    internal sealed class NotEqualTo : Comparison
    {
        protected override string Symbol => "!=";

        protected internal override ParsingPriority Priority => ParsingPriority.Function;

        protected override Boolean EvaluateComparison(IEvaluateable a, IEvaluateable b)
        {
            if (a is Number nA && b is Number nB) return nA.Value != nB.Value;
            if (a is Boolean bA && b is Boolean bB) return bA.Value != bB.Value;
            return Boolean.True;
        }

    }


    #endregion





    #region Logical operators

    [Serializable]
    internal sealed class And : Operator
    {

        protected internal override ParsingPriority Priority => ParsingPriority.And;

        public override IEvaluateable Evaluate(params IEvaluateable[] evaluatedInputs)
        {
            bool b = true;
            List<IEvaluateable> expressions = new List<IEvaluateable>();
            foreach (IEvaluateable input in evaluatedInputs)
            {
                if (input is Boolean i) b &= i;
                else expressions.Add(input);
            }
            if (expressions.Count > 0) throw new NotImplementedException();
            return b ? Boolean.True : Boolean.False;
        }


        

        protected override string Symbol => "&";


    }

    [Serializable]
    internal sealed class Or : Operator
    {
        protected internal override ParsingPriority Priority => ParsingPriority.Or;

        public override IEvaluateable Evaluate(params IEvaluateable[] evaluatedInputs)
        {
            bool b = false;
            List<IEvaluateable> expressions = new List<IEvaluateable>();
            foreach (IEvaluateable input in evaluatedInputs)
            {
                if (input is Boolean i) b |= i;
                else expressions.Add(input);
            }
            if (expressions.Count > 0) throw new NotImplementedException();
            return Boolean.FromBool(b);
        }
        protected override string Symbol => "|";

        protected override IEvaluateable GetDerivative(Variable v) => NonDifferentiableFunctionError();
    }

    #endregion





    #region Reference operators

    [Serializable]
    internal sealed class Relation : Operator
    {
        protected internal override ParsingPriority Priority => ParsingPriority.Relation;

        private LinkedList<Context> _Chain = new LinkedList<Context>();
        internal Variable Variable = null;

        public override IEvaluateable Evaluate()
        {
            //if (Inputs.Length != 1) return InputCountError(Inputs, new int[] { 1 });
            //if (!(Inputs[0] is Variable)) return InputTypeError(Inputs, 0, new Type[] { typeof(Context) });
            return Variable.Evaluate();
        }

        public override IEvaluateable Evaluate(params IEvaluateable[] inputs) => Variable.Evaluate();

        protected internal override void ParseNode(DynamicLinkedList<object>.Node node)
        {
            if (node.Previous == null)
                throw new InvalidOperationException("Relation operator (.) must be preceded by a context.");
            switch (node.Previous.Remove())
            {
                case Relation r:
                    if (r.Variable != null)
                        throw new InvalidOperationException("Cannot apply relation operator to " + typeof(Variable).Name + ".");
                    foreach (Context ctxt in this._Chain) r._Chain.AddLast(ctxt);
                    this._Chain = r._Chain;
                    break;
                case Context c:
                    this._Chain.AddFirst(c); break;
                default:
                    throw new InvalidOperationException("Relation operator (.) must be preceded by a context.");
            }

            if (node.Next == null)
                throw new InvalidOperationException("Relation operator (.) must be followed by another "
                            + typeof(Context).Name + " or a " + typeof(Variable).Name + ".");
            else if (this.Variable != null)
                throw new InvalidOperationException("Relation operator (.) already refers to Variable " + this.Variable.Name + " in its chain.");
            switch (node.Next.Remove())
            {
                case Relation r:
                    foreach (Context ctxt in r._Chain)
                        this._Chain.AddLast(ctxt);                    
                    this.Variable = r.Variable; break;
                case Variable v:
                    // If this is the end of the relation, we've found the Variable.  Example:  line.length
                    if (node.Next == null || !(node.Next.Contents is Relation))
                        this.Variable = v;
                    // Otherwise, the Variable is functioning as a Context.  Example: line.point0.x
                    else if (v.Context != null)
                        this._Chain.AddLast(v.Context);
                    else
                        throw new InvalidOperationException(string.Format("Relation operator (.) points to a %s as a %s, but the %s has no %s.", typeof(Variable).Name, typeof(Context).Name, typeof(Variable).Name, typeof(Context).Name));
                    break;
                case Context c:
                    this._Chain.AddLast(c); break;
                default:
                    throw new InvalidOperationException("Relation operator (.) must be followed by another "
                        + typeof(Context).Name + " or a " + typeof(Variable).Name + ".");
            }

            this.Inputs = new IEvaluateable[]{ this.Variable};
        }

        protected override string Symbol => ".";

        public override string ToString() => string.Join(".", _Chain.Select(c => c.Name)) + "." + Variable.Name;
    }

    [Serializable]
    internal sealed class Span : Operator
    {
        protected internal override ParsingPriority Priority => ParsingPriority.Span;
        
        public override IEvaluateable Evaluate(params IEvaluateable[] inputs)
        {
            if (inputs.Length != 2) return InputCountError(inputs, new int[] { 2 });
            if (inputs[0] is Number a)
            {
                if (inputs[1] is Number b) return new Range(a, b);
                else return InputTypeError(inputs, 1, new Type[] { typeof(Number) });
            }
            else return InputTypeError(inputs, 0, new Type[] { typeof(Number) });
        }

        protected override string Symbol => ":";

    }

    #endregion









}
