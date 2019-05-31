using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency
{
    public interface IEvaluateable
    {
        /// <summary>The current value of this <see cref="IEvaluateable"/>.</summary>
        IEvaluateable Value { get; }
        /// <summary>Updates and returns the new value of this <see cref="IEvaluateable"/>.</summary>
        IEvaluateable Evaluate();
    }

    public interface IContext : IEvaluateable
    {
        bool TryGetSubcontext(string token, out IContext ctxt);
        bool TryGetVariable(string token, out IVariable var);
        IContext Parent { get; }
    }

    public interface IVariable : IEvaluateable
    {
        IContext Context { get; }
    }

    internal interface IIndexable : IEvaluateable
    {
        IEvaluateable MaxIndex { get; }
        IEvaluateable MinIndex { get; }
        IEvaluateable this[params Number[] indices] { get; }

    }
}
