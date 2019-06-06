using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataStructures;
using System.Diagnostics;

namespace Dependency
{
    public abstract class Operator : Function
    {

        protected Operator() { }

        public static bool TryCreate(string token, out Operator oper)
        {
            switch (token)
            {
                case "-": oper = new Subtraction(); return true; // Might still end up a negation
                case "!":
                case "~": oper = new Negation(); return true;
                case "+": oper = new Addition(); return true;
                case "*": oper = new Multiplication(); return true;
                case "/": oper = new Division(); return true;
                case "^": oper = new Exponentiation(); return true;
                case "&": oper = new And(); return true;
                case "|": oper = new Or(); return true;
                case ":": oper = new Range(); return true;
                default: oper = null; return false;
            }
        }

        internal abstract bool Parse(DataStructures.DynamicLinkedList<IEvaluateable>.Node node);

        protected bool ParseMany<T>(DataStructures.DynamicLinkedList<IEvaluateable>.Node node) where T : IEvaluateable
        {
            if (node.Previous == null || node.Next == null) return false;
            IEvaluateable prev = node.Previous.Remove(), next = node.Next.Remove();
            LinkedList<IEvaluateable> list = new LinkedList<IEvaluateable>();
            list.AddLast(prev);
            list.AddLast(next);
            while (node.Previous != null && node.Previous is T)
            {
                node.Previous.Remove();
                if (node.Previous == null) return false;
                list.AddFirst(node.Previous.Remove());
            }
            while (node.Next != null && node.Next is T)
            {
                node.Next.Remove();
                if (node.Next == null) return false;
                list.AddLast(node.Next.Remove());
            }
            this.Inputs = list.ToArray();
            return true;
        }
        protected bool ParseBinary(DataStructures.DynamicLinkedList<IEvaluateable>.Node node)
        {
            if (node.Previous == null || node.Next == null) return false;
            this.Inputs = new IEvaluateable[] { node.Previous.Remove(), node.Next.Remove() };
            return true;
        }

    }


    public sealed class Addition : Operator, IValidateValue
    {
        private static readonly InputConstraint[] _Constraints
            = new InputConstraint[]
            {
                new InputConstraint(true, TypeFlags.NumberAny, TypeFlags.NumberAny)
            };
        InputConstraint[] IValidateValue.GetConstraints() => _Constraints;

        protected override IEvaluateable Evaluate(IEvaluateable[] inputs, int constraintIdx)
        {
            switch (constraintIdx)
            {
                case 0:
                    Number sum = (Number)inputs[0];
                    for (int i = 1; i < inputs.Length; i++) sum += (Number)inputs[i];
                    return sum;

                default:
                    return new EvaluationError(this, inputs, "Have not implemented evaluation for constraint " + constraintIdx);

            }
        }

        internal override bool Parse(DynamicLinkedList<IEvaluateable>.Node node) => ParseMany<Addition>(node);

        public override string ToString() => string.Join(" + ", (IEnumerable<IEvaluateable>)Inputs);
    }


    public sealed class And : Operator, IValidateValue
    {
        private static readonly InputConstraint[] _Constraints
            = new InputConstraint[]
            {
                new InputConstraint(true, TypeFlags.Boolean)
            };
        InputConstraint[] IValidateValue.GetConstraints() => _Constraints;

        protected override IEvaluateable Evaluate(IEvaluateable[] inputs, int constraintIdx)
        {
            switch (constraintIdx)
            {
                case 0:
                    Boolean b = (Boolean)inputs[0];
                    for (int i = 1; i < inputs.Length; i++) b &= (Boolean)inputs[i];
                    return b;
                default:
                    return new EvaluationError(this, inputs, "Have not implemented evaluation for constraint " + constraintIdx);

            }
        }

        internal override bool Parse(DynamicLinkedList<IEvaluateable>.Node node) => ParseMany<And>(node);

        public override string ToString() => string.Join(" & ", (IEnumerable<IEvaluateable>)Inputs);
    }


    public sealed class Division : Operator, IValidateValue
    {
        private static readonly InputConstraint[] _Constraints
            = new InputConstraint[]
            {
                new InputConstraint(false, TypeFlags.NumberAny, TypeFlags.Number | TypeFlags.Positive | TypeFlags.Negative)
            };
        InputConstraint[] IValidateValue.GetConstraints() => _Constraints;

