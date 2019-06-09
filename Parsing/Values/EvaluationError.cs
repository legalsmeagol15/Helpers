using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dependency.TypeControl;

namespace Dependency
{
    [Serializable]
    public class EvaluationError : IEvaluateable, ITypeFlag
    {
        public readonly string Message;
        public readonly object Complainant;
        public readonly IEnumerable<IEvaluateable> Inputs;
        public readonly int Start;
        public readonly int End;

        public EvaluationError(object complainant, IEnumerable<IEvaluateable> inputs, string message = "Failed to evaluate inputs", int startIdx = -1, int endIdx = -1)
        {
            this.Message = message;
            this.Start = startIdx;
            this.End = endIdx;
            this.Complainant = complainant;
            this.Inputs = inputs;
        }

        IEvaluateable IEvaluateable.Value => this;
        IEvaluateable IEvaluateable.UpdateValue() => this;
        TypeFlags ITypeFlag.Flags => TypeFlags.Error;
    }

    public class InputCountError : EvaluationError
    {
        internal IEnumerable<TypeConstraint> Constraints;

        internal InputCountError(object complainant, IList<IEvaluateable> inputs, IEnumerable<TypeConstraint> constraints)
            : base(complainant, inputs, "Incorrect number of inputs (" + inputs.Count + ")")
        {
            this.Constraints = constraints;
        }
    }

    public class TypeMismatchError : EvaluationError
    {

        public readonly int InputIndex;
        public readonly int ConstraintIndex;
        internal IEnumerable<TypeConstraint> Constraints;
        internal readonly TypeFlags GivenFlags;

        /// <summary>
        /// Represents an error which occurred when a function could not match the type of the evaluated inputs to its 
        /// requirements.
        /// </summary>
        /// <param name="complainant">The function that failed to evaluate.</param>
        /// <param name="inputs">The inputs that the function failed to evaluate.</param>
        /// <param name="constraintIdx">The 0-based constraint index that represented the best fit (if one existed with that allowed the given number of inputs).</param>
        /// <param name="inputIndex">The 0-based index of the first input whose type did not match requirements.</param>
        /// <param name="constraints">The constraint set used to evaluate the given inputs.</param>
        /// <param name="message">The message.</param>
        internal TypeMismatchError(object complainant, IList<IEvaluateable> inputs, int constraintIdx, int inputIndex, TypeConstraint[] constraints, string message = null)
            : base(complainant, inputs, message)
        {
            this.ConstraintIndex = constraintIdx;
            this.InputIndex = inputIndex;
            this.Constraints = constraints;
            this.GivenFlags = (inputs[inputIndex] is ITypeFlag itf) ? itf.Flags : TypeFlags.Any;
        }        
    }

    

    public sealed class IndexingError : EvaluationError
    {
        public IndexingError(object complainant, IEnumerable<IEvaluateable> inputs, string msg) : base(complainant, inputs, msg) { }

    }
}
