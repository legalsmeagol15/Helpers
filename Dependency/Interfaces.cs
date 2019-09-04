using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dependency.Functions;
using Dependency.Variables;
using Helpers;
using System.Threading;

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

    /// <summary>
    /// An <see cref="IVariable"/> can have multiple listeners, and its content evaluation tree can listen to multiple 
    /// <see cref="IVariable"/>s in turns.
    /// </summary>
    internal interface IVariable : IEvaluateable
    {
        /// <summary>Remove record of a listener to this <see cref="IVariable"/>.</summary>
        /// <returns>True if the listener set was changed; if the listener never existed there to begin with, returns 
        /// false.</returns>
        bool RemoveListener(IDynamicItem r);

        /// <summary>Add record of a listener to this <see cref="IVariable"/>.  When the <see cref="IVariable"/> is 
        /// updated, the listeners should then be updated (ideally asynchronously).</summary>
        /// <returns>True if the listener set was changed; if the listener already existed there, returns false.
        /// </returns>
        bool AddListener(IDynamicItem r);

        /// <summary>Pointers to the things this <see cref="IVariable"/> listens to.</summary>
        ISet<Reference> References { get; set; }

        IEnumerable<IDynamicItem> GetListeners();

        /// <summary>The content evaluation tree of this <see cref="IVariable"/>.</summary>
        IEvaluateable Contents { get; set; }

        void SetValue(IEvaluateable value);
        
        /// <summary>Fired when the <see cref="IVariable"/>'s cached value changes.</summary>
        event ValueChangedHandler<IEvaluateable> ValueChanged;

        void FireValueChanged(IEvaluateable oldValue, IEvaluateable newValue);
    }
    internal interface IVariableAsync : IVariable
    {
        ReaderWriterLockSlim ValueLock { get; }

        
    }


    internal interface IWeakVariable<T>
    {
        Variable Variable { get; }
        void SetLock(bool locked);
        T Value { get; }
        bool TryGetVariable(out Variable v);
    }





}