        protected override IEvaluateable Evaluate(IEvaluateable[] inputs, int constraintIndex)
        {
            switch (constraintIndex)
            {
                case 0:
                    return ((Number)inputs[0]) / ((Number)inputs[1]);
                default:
                    return new EvaluationError(this, inputs, "Have not implemented evaluation for constraint " + constraintIndex);
            }
        }

        internal override bool Parse(DynamicLinkedList<IEvaluateable>.Node node) => ParseBinary(node);

        public override string ToString() => string.Join(" / ", (IEnumerable<IEvaluateable>)Inputs);
    }


    public sealed class Exponentiation : Operator, IValidateValue
    {
        private static readonly InputConstraint[] _Constraints
            = new InputConstraint[]
            {
                new InputConstraint(true, TypeFlags.NumberAny, TypeFlags.NumberAny)
            };
        InputConstraint[] IValidateValue.GetConstraints() => _Constraints;

        protected override IEvaluateable Evaluate(IEvaluateable[] inputs, int constraintIndex)
        {
            switch (constraintIndex)
            {
                case 0:
                    double d = (double)((Number)inputs[0]).Value;
                    for (int i = 1; i < inputs.Length; i++)
                        d = Math.Pow(d, (double)((Number)inputs[i]).Value);
                    return new Number(d);
                default:
                    return new EvaluationError(this, inputs, "Have not implemented evaluation for constraint " + constraintIndex);
            }
        }
        internal override bool Parse(DynamicLinkedList<IEvaluateable>.Node node) => ParseBinary(node);

        public override string ToString() => string.Join(" ^ ", (IEnumerable<IEvaluateable>)Inputs);
    }


    public sealed class Indexing : Operator, IExpression, IValidateValue
    {
        //This is unique: an operator that is also a left-to-right expression.

        private static readonly InputConstraint[] _Constraints
            = new InputConstraint[]
            {
                new InputConstraint(false, TypeFlags.Indexable, TypeFlags.Vector | TypeFlags.Number | TypeFlags.Positive | TypeFlags.ZeroNullEmpty)
            };
        InputConstraint[] IValidateValue.GetConstraints() => _Constraints;

        internal IEvaluateable Base { get => Inputs[0]; set { Inputs[0] = value; } }

        /// <summary>The vector that this bracket object contains.</summary>
        internal IEvaluateable Ordinal { get => Inputs[1]; set { Inputs[1] = value; } }

        protected override IEvaluateable Evaluate(IEvaluateable[] inputs, int constraintIndex)
        {
            switch (constraintIndex)
            {
                case 0:
                    IIndexable idxable = (IIndexable)inputs[0];
                    Number[] ns = new Number[inputs.Length - 1];
                    for (int i = 1; i < inputs.Length; i++)
                    {
                        if (inputs[i] is Number n)
                            ns[i - 1] = n;
                        else
                            return new TypeMismatchError(this, inputs.Skip(1), TypeFlags.IntegerNatural, inputs[i], "Indexable ordinal must evaluate to a number or vector of numbers.");
                    }
                    return idxable[ns];
                default:
                    return new EvaluationError(this, inputs, "Have not implemented evaluation for constraint " + constraintIndex);
            }
        }

        internal override bool Parse(DynamicLinkedList<IEvaluateable>.Node node)
        {
            // The ordinal inputs were already parsed left-to-right as an expression.  Just need to parse the base.
            if (node.Previous == null) return false;
            this.Base = node.Previous.Remove();
            return true;
        }

        public override string ToString() => Base.ToString() + "{" + Ordinal.ToString() + "}";
    }


    public sealed class Multiplication : Operator, IValidateValue
    {
        private static readonly InputConstraint[] _Constraints
            = new InputConstraint[]
            {
                new InputConstraint(true, TypeFlags.NumberAny, TypeFlags.NumberAny)
            };
        InputConstraint[] IValidateValue.GetConstraints() => _Constraints;

        protected override IEvaluateable Evaluate(IEvaluateable[] inputs, int constraintIdx)
        {
            switch (constraintIdx)
            {
                case 0:
                    Number product = (Number)inputs[0];
                    for (int i = 1; i < inputs.Length; i++) product *= (Number)inputs[i];
                    return product;

                default:
                    return new EvaluationError(this, inputs, "Have not implemented evaluation for constraint " + constraintIdx);

            }
        }
        internal override bool Parse(DynamicLinkedList<IEvaluateable>.Node node) => ParseMany<Multiplication>(node);

