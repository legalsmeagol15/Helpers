using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency
{
    public abstract class Function : IExpression
    {
        private IEvaluateable _Value = null;
        public IEvaluateable Value => _Value ?? Evaluate();

        public IEvaluateable Evaluate() => _Value = Evaluate(Contents.Select(c => c.Evaluate()));

        protected abstract IEvaluateable Evaluate(IEnumerable<IEvaluateable> inputs);

        public IEvaluateable[] Contents { get; internal set; }
    }

    public abstract class NamedFunction : Function
    {
        public string Name { get; }
    }
}
