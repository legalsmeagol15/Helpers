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

        IEvaluateable IEvaluateable.Evaluate() => this;
    }

    public class InputCountError : EvaluationError
    {
        public readonly int CorrectCount;

        public InputCountError(object complainant, IEnumerable<IEvaluateable> inputs, int correctCount)
            : base(complainant, inputs, "Incorrect number of inputs.  Should be " + correctCount + ".")
        {
            this.CorrectCount = correctCount;
        }
    }

    public class TypeMismatchError : EvaluationError 
    {
        public readonly int InputIndex;

        public readonly IEnumerable<Type> ValidTypes;

        public TypeMismatchError(object complainant, IEnumerable<IEvaluateable> inputs, int inputIndex, params Type[] validTypes)
            : base(complainant, inputs)
        {
            this.InputIndex = inputIndex;
            this.ValidTypes = validTypes;
        }
    }

    public sealed class IndexingError : EvaluationError
    {
        public IndexingError(object complainant, IEnumerable<IEvaluateable> inputs, string msg) : base(complainant, inputs, msg) { }

    }
}
