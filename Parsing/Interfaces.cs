using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing
{

    /// <summary>IEvaluteable objects can evaluate to another IEvaluatable value.</summary>
    public interface IEvaluateable
    {
        IEvaluateable Evaluate();
    }


    public interface IIndexable
    {
        object this[IEvaluateable index] { get; }

        int MaxIndex { get; }
        int MinIndex { get; }
    }

    public interface IEvaluateable<T> : IEvaluateable where T : IEvaluateable
    {
        new T Evaluate();
    }
}
