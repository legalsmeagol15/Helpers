using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dependency.Functions;
using Dependency.Variables;
using Helpers;

namespace Dependency
{


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


    /// <summary>
    /// <para/>The subcontext or property returned for a given object should always be the same.
    /// </summary>
    public interface IContext
    {
        bool TryGetSubcontext(object path, out IContext ctxt);

        bool TryGetProperty(object path, out IEvaluateable source);
    }


    internal interface IDynamicItem
    {
        IDynamicItem Parent { get; set; }

        /// <summary>
        /// Updates the value of this item.  Note that this method should NOT call a parent's update method in  any 
        /// case exception for the <seealso cref="Reference"/>, which is the update driver.
        /// </summary>
        /// <returns>Returns true if the value changed; otherwise, returns false.</returns>
        bool Update();
    }


    public interface IEvaluateable
    {
        /// <summary>Returns the most recently-updated value of this <see cref="IEvaluateable"/>.</summary>
        IEvaluateable Value { get; }
        
    }


    /// <summary>Readable left-to-right.</summary>
    internal interface IExpression : IEvaluateable
    {
        IEvaluateable Contents { get; }
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
        IEvaluateable this[IEvaluateable index] { get; }
    }


    public interface ILiteral : IEvaluateable { }
    internal interface ILiteral<TClr> : ILiteral
    {
        TClr CLRValue { get; }
    }

    public interface INamed
    {
        string Name { get; }
    }


    public interface ISubcontext : IContext
    {
        IContext Parent { get; set; }
    }


    internal interface ITypeGuarantee
    {
        TypeFlags TypeGuarantee { get; }
    }


    internal interface IVariable
    {
        bool RemoveListener(Functions.Reference r);
        bool AddListener(Functions.Reference r);
        IEnumerable<Functions.Reference> GetReferences();
        IEvaluateable Contents { get; set; }
        event ValueChangedHandler<IEvaluateable> ValueChanged;
    }


    internal interface IWeakVariable<T>
    {
        Variable Variable { get; }
        void SetLock(bool locked);
        T Value { get; }
        bool TryGetVariable(out Variable v);
    }





}
