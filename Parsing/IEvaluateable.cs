using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency
{
    internal enum TypesAllowed
    {
        NonIntegerNumber = 1 << 0,
        IntegerNumber = 1 << 1,
        PositiveNumber = 1 << 2,
        NegativeNumber = 1 << 3,
        ZeroNullEmpty = 1 << 4,
        Real = NonIntegerNumber | IntegerNumber | PositiveNumber | NegativeNumber | ZeroNullEmpty,
        Imaginary = 1 << 10,
        Complex = Real | Imaginary,
        Vector = 1 << 20,
        NonEmptyString = 1 << 25,
        String = ZeroNullEmpty | NonEmptyString,
        Indexable = Vector,
        Other = 1 << 31,
            Any = ~0
    }

    public interface IEvaluateable
    {
        /// <summary>Updates and returns the new value of this <see cref="IEvaluateable"/>.</summary>
        IEvaluateable Value { get; }

        IEvaluateable UpdateValue();

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
