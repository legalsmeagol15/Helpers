using DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing.Functions
{

    internal sealed class Constant : Function, IEvaluatable
    {
        // Reflection will add the following constants to a standard factory.
        public static readonly Constant PI = new Constant("PI", Number.Pi);
        public static readonly Constant E = new Constant("E", Number.E);

        private readonly string _ConstantName;
        public override string Name => _ConstantName;
        public readonly Number Value;
        public Constant(string name, Number number) { this._ConstantName = name; Value = number; }
        protected internal override IEvaluatable Evaluate(params IEvaluatable[] inputs) => Value;
        protected internal override void ParseNode(DynamicLinkedList<object>.Node node) => ParseNode(node, 0,0);
    }


}
