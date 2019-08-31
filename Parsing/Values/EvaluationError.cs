using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dependency.TypeControl;

namespace Dependency
{
    /// <summary>The base error class.  Only maintains a message.</summary>
    public abstract class Error : ILiteral, ITypeGuarantee
    {
        public readonly string Message;
        protected Error(string message) { this.Message = message; }
        IEvaluateable IEvaluateable.Value => this;
        TypeFlags ITypeGuarantee.TypeGuarantee => TypeFlags.Error;
        public override bool Equals(object obj) => obj is Error other && Message.Equals(other.Message);
        public override int GetHashCode() => Message.GetHashCode();

        protected static bool ListsEqual<T>(IList<T> a, IList<T> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++) if (!a[i].Equals(b[i])) return false;
            return true;
        }
    }
    

    public sealed class InvalidValue : Error
    {
        public InvalidValue(string message = null) : base(message ?? "An invalid value was created.") { }
    }

    public class ReferenceError : EvaluationError
    {
        public readonly IContext Context;
        public ReferenceError(object complainant, IList<IEvaluateable> inputs, IContext context, string message)
            : base(complainant, inputs, message)
        {
            this.Context = context;
        }
    }

    public class EvaluationError : Error
    {
        // public readonly string Message;
        public readonly object Complainant;
        public readonly IList<IEvaluateable> Inputs;

        public EvaluationError(object complainant, IList<IEvaluateable> inputs, string message = null)
            : base(message ?? "Failed to evaluate inputs on " + complainant.GetType().Name + ".")
        {   
            this.Complainant = complainant;
            this.Inputs = inputs;
        }
        public override bool Equals(object obj) 
            => obj is EvaluationError other 
            && Message.Equals(other.Message) 
            && ReferenceEquals(Complainant, other.Complainant) 
            && ListsEqual(Inputs, other.Inputs);
        public override int GetHashCode() => Message.GetHashCode();
    }

    public sealed class InputCountError : EvaluationError
    {
        internal TypeControl TypeControl;

        internal InputCountError(object complainant, IList<IEvaluateable> inputs, TypeControl typeControl)
            : base(complainant, inputs, "Incorrect number of inputs (" + inputs.Count + ")")
        {
            this.TypeControl = typeControl;
        }
        public override bool Equals(object obj)
            => obj is InputCountError other
            && Message.Equals(other.Message)
            && ReferenceEquals(Complainant, other.Complainant)
            && ListsEqual(Inputs, other.Inputs)
            && TypeControl.Equals(other.TypeControl);
        public override int GetHashCode() => Message.GetHashCode();

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
        public override bool Equals(object obj)
            => obj is TypeMismatchError other
            && Message.Equals(other.Message)
            && ReferenceEquals(Complainant, other.Complainant)
            && ListsEqual(Inputs, other.Inputs)
            && TypeControl.Equals(other.TypeControl)
            && ConstraintIndex.Equals(other.ConstraintIndex)
            && InputIndex.Equals(other.InputIndex)
            && GivenFlags == other.GivenFlags;
        public override int GetHashCode() => Message.GetHashCode();
    }
    
    public sealed class IndexingError : EvaluationError
    {
        public IndexingError(object complainant, IList<IEvaluateable> inputs, string msg) : base(complainant, inputs, msg) { }

    }
}
