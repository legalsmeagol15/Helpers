using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dependency.TypeControl;

namespace Dependency
{
#pragma warning disable CS0659
    /// <summary>The base error class.  Only maintains a message.</summary>
    public abstract class Error : ILiteral, ITypeGuarantee
    {
        public readonly string Message;
        protected Error(string message) { this.Message = message; }
        IEvaluateable IEvaluateable.Value => this;
        TypeFlags ITypeGuarantee.TypeGuarantee => TypeFlags.Error;
        public override bool Equals(object obj) => obj is Error other && Message.Equals(other.Message);
        //public sealed override int GetHashCode() => Message.GetHashCode();
        public sealed override int GetHashCode() => throw new InvalidOperationException("Errors should not be hashed.");
        protected static bool ListsEqual<T>(IList<T> a, IList<T> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++) if (!a[i].Equals(b[i])) return false;
            return true;
        }
        public override string ToString() => this.GetType().Name + "(" + Message + ")";
    }

    public sealed class CircularityError : Error
    {
        internal readonly IEvaluateable Origin;
        internal readonly IEnumerable<IEvaluateable> Path;
        internal CircularityError(ISyncUpdater origin, IEnumerable<IEvaluateable> path) : base("Circular reference.") { this.Origin = origin; this.Path = path; }
        internal CircularityError(IVariable origin, IEnumerable<IEvaluateable> path) : base("Circular reference from variable.") { this.Origin = origin; this.Path = path; }        
        public override bool Equals(object obj)
        {
            if (!(obj is CircularityError other)) return false;
            if (!Origin.Equals(other.Origin)) return false;
            return Mathematics.Set.IterateEquals(Path, other.Path);            
        }
    }
    public sealed class InvalidValueError : Error
    {
        public InvalidValueError(string message = null) : base(message ?? "An invalid value was created.") { }
    }

    public class NotAContextError : Error
    {
        public readonly object Origin;
        public readonly string[] Steps;
        internal NotAContextError(object origin, string[] steps) : base("Given origin is not a context.") { this.Origin = origin; this.Steps = steps; }
        public override bool Equals(object obj) => obj is NotAContextError other && StepsEqual(other.Origin, other.Steps, Origin, Steps);
        internal static bool StepsEqual(object originA, string[] stepsA, object originB, string[] stepsB)
        {
            if (originA == null)
            {
                if (originB != null) return false;
            }
            else if (!originA.Equals(originB)) return false;
            else if ((stepsA == null) ^ (stepsB == null)) return false;
            else if (stepsA == null) return true;
            else if (stepsA.Length != stepsB.Length) return false;
            for (int i = 0; i < stepsA.Length; i++)
                if (stepsA[i] != stepsB[i]) return false;
            return true;
        }
    }
    public class NotAVariableError : Error
    {
        public readonly object Origin;
        public readonly string[] Steps;
        internal NotAVariableError(object origin, string[] steps) : base("Give path references a non-variable dynamic object.") { this.Origin = origin; this.Steps = steps; }
        public override bool Equals(object obj) => obj is NotAVariableError other && NotAContextError.StepsEqual(Origin, Steps, other.Origin, other.Steps);
    }
    public class ReferenceError : Error
    {
        public readonly IReference Complainant;
        internal ReferenceError(IReference complainant, string message) : base(message) { this.Complainant = complainant; }
        public override bool Equals(object obj)
        {
            if (!(obj is ReferenceError re)) return false;
            if (!Complainant.Equals(re.Complainant)) return false;
            if (Message != re.Message) return false;
            return true;
        }
    }

    public sealed class IndexingError : ReferenceError
    {
        public IndexingError(IReference complainant, string message) : base(complainant, message) { }
        public override bool Equals(object obj)
            => obj is IndexingError && base.Equals(obj);
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
    }

    public class ConversionError : Error
    {
        public ConversionError(IEvaluateable from, Type @to, string message = null) 
            : base(message ?? "Cannot convertion " + from.ToString() + " to " + to.Name) { }
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

    }

    public sealed class TypeMismatchError : EvaluationError
    {

        public readonly int InputIndex;
        public readonly Constraint BestMatch;
        internal readonly TypeControl TypeControl;
        internal readonly TypeFlags GivenFlags;

        /// <summary>
        /// Represents an error which occurred when a function could not match the type of the evaluated inputs to its 
        /// requirements.
        /// </summary>
        /// <param name="complainant">The function that failed to evaluate.</param>
        /// <param name="inputs">The inputs that the function failed to evaluate.</param>
        /// <param name="bestMatch">The closes matching constraint.</param>
        /// <param name="inputIndex">The 0-based index of the first input whose type did not match requirements.</param>
        /// <param name="typeControl">The constraint set used to evaluate the given inputs.</param>
        /// <param name="message">The message.</param>
        internal TypeMismatchError(object complainant, IList<IEvaluateable> inputs, Constraint bestMatch, int inputIndex, TypeControl typeControl, string message = null)
            : base(complainant, inputs, bestMatch == null ? "Failed to match arguments to any constraint."
                                             : "Failed to match argument " + inputIndex + " (" + TypeControl.TypeObject(inputs[inputIndex]).ToString() + ") to constraint expecting " + bestMatch[inputIndex].ToString())
        {
            this.BestMatch = bestMatch;
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
            && ReferenceEquals(BestMatch, other.BestMatch)
            && InputIndex.Equals(other.InputIndex)
            && GivenFlags == other.GivenFlags;
    }

    public sealed class RangeError : Error
    {
        public readonly Dependency.Values.Range Range;
        public RangeError(Dependency.Values.Range complainant, string message) : base(message ) { this.Range = complainant; }
    }
#pragma warning restore CS0659

}
