using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dependency.TypeControl;

namespace Dependency
{
    
    public interface IEvaluateable
    {
        /// <summary>Updates and returns the new value of this <see cref="IEvaluateable"/>.</summary>
        IEvaluateable Value { get; }

        IEvaluateable UpdateValue();

        
    }

    internal interface ILiteral<TClr> : IEvaluateable
    {
        TClr CLRValue { get; }
    }

    internal interface ITypeFlag
    {
        TypeFlags Flags { get; }
    }

    public interface IContext
    {
        bool TryGetSubcontext(string token, out IContext ctxt);
        bool TryGetVariable(string token, out IVariable var);
        bool TryGetConstant(string token, out IEvaluateable k);

        IContext Parent { get; }
    }

    internal interface IRangeable : IContext
    {
        bool TryGetImmobile(string token, out Parse.Reference r);
    }

    internal interface IValidateValue { TypeConstraint[] GetConstraints(); }



    public interface IVariable : IEvaluateable
    {
        IContext Context { get; }

        IEvaluateable Contents { get; }

        string Name { get; }
    }

    internal interface IIndexable : IEvaluateable
    {
        IEvaluateable MaxIndex { get; }
        IEvaluateable MinIndex { get; }
        IEvaluateable this[params Number[] indices] { get; }

    }
    
}
