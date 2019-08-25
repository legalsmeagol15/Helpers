﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dependency.TypeControl;

namespace Dependency
{
    public abstract class Error : ILiteral, ITypeGuarantee
    {
        public readonly string Message;
        protected Error(string message) { this.Message = message; }
        IEvaluateable IEvaluateable.Value => this;
        IEvaluateable IEvaluateable.UpdateValue() => this;
        TypeFlags ITypeGuarantee.TypeGuarantee => TypeFlags.Error;
    }

    public sealed class ValueError : Error
    {
        public ValueError(string message = null) : base(message ?? "An invalid value was created.") { }
    }

    [Serializable]
    public class EvaluationError : Error
    {
        // public readonly string Message;
        public readonly object Complainant;
        public readonly IEnumerable<IEvaluateable> Inputs;
        public readonly int Start;
        public readonly int End;

        public EvaluationError(object complainant, IEnumerable<IEvaluateable> inputs, string message = null)
            : base(message ?? "Failed to evaluate inputs on " + complainant.GetType().Name + ".")
        {   
            this.Complainant = complainant;
            this.Inputs = inputs;
        }
    }

    public sealed class InputCountError : EvaluationError
    {
        internal TypeControl TypeControl;

        internal InputCountError(object complainant, IList<IEvaluateable> inputs, TypeControl typeControl)
            : base(complainant, inputs, "Incorrect number of inputs (" + inputs.Count + ")")
        {
            this.TypeControl = typeControl;
        }
    }

    public sealed class TypeMismatchError : EvaluationError
    {

        public readonly int InputIndex;
        public readonly int ConstraintIndex;
        internal readonly TypeControl TypeControl;
        internal readonly TypeFlags GivenFlags;

        /// <summary>
        /// Represents an error which occurred when a function could not match the type of the evaluated inputs to its 
        /// requirements.
        /// </summary>
        /// <param name="complainant">The function that failed to evaluate.</param>
        /// <param name="inputs">The inputs that the function failed to evaluate.</param>
        /// <param name="constraintIdx">The 0-based constraint index that represented the best fit (if one existed with that allowed the given number of inputs).</param>
        /// <param name="inputIndex">The 0-based index of the first input whose type did not match requirements.</param>
        /// <param name="typeControl">The constraint set used to evaluate the given inputs.</param>
        /// <param name="message">The message.</param>
        internal TypeMismatchError(object complainant, IList<IEvaluateable> inputs, int constraintIdx, int inputIndex, TypeControl typeControl, string message = null)
            : base(complainant, inputs, message)
        {
            this.ConstraintIndex = constraintIdx;
            this.InputIndex = inputIndex;
            this.TypeControl = typeControl;
            this.GivenFlags = (inputs[inputIndex] is ITypeGuarantee itf) ? itf.TypeGuarantee : TypeFlags.Any;
        }
    }
    
    public sealed class IndexingError : EvaluationError
    {
        public IndexingError(object complainant, IEnumerable<IEvaluateable> inputs, string msg) : base(complainant, inputs, msg) { }

    }
}
