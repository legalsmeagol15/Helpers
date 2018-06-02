using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Graphs;
using static Parsing.DataContext;

namespace Parsing.Functions
{
    internal sealed class Derivative : Function
    {
        //public readonly DataContext.Variable Variable;
        
        protected override IEvaluateable GetDerivative(DataContext.Variable v) => Function.Differentiate(Evaluate(Inputs.ToArray()), v);

        public override IEvaluateable Evaluate()
        {
            throw new NotImplementedException();
        }

        public override IEvaluateable Evaluate(params IEvaluateable[] inputs)
        {
            throw new NotImplementedException();
        }


        private class Node
        {
            public readonly Clause Parent;
            public readonly Clause Function;
            public readonly List<IEvaluateable> Children = new List<IEvaluateable>();
            public Node(Clause parent, Clause function) { this.Parent = parent; this.Function = function; }
        }

        
    }

}