        public override string ToString() => string.Join(" * ", (IEnumerable<IEvaluateable>)Inputs);
    }


    public sealed class Negation : Operator, IValidateValue
    {
        private static readonly InputConstraint[] _Constraints
            = new InputConstraint[]
            {
                new InputConstraint(false, TypeFlags.NumberAny)
            };
        InputConstraint[] IValidateValue.GetConstraints() => _Constraints;
        protected override IEvaluateable Evaluate(IEvaluateable[] inputs, int constraintIdx)
        {
            switch (constraintIdx)
            {
                case 0:
                    return -((Number)inputs[0]);
                default:
                    return new EvaluationError(this, inputs, "Have not implemented evaluation for constraint " + constraintIdx);

            }
        }
        internal override bool Parse(DynamicLinkedList<IEvaluateable>.Node node)
        {
            if (node.Next == null) return false;
            Inputs = new IEvaluateable[] { node.Next.Remove() };
            return true;
        }

        public override string ToString() => "-" + Inputs[0].ToString();
    }


    public sealed class Or : Operator, IValidateValue
    {
        private static readonly InputConstraint[] _Constraints
            = new InputConstraint[]
            {
                new InputConstraint(true, TypeFlags.Boolean)
            };
        InputConstraint[] IValidateValue.GetConstraints() => _Constraints;
        protected override IEvaluateable Evaluate(IEvaluateable[] inputs, int constraintIdx)
        {
            switch (constraintIdx)
            {
                case 0:
                    Boolean b = (Boolean)inputs[0];
                    for (int i = 1; i < inputs.Length; i++) b |= (Boolean)inputs[i];
                    return b;
                default:
                    return new EvaluationError(this, inputs, "Have not implemented evaluation for constraint " + constraintIdx);

            }
        }
        internal override bool Parse(DynamicLinkedList<IEvaluateable>.Node node) => ParseMany<Or>(node);

        public override string ToString() => string.Join(" | ", (IEnumerable<IEvaluateable>)Inputs);
    }


    public sealed class Range : Operator, IIndexable, IValidateValue
    {
        private static readonly InputConstraint[] _Constraints
            = new InputConstraint[]
            {
                new InputConstraint(false, TypeFlags.IntegerNatural, TypeFlags.IntegerNatural)
            };
        InputConstraint[] IValidateValue.GetConstraints() => _Constraints;

        public readonly IEvaluateable Start;
        public readonly IEvaluateable End;

        IEvaluateable IIndexable.this[params Number[] indices] => throw new NotImplementedException();

        IEvaluateable IIndexable.MaxIndex => throw new NotImplementedException();

        IEvaluateable IIndexable.MinIndex => throw new NotImplementedException();

        protected override IEvaluateable Evaluate(IEvaluateable[] inputs, int constraintIdx)
        {
            throw new NotImplementedException();
        }

        internal override bool Parse(DynamicLinkedList<IEvaluateable>.Node node)
        {
            throw new NotImplementedException();
        }
    }


    public sealed class Subtraction : Operator, IValidateValue
    {
        private static readonly InputConstraint[] _Constraints
            = new InputConstraint[]
            {
                new InputConstraint(false, TypeFlags.NumberAny, TypeFlags.NumberAny)
            };
        InputConstraint[] IValidateValue.GetConstraints() => _Constraints;

        protected override IEvaluateable Evaluate(IEvaluateable[] inputs, int constraintIdx)
        {
            switch (constraintIdx)
            {
                case 0:
                    return ((Number)inputs[0]) - ((Number)inputs[1]);
                default:
                    return new EvaluationError(this, inputs, "Have not implemented evaluation for constraint " + constraintIdx);

            }
        }

        internal override bool Parse(DynamicLinkedList<IEvaluateable>.Node node)
        {
            if (node.Previous == null || node.Previous.Contents is Operator)
            {
                Negation n = new Negation();
                node.Contents = n;
                return n.Parse(node);
            }
            return ParseBinary(node);
        }

        public override string ToString() => string.Join(" - ", (IEnumerable<IEvaluateable>)Inputs);
    }

}
