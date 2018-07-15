﻿using DataStructures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Parsing.Context;
using static Parsing.Context.Function;

namespace Parsing.Functions
{
    [Serializable]
    /// <summary>
    /// A special type of function typically written with a special relationship to its inputs.  For 
    /// example, addition could be written "add(a,b)", instead we use "a + b" with a special symbol 
    /// in between its two inputs.
    /// </summary>
    internal abstract class Operator : Context.Function
    {
        protected internal Operator(params IEvaluateable[] inputs) : base(inputs) { }

        protected internal override void ParseNode(DynamicLinkedList<object>.Node node) => ParseNode(node, 1, 1);

        protected abstract string Symbol { get; }

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


        protected override IEvaluateable GetDerivative(Context.Variable v) => new Addition(Inputs.Select(i => Differentiate(i, v)).ToArray());
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


        protected override IEvaluateable GetDerivative(Context.Variable v)
        {
            IEvaluateable f = Inputs[0], g = Inputs[1];
            IEvaluateable lhs_numerator = new Multiplication(Differentiate(f, v), g).GetSimplified();
            IEvaluateable rhs_numerator = new Multiplication(f, Differentiate(g, v)).GetSimplified();
            IEvaluateable numerator = new Subtraction(lhs_numerator, rhs_numerator).GetSimplified();
            IEvaluateable denominator = new Exponentiation(g, new Number(2m)).GetSimplified();
            return new Division(numerator, denominator);
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


        //protected override IEvaluatable ApplyChainRule() => GetDerivative();
        protected override IEvaluateable GetDerivative(Context.Variable v)
        {
            List<IEvaluateable> inputs = Inputs.ToList();
            while (inputs.Count > 2)
            {
                throw new NotImplementedException();
            }

            IEvaluateable @base = inputs[0], exponent = inputs[1];
            if (@base == v && exponent is Number n) return new Multiplication(n, new Exponentiation(v, n - 1));
            else throw new NotImplementedException();
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
        protected override IEvaluateable GetDerivative(Variable v)
        {
            if (Inputs.Length != 2) throw new NotImplementedException();
            IEvaluateable f = Inputs[0], g = Inputs[1];
            return new Addition(new Multiplication(Differentiate(f, v), g).GetSimplified(), new Multiplication(f, Differentiate(g, v)).GetSimplified());
        }
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


        protected override IEvaluateable GetDerivative(Context.Variable v) => NonDifferentiableFunctionError();

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

        public override IEvaluateable Evaluate()
        {
            if (Inputs.Length != 2) return InputCountError(Inputs, new int[] { 2 });

            // The parent context must always be determinable at compile time.  No exceptions.  If the target was parsed at "compile 
            // time", ie, when the expression was created, return its evaluation.
            if (Inputs[0] is Context context)
            {                
                switch (Inputs[1])
                {
                    case Variable v: return v.Value;
                    case Context sub: return sub.Evaluate();
                    default:
                        // The target was NOT parsed at compile time, evaluate it and see if a its evaluation can be returned.
                        return Evaluate(context, Inputs[1].Evaluate());
                }
            }


            return InputTypeError(Inputs, 0, new Type[] { typeof(Context) });
        }

        public override IEvaluateable Evaluate(params IEvaluateable[] inputs)
        {
            // Only if the reference was not determined at compile time should this method be invoked.
            Context context = (Context)inputs[0];
            if (inputs[1] is String str)
            {
                if (context.TryGet(str, out Variable dyn_var)) return dyn_var.Value;
                else if (context.TryGet(str, out Variable dyn_sub)) return dyn_sub.Value;
                else return new EvaluationError("Unrecognized member of context \"" + context.Name + "\":  " + str);
            }
            else return InputTypeError(inputs, 1, new Type[] { typeof(Variable), typeof(Context) });
        }

        protected internal override void ParseNode(DynamicLinkedList<object>.Node node)
        {
            // Relations parse backwards:  the leftmost relation in "a.b.c" should be at the top of the parse tree.
            ParseNode(node, 1, 1);
            if (Inputs[0] is Relation prior)
            {
                Inputs[0] = prior.Inputs[1];
                prior.Inputs[1] = this;
            }
        }

        protected override string Symbol => ".";
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
