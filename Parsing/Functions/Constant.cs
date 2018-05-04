using DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.Parsing.Functions
{

    internal sealed class Constant : Function, IEvaluatable
    {
        private readonly string _ConstantName;
        public override string Name => _ConstantName;
        public readonly Number Value;
        public Constant(string name, Number number) { this._ConstantName = name; Value = number; }
        protected internal override IEvaluatable EvaluateFunction(IList<IEvaluatable> evaluatedInputs) => throw new Exception("Sanity check.");
        IEvaluatable IEvaluatable.Evaluate() => Value;

        protected internal override IEvaluatable Parse(DynamicLinkedList<object>.Node node) => Parse(node, 0, 0);
    }


}
