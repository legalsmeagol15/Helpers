using DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing.Functions
{

    internal sealed class Constant : DataContext.Function, IEvaluatable
    {
        // Reflection will add the following constants to a standard factory.
        public static readonly Constant PI = new Constant("PI", Number.Pi);
        public static readonly Constant E = new Constant("E", Number.E);

        private readonly string _ConstantName;
        public override string Name => _ConstantName;
        public readonly Number Value;
        public Constant(string name, Number number) { this._ConstantName = name; Value = number; }
        protected internal override IEvaluatable Evaluate(params IEvaluatable[] inputs) => Value;
        protected internal override void ParseNode(DynamicLinkedList<object>.Node node)
        {
            // The only "parsing" that needs to be done is to look for an optional 0-arg paren immediately following the constant.  If 
            // it's there, it should be removed.
            if (node.Next != null && node.Next.Contents is Clause c && c.Inputs.Length == 0) node.Next.Remove();
        }

        protected override IEvaluatable GetDerivative(DataContext.Variable v) => Number.Zero;
    }


}
