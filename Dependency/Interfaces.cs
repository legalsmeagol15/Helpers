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
    
    /// <summary>An <seealso cref="IEvaluateable"/> object that will pass changes in value up through an evaluation 
    /// tree to its parent.</summary>
    internal interface IDynamicItem : IEvaluateable
    {
        /// <summary>
        /// The parent <seealso cref="Reference"/>, <seealso cref="Function"/>, or other <see cref="IDynamicItem"/> 
        /// that will be evaluated upon changes in this object's dependency value.
        /// </summary>
        IDynamicItem Parent { get; set; }

        /// <summary>
        /// Compel the <seealso cref="IDynamicItem"/> to update its stored value.  If a <paramref name="forcedValue"/> 
        /// is given, the new value cached should be equal to that.
        /// </summary>
        /// <param name="forcedValue">Optional.  The new value that should be assigned to this 
        /// <seealso cref="IEvaluateable"/>.  If omitted or null, the <seealso cref="IDynamicItem"/> should calculate 
        /// its new value according to its own logic.</param>
        /// <returns>Returns true if the update changed the value of this <seealso cref="IDynamicItem"/>; otherwise, 
        /// returns false.</returns>
        bool Update(IEvaluateable forcedValue = null);
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

    internal interface IFunction : IDynamicItem
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
    internal interface IVariable : IEvaluateable, IDynamicItem
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
        
        /// <summary>Sets the contents of the <see cref="IVariable"/>.</summary>
        void SetContents(IEvaluateable newContents);
        
        /// <summary>Fired when the <see cref="IVariable"/>'s cached value changes.</summary>
        event ValueChangedHandler<IEvaluateable> ValueChanged;

        void FireValueChanged(IEvaluateable oldValue, IEvaluateable newValue);
    }


    internal interface IWeakVariable<T>
    {
        /// <summary>Creates or retrieves the existing associated <seealso cref="Variable"/>.</summary>
        Variable Source { get; }
        void SetLock(bool locked);
        /// <summary>The CLR value associated with this variable.</summary>
        T Value { get; }
        /// <summary>Returns true if an existing <seealso cref="Variable"/> can be retrieved; otherwise, returns false.
        /// </summary>
        bool TryGetSource(out Variable v);
    }





}
