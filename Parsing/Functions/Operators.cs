using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataStructures;
using System.Diagnostics;
using static Dependency.TypeControl;

namespace Dependency
{
    /// <summary>Operators cannot come consecutively when an expression is read left-to-right(with limited exception 
    /// for negations).</summary>
    internal interface IOperator { }

    internal abstract class ComparisonOperator : Function, IOperator, IValidateValue
    {
        protected static readonly TypeConstraint[] Constraints = new TypeConstraint[] { TypeConstraint.Nonvariadic(TypeFlags.NumberAny, TypeFlags.NumberAny) };

        TypeConstraint[] IValidateValue.GetConstraints() => Constraints;

        protected abstract bool Compare(Number a, Number b);

        protected sealed override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
            => Compare((Number)evaluatedInputs[0], (Number)evaluatedInputs[1]) ? Dependency.Boolean.True : Dependency.Boolean.False;

    }

    public sealed class Addition : Function, IOperator, IValidateValue
    {
        private static readonly TypeConstraint[] _Constraints
            = new TypeConstraint[]
            {
                TypeConstraint.Variadic(TypeFlags.NumberAny, TypeFlags.NumberAny)
            };
        TypeConstraint[] IValidateValue.GetConstraints() => _Constraints;

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

        public override string ToString() => string.Join(" + ", (IEnumerable<IEvaluateable>)Inputs);
    }

    public sealed class And : Function, IOperator, IValidateValue
    {
        private static readonly TypeConstraint[] _Constraints
            = new TypeConstraint[]
            {
                TypeConstraint.Variadic(TypeFlags.Boolean)
            };
        TypeConstraint[] IValidateValue.GetConstraints() => _Constraints;

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

        public override string ToString() => string.Join(" & ", (IEnumerable<IEvaluateable>)Inputs);
    }

    public sealed class Division : Function, IOperator, IValidateValue
    {
        private static readonly TypeConstraint[] _Constraints
            = new TypeConstraint[]
            {
                TypeConstraint.Nonvariadic(TypeFlags.NumberAny, TypeFlags.Number | TypeFlags.Positive | TypeFlags.Negative)
            };
        TypeConstraint[] IValidateValue.GetConstraints() => _Constraints;

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

        public override string ToString() => string.Join(" / ", (IEnumerable<IEvaluateable>)Inputs);
    }

    public sealed class Equality : Function
    {
        protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
        {
            IEvaluateable iev = evaluatedInputs[0];
            for (int i = 1; i < evaluatedInputs.Length; i++) if (iev != evaluatedInputs[i]) return Boolean.False;
            return Boolean.True;
        }
    }

    public sealed class Evaluation : Function, IOperator, IValidateValue
    {
        private static readonly TypeConstraint[] _Constraints = new TypeConstraint[] { TypeConstraint.Nonvariadic(TypeFlags.Any) };
        TypeConstraint[] IValidateValue.GetConstraints() => _Constraints;

        protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex) => evaluatedInputs[0].Value;    // One more layer of evaluation

    }

    public sealed class Exponentiation : Function, IOperator, IValidateValue
    {
        private static readonly TypeConstraint[] _Constraints
            = new TypeConstraint[]
            {
                TypeConstraint.Variadic(TypeFlags.NumberAny, TypeFlags.NumberAny),
                TypeConstraint.Variadic(TypeFlags.Boolean, TypeFlags.Boolean)
            };
        TypeConstraint[] IValidateValue.GetConstraints() => _Constraints;

        protected override IEvaluateable Evaluate(IEvaluateable[] inputs, int constraintIndex)
        {
            switch (constraintIndex)
            {
                case 0:
                    double d = (double)((Number)inputs[0]).Value;
                    for (int i = 1; i < inputs.Length; i++)
                        d = Math.Pow(d, (double)((Number)inputs[i]).Value);
                    return new Number(d);
                case 1:
                    bool val = (Dependency.Boolean)inputs[0];
                    for (int i = 1; i < inputs.Length; i++)
                        val ^= (Dependency.Boolean)inputs[i];
                    return val ? Dependency.Boolean.True : Dependency.Boolean.False;
                default:
                    return new EvaluationError(this, inputs, "Have not implemented evaluation for constraint " + constraintIndex);
            }
        }
        public override string ToString() => string.Join(" ^ ", (IEnumerable<IEvaluateable>)Inputs);
    }

    internal sealed class GreaterThan : ComparisonOperator
    {
        protected override bool Compare(Number a, Number b) => a > b;
    }
    internal sealed class GreaterThanOrEquals : ComparisonOperator
    {
        protected override bool Compare(Number a, Number b) => a >= b;
    }

    public sealed class Indexing : Function, IOperator, Parse.IExpression, IValidateValue
    {
        //This is unique: an operator that is also a left-to-right expression.

        private static readonly TypeConstraint[] _Constraints
            = new TypeConstraint[]
            {
                TypeConstraint.Nonvariadic(TypeFlags.Indexable, TypeFlags.Vector | TypeFlags.Number | TypeFlags.Positive | TypeFlags.Zero)
            };
        TypeConstraint[] IValidateValue.GetConstraints() => _Constraints;

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
                            return new TypeMismatchError(this, inputs.Skip(1).ToArray(), 0, 1, _Constraints, "Indexable ordinal must evaluate to a number or vector of numbers.");
                    }
                    return idxable[ns];
                default:
                    return new EvaluationError(this, inputs, "Have not implemented evaluation for constraint " + constraintIndex);
            }
        }


        public override string ToString() => Base.ToString() + "{" + Ordinal.ToString() + "}";
    }

    internal sealed class LessThan : ComparisonOperator
    {
        protected override bool Compare(Number a, Number b) => a < b;
    }
    internal sealed class LessThanOrEquals : ComparisonOperator
    {
        protected override bool Compare(Number a, Number b) => a <= b;
    }

    public sealed class Multiplication : Function, IOperator, IValidateValue
    {
        private static readonly TypeConstraint[] _Constraints
            = new TypeConstraint[]
            {
                TypeConstraint.Variadic(TypeFlags.NumberAny, TypeFlags.NumberAny)
            };
        TypeConstraint[] IValidateValue.GetConstraints() => _Constraints;

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


        public override string ToString() => string.Join(" * ", (IEnumerable<IEvaluateable>)Inputs);
    }

    public sealed class Negation : Function, IOperator, IValidateValue
    {
        private static readonly TypeConstraint[] _Constraints
            = new TypeConstraint[]
            {
                TypeConstraint.Nonvariadic(TypeFlags.NumberAny)
            };
        TypeConstraint[] IValidateValue.GetConstraints() => _Constraints;
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


        public override string ToString() => "-" + Inputs[0].ToString();
    }

    public sealed class NotEquality : Function
    {
        protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
        {
            IEvaluateable iev = evaluatedInputs[0];
            for (int i = 1; i < evaluatedInputs.Length; i++) if (iev != evaluatedInputs[i]) return Boolean.False;
            return Boolean.True;
        }
    }

    public sealed class Or : Function, IOperator, IValidateValue
    {
        private static readonly TypeConstraint[] _Constraints
            = new TypeConstraint[]
            {
                TypeConstraint.Variadic(TypeFlags.Boolean)
            };
        TypeConstraint[] IValidateValue.GetConstraints() => _Constraints;
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

        public override string ToString() => string.Join(" | ", (IEnumerable<IEvaluateable>)Inputs);
    }

    public sealed class Range : Function, IOperator, IIndexable, IValidateValue
    {
        private static readonly TypeConstraint[] _Constraints
            = new TypeConstraint[]
            {
                TypeConstraint.Nonvariadic(TypeFlags.IntegerAny, TypeFlags.IntegerAny)
            };
        TypeConstraint[] IValidateValue.GetConstraints() => _Constraints;

        public readonly IEvaluateable Start;
        public readonly IEvaluateable End;

        IEvaluateable IIndexable.this[params Number[] indices] => throw new NotImplementedException();

        IEvaluateable IIndexable.MaxIndex => throw new NotImplementedException();

        IEvaluateable IIndexable.MinIndex => throw new NotImplementedException();

        protected override IEvaluateable Evaluate(IEvaluateable[] inputs, int constraintIdx)
        {
            throw new NotImplementedException();
        }

    }

    public sealed class Subtraction : Function, IOperator, IValidateValue
    {
        private static readonly TypeConstraint[] _Constraints
            = new TypeConstraint[]
            {
                TypeConstraint.Nonvariadic(TypeFlags.NumberAny, TypeFlags.NumberAny)
            };
        TypeConstraint[] IValidateValue.GetConstraints() => _Constraints;

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


        public override string ToString() => string.Join(" - ", (IEnumerable<IEvaluateable>)Inputs);
    }

    public sealed class Ternary : Function, IOperator, IValidateValue
    {
        private static readonly TypeConstraint[] _Constraints = new TypeConstraint[]
        {
            TypeConstraint.Nonvariadic(TypeFlags.Boolean, TypeFlags.Any, TypeFlags.Any)
        };
        TypeConstraint[] IValidateValue.GetConstraints() => _Constraints;

        protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
        {
            if (evaluatedInputs[0] is Dependency.Boolean b && b.Value) return evaluatedInputs[1];
            return evaluatedInputs[2];
        }
    }
}
