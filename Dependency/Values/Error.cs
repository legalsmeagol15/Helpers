using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using static Dependency.TypeControl;

namespace Dependency
{
#pragma warning disable CS0659
    /// <summary>The base error class.  Only maintains a message.</summary>
    /// <remarks>This is serializable because it is a valid value for an expression at any time.</remarks>
    [Serializable]
    public abstract class Error : ILiteral, ITypeGuarantee
    {
        public string Message { get; protected set; }
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

    [Serializable]
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

    [Serializable]
    public sealed class InvalidValueError : Error
    {
        public InvalidValueError(string message = null) : base(message ?? "An invalid value was created.") { }
    }

    [Serializable]
    public sealed class NotAContextError : Error
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

    [Serializable]
    public sealed class NotAVariableError : Error
    {
        public readonly object Origin;
        public readonly string[] Steps;
        internal NotAVariableError(object origin, string[] steps) : base("Give path references a non-variable dynamic object.") { this.Origin = origin; this.Steps = steps; }
        public override bool Equals(object obj) => obj is NotAVariableError other && NotAContextError.StepsEqual(Origin, Steps, other.Origin, other.Steps);
    }

    [Serializable]
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

    [Serializable]
    public sealed class IndexingError : ReferenceError
    {
        public IndexingError(IReference complainant, string message) : base(complainant, message) { }
        public override bool Equals(object obj)
            => obj is IndexingError && base.Equals(obj);
    }

    [Serializable]
    public class EvaluationError : Error
    {
        public object Complainant { get; protected set; }
        public IList<IEvaluateable> Inputs { get; protected set; }

        protected EvaluationError() : base("For serialization purposes only") { }
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

    [Serializable]
    public sealed class ConversionError : Error
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

    [Serializable]
    public sealed class TypeMismatchError : EvaluationError, ISerializationEquality, System.Runtime.Serialization.ISerializable
    {

        public readonly int InputIndex;
        public readonly Constraint BestMatch;
        internal readonly TypeFlags GivenFlags;

        /// <summary>
        /// Represents an error which occurred when a function could not match the type of the evaluated inputs to its 
        /// requirements.
        /// </summary>
        /// <param name="complainant">The function that failed to evaluate.</param>
        /// <param name="inputs">The inputs that the function failed to evaluate.</param>
        /// <param name="bestMatch">The closes matching constraint.</param>
        /// <param name="unmatchedIndex">The 0-based index of the first input whose type did not match requirements.</param>
        internal TypeMismatchError(object complainant, IList<IEvaluateable> inputs, Constraint bestMatch, int unmatchedIndex)
            : base(complainant, inputs, ComposeMessage(unmatchedIndex, TypeControl.TypeObject(inputs[unmatchedIndex]), bestMatch))
        {
            this.BestMatch = bestMatch;
            this.InputIndex = unmatchedIndex;
            this.GivenFlags = (inputs[unmatchedIndex] is ITypeGuarantee itf) ? itf.TypeGuarantee : TypeFlags.Any;
        }
        private static string ComposeMessage(int unmatchedIndex, TypeFlags existing, Constraint constraint)
        {
            if (constraint == null) return "Failed to match arguments to any constraint.";
            return "Failed to match argument " + unmatchedIndex + " (" + existing.ToString() + ") to expected " + constraint[unmatchedIndex].ToString();
        }
        public override bool Equals(object obj)
            => obj is TypeMismatchError other
            && Message.Equals(other.Message)
            && ReferenceEquals(Complainant, other.Complainant)
            && ListsEqual(Inputs, other.Inputs)
            && ReferenceEquals(BestMatch, other.BestMatch)
            && InputIndex.Equals(other.InputIndex)
            && GivenFlags == other.GivenFlags;

        /// <summary>
        /// This must serialize if we hope to store the non-serializable Constraint.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(InputIndex), InputIndex);
            info.AddValue(nameof(GivenFlags), GivenFlags);
            info.AddValue(nameof(Complainant), Complainant);    // Is this also stored by the base class?
            info.AddValue(nameof(Inputs), Inputs);

            TypeControl tc = TypeControl.GetConstraints(Complainant.GetType());
            int constraintIdx = tc.Constraints.IndexOf(BestMatch);
            if (constraintIdx < 0)
                throw new SerializationException("Cannot identify constraint for type " + Complainant.GetType().Name);
            info.AddValue(nameof(BestMatch), constraintIdx);
        }

        private TypeMismatchError(SerializationInfo info, StreamingContext context) 
        {
            this.InputIndex = info.GetInt32(nameof(InputIndex));
            this.GivenFlags = (TypeFlags)info.GetValue(nameof(GivenFlags), typeof(TypeFlags));
            this.Complainant = info.GetValue(nameof(Complainant), typeof(object));
            this.Inputs = (IList<IEvaluateable>)info.GetValue(nameof(Inputs), typeof(IList<IEvaluateable>));
            int constraintIdx = info.GetInt32(nameof(BestMatch));
            TypeControl tc = TypeControl.GetConstraints(Complainant.GetType());  // Will this be null?  Yes
            this.BestMatch = tc.Constraints[constraintIdx];
            this.Message = ComposeMessage(InputIndex, TypeControl.TypeObject(this.Inputs[this.InputIndex]) , this.BestMatch);
        }
    }

    [Serializable]
    public sealed class RangeError : Error
    {
        public readonly Dependency.Values.Range Range;
        public RangeError(Dependency.Values.Range complainant, string message) : base(message) { this.Range = complainant; }
    }
#pragma warning restore CS0659

}
