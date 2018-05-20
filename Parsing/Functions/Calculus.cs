using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Graphs;

namespace Parsing.Functions
{
    internal sealed class Derivative : Function
    {
        public readonly Variable Variable;
        
        protected override IEvaluatable[] EvaluateInputs() => Inputs.ToArray();

        protected override IEvaluatable GetDerivative(Variable v) => Function.Differentiate(Evaluate(Inputs.ToArray()), v);

        protected internal override IEvaluatable Evaluate(params IEvaluatable[] inputs)
        {
            if (inputs.Length != 1) return InputCountError(inputs, 1);            
            return Function.Differentiate(inputs[0], Variable);
        }

        private class Node
        {
            public readonly Clause Parent;
            public readonly Clause Function;
            public readonly List<IEvaluatable> Children = new List<IEvaluatable>();
            public Node(Clause parent, Clause function) { this.Parent = parent; this.Function = function; }
        }

        
    }

}
