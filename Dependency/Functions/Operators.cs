using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataStructures;
using System.Diagnostics;
using static Dependency.TypeControl;
using Dependency.Functions;

namespace Dependency
{

    namespace Operators
    {
        /// <summary>Operators cannot come consecutively when an expression is read left-to-right(with limited exception 
        /// for negations).</summary>
        internal interface IOperator { }

        [NonVariadic(0, TypeFlags.RealAny, TypeFlags.RealAny)]
        [Serializable]
        internal abstract class ComparisonOperator : Function, IOperator
        {
            protected abstract bool Compare(Number a, Number b);

            protected sealed override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
                => Compare((Number)evaluatedInputs[0], (Number)evaluatedInputs[1]) ? Dependency.Boolean.True : Dependency.Boolean.False;
        }

        [Variadic(0, TypeFlags.RealAny, TypeFlags.RealAny)]
        [Serializable]
        public sealed class Addition : Function, IOperator
        {
            
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

        [Variadic(0, TypeFlags.RealAny, TypeFlags.RealAny)]
        [Serializable]
        public sealed class And : Function, IOperator
        {
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

        [NonVariadic(0, TypeFlags.RealAny, TypeFlags.RealAny)]
        [Serializable]
        public sealed class Division : Function, IOperator
        {
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

        [Serializable]
        public sealed class Equality : Function
        {
            protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
            {
                IEvaluateable iev = evaluatedInputs[0];
                for (int i = 1; i < evaluatedInputs.Length; i++) if (iev != evaluatedInputs[i]) return Boolean.False;
                return Boolean.True;
            }
        }

        [NonVariadic(0, TypeFlags.Any)]
        [Serializable]
        public sealed class Evaluation : Function, IOperator
        {
            protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex) => evaluatedInputs[0].Value;    // One more layer of evaluation
        }

        [Variadic(0, TypeFlags.Any, TypeFlags.Any)]
        [Variadic(1, TypeFlags.Boolean, TypeFlags.Boolean)]
        [Serializable]
        public sealed class Exponentiation : Function, IOperator
        {
            protected override IEvaluateable Evaluate(IEvaluateable[] inputs, int constraintIndex)
            {
                switch (constraintIndex)
                {
                    case 0:
                        double d = (double)((Number)inputs[0]).CLR_Value;
                        for (int i = 1; i < inputs.Length; i++)
                            d = Math.Pow(d, (double)((Number)inputs[i]).CLR_Value);
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

        [Serializable]
        internal sealed class GreaterThan : ComparisonOperator
        {
            protected override bool Compare(Number a, Number b) => a > b;
        }

        [Serializable]
        internal sealed class GreaterThanOrEquals : ComparisonOperator
        {
            protected override bool Compare(Number a, Number b) => a >= b;
        }

        [Serializable]
        internal sealed class LessThan : ComparisonOperator
        {
            protected override bool Compare(Number a, Number b) => a < b;
        }

        [Serializable]
        internal sealed class LessThanOrEquals : ComparisonOperator
        {
            protected override bool Compare(Number a, Number b) => a <= b;
        }

        [Variadic(0, TypeFlags.RealAny, TypeFlags.RealAny)]
        [Serializable]
        public class Multiplication : Function, IOperator
        {
            protected sealed override IEvaluateable Evaluate(IEvaluateable[] inputs, int constraintIdx)
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

        [Serializable]
        public sealed class ImpliedMultiplication  : Multiplication
        {
            public override string ToString() => string.Join("", (IEnumerable<IEvaluateable>)Inputs);
        }
        
        [NonVariadic(0, TypeFlags.RealAny)]
        [Serializable]
        public sealed class Negation : Function, IOperator
        {
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

        [Serializable]
        public sealed class NotEquality : Function
        {
            protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
            {
                IEvaluateable iev = evaluatedInputs[0];
                for (int i = 1; i < evaluatedInputs.Length; i++) if (iev != evaluatedInputs[i]) return Boolean.False;
                return Boolean.True;
            }
        }

        [Variadic(0, TypeFlags.RealAny, TypeFlags.RealAny)]
        [Serializable]
        public sealed class Or : Function, IOperator
        {
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

        //[TypeControl.NonVariadic(0, TypeFlags.Integer, TypeFlags.Integer)]
        //public sealed class Range : Function, IOperator, 3IIndexable
        //{
        //    public readonly IEvaluateable Start;
        //    public readonly IEvaluateable End;

        //    IEvaluateable IIndexable.this[params Number[] indices] => throw new NotImplementedException();

        //    IEvaluateable IIndexable.MaxIndex => throw new NotImplementedException();

        //    IEvaluateable IIndexable.MinIndex => throw new NotImplementedException();

        //    protected override IEvaluateable Evaluate(IEvaluateable[] inputs, int constraintIdx)
        //    {
        //        throw new NotImplementedException();
        //    }

        //}

        [NonVariadic(0, TypeFlags.RealAny, TypeFlags.RealAny)]
        [Serializable]
        public sealed class Subtraction : Function, IOperator
        {
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

        [NonVariadic(0, TypeFlags.Boolean, TypeFlags.Any, TypeFlags.Any)]
        [Serializable]
        public sealed class Ternary : Function, IOperator
        {
            protected override IEvaluateable Evaluate(IEvaluateable[] evaluatedInputs, int constraintIndex)
            {
                if (evaluatedInputs[0] is Dependency.Boolean b && b.Value) return evaluatedInputs[1];
                return evaluatedInputs[2];
            }
        }
    }

}
