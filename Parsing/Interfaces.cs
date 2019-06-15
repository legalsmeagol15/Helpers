using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dependency.Functions;

namespace Dependency
{
    public interface IAllowReferenceMobility
    {
        bool TryExtract(string token, out string stripped, out Mobility mobility);
    }

    /// <summary>
    /// A function which caches its <seealso cref="TypeControl"/> input validator may validate just a little faster 
    /// than a function which must retrieve the validator from the cached catalogue.
    /// </summary>
    public interface ICacheValidator
    {
        TypeControl TypeControl { get; set; }
    }

    public interface ICategorized
    {
        IList<string> Categories { get; }
    }



    public interface IContext
    {
        bool TryGetSubcontext(string token, out IContext ctxt);
        bool TryGetSource(string token, out ISource source, out Mobility mobility);

        IContext Parent { get; }
    }

    public interface IEvaluateable
    {
        /// <summary>Updates and returns the new value of this <see cref="IEvaluateable"/>.</summary>
        IEvaluateable Value { get; }

        IEvaluateable UpdateValue();


    }

    public interface IFunction : IEvaluateable
    {
        IList<IEvaluateable> Inputs { get; }
    }


    /// <summary>Creates <see cref="NamedFunction"/> objects based on the given string name.</summary>
    public interface IFunctionFactory
    {
        /// <summary>
        /// Tries to create a <see cref="NamedFunction"/> based on the given token, from the catalogue available to 
        /// the <see cref="IFunctionFactory"/>.
        /// </summary>
        /// <returns>Returns true in the case of success, false if not.  If true, the given parameter 
        /// <paramref name="nf"/> will contain the <see cref="NamedFunction"/>; otherwise, the parameter will be 
        /// null.</returns>
        bool TryCreate(string token, out NamedFunction nf);
    }

    internal interface IIndexable : IEvaluateable
    {
        IEvaluateable MaxIndex { get; }
        IEvaluateable MinIndex { get; }
        IEvaluateable this[params Number[] indices] { get; }
    }

    public interface IListener
    {
        IEnumerable<ISource> Sources { get; }

    }
    public interface ICLRListener<T> : IListener
    {

        Action<T> UpdateValue { get; }
    }

    internal interface ILiteral<TClr> : IEvaluateable
    {
        TClr CLRValue { get; }
    }

    public interface INamed
    {
        string Name { get; }
    }

    internal interface ITerms : IEvaluateable
    {
        IEnumerable<ISource> GetTerms();
    }



    internal interface ITypeGuarantee
    {
        TypeFlags TypeGuarantee { get; }
    }




    public interface ISource : IEvaluateable
    {
        IEnumerable<IListener> Listeners { get; }

        IEvaluateable Contents { get; set; }
    }


    public interface IVariable : IEvaluateable, ISource, IListener, INamed
    {
        IContext Context { get; }

    }


}
