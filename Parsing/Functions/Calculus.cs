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
        
        public Derivative(Variable v, Function f) : base(f) { this.Variable = v; }
        
        protected override IEvaluatable[] EvaluateInputs() => Inputs.ToArray();

        protected override IEvaluatable GetDerivative() => Function.Differentiate(Evaluate(Inputs.ToArray()));

        protected internal override IEvaluatable Evaluate(params IEvaluatable[] inputs)
        {
            if (inputs.Length != 1) InputCountError(inputs, 1);
            IEvaluatable f = inputs[0];
            return Function.Differentiate(f);
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
