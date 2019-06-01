using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency
{
    [Serializable]
    public class EvaluationError : IEvaluateable
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
    }

    public class InputCountError : EvaluationError
    {
        internal IEnumerable<TypesAllowed[]> Constraints;

        internal InputCountError(object complainant, IEnumerable<IEvaluateable> inputs, IEnumerable<TypesAllowed[]> constraints)
            : base(complainant, inputs, "Incorrect number of inputs.  Should be " + String.QueensJoin(constraints.Select(c => c.Length)) + "." )
        {
            this.Constraints = constraints;
        }
    }

    public class TypeMismatchError : EvaluationError 
    {

        public readonly int InputIndex;
        public readonly int ConstraintIndex;
        internal IEnumerable<TypesAllowed[]> Constraints;

        internal TypeMismatchError(object complainant, IEnumerable<IEvaluateable> inputs, int constraintIdx, int inputIndex, IEnumerable<TypesAllowed[]> constraints)
            : base(complainant, inputs)
        {
            this.ConstraintIndex = constraintIdx;
            this.InputIndex = inputIndex;
            this.Constraints = constraints;
        }
    }

    

    public sealed class IndexingError : EvaluationError
    {
        public IndexingError(object complainant, IEnumerable<IEvaluateable> inputs, string msg) : base(complainant, inputs, msg) { }

    }
}
