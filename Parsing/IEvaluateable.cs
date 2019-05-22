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

    internal interface IIndexable : IEvaluateable
    {
        int MaxIndex { get; }
        int MinIndex { get; }
        object this[IEvaluateable index] { get; }

    }
}
