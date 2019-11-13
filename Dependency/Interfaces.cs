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
    /// An <see cref="IAsyncUpdater"/> can have multiple listeners, and its content evaluation tree can listen to multiple 
    /// <see cref="IAsyncUpdater"/>s in turns.
    /// </summary>
    internal interface IAsyncUpdater : IEvaluateable
    {
        /// <summary>Remove record of a listener to this <see cref="IAsyncUpdater"/>.</summary>
        /// <returns>True if the listener set was changed; if the listener never existed there to begin with, returns 
        /// false.</returns>
        bool RemoveListener(ISyncUpdater r);

        /// <summary>Add record of a listener to this <see cref="IAsyncUpdater"/>.  When the <see cref="IAsyncUpdater"/> is 
        /// updated, the listeners should then be updated (ideally asynchronously).</summary>
        /// <returns>True if the listener set was changed; if the listener already existed there, returns false.
        /// </returns>
        bool AddListener(ISyncUpdater r);

        IEnumerable<ISyncUpdater> GetListeners();
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

    

    /// <summary>
    /// <para/>The subcontext or property returned for a given object should always be the same.
    /// </summary>
    public interface IContext
    {
        bool TryGetSubcontext(object path, out IContext ctxt);

        bool TryGetProperty(object path, out IEvaluateable source);
    }

    /// <summary>
    /// Converts values to and from a CLR value of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IConverter<T>
    {
        bool CanConvert(IEvaluateable ie);
        bool TryConvert(IEvaluateable ie, out T target);
        IEvaluateable ConvertFrom(T item);
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

    internal interface IFunction : ISyncUpdater
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
        bool TryIndex(IEvaluateable ordinal, out IEvaluateable val);
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
    
    public interface INotifyUpdates<T>
    {
        /// <summary>Fired when the <see cref="IAsyncUpdater"/>'s cached value changes.</summary>
        event ValueChangedHandler<T> ValueChanged;
    }

    public interface IReference
    {
        IEnumerable<IEvaluateable> GetComposers();
    }

    public interface IRelativeString
    {
        string ToString(IContext origin);
    }
    
    public interface ISubcontext : IContext
    {
        IContext Parent { get; set; }
    }


    /// <summary>
    /// An <seealso cref="IEvaluateable"/> object that will synchronously pass changes in value up through an 
    /// evaluation tree to its parent.
    /// </summary>
    internal interface ISyncUpdater : IEvaluateable
    {
        /// <summary>
        /// The parent <seealso cref="Reference"/>, <seealso cref="Function"/>, or other <see cref="ISyncUpdater"/> 
        /// that will be evaluated upon changes in this object's dependency value.
        /// </summary>
        ISyncUpdater Parent { get; set; }

        /// <summary>Compel the <seealso cref="ISyncUpdater"/> to update its stored value.  This method is called 
        /// after the given <paramref name="updatedChild"/> has been updated with a new value.</summary>
        /// <param name="updatedChild">The child that was updated who is passing on the update to this 
        /// <seealso cref="ISyncUpdater"/>.  If null, no child was update to cause this call to 
        /// <see cref="Update(Update,ISyncUpdater)"/>.</param>
        /// <param name="caller">The <seealso cref="Dependency.Variables.Update"/> which is managing the update 
        /// procudure.</param>
        /// <returns>Returns true if the update changed the value of this <seealso cref="ISyncUpdater"/>; otherwise, 
        /// returns false.</returns>
        bool Update(Update caller, ISyncUpdater updatedChild);
    }



    internal interface ITypeGuarantee
    {
        TypeFlags TypeGuarantee { get; }
    }


    public interface IVariable  : IEvaluateable
    {
        IEvaluateable Contents { get; }
    }

    internal interface IVariableCollection : IEnumerable<IEvaluateable>
    {
        Vector Contents { get; }

    }


    internal interface IUpdatedVariable : IVariable
    {
        /// <summary>ONLY sets the contents.  Does nothing else.  This is called only within a write lock so don't lock again.</summary>
        void SetContents(IEvaluateable newContent);
        /// <summary>ONLY sets the value.  Does nothing else.  This is called only outside a write lock, so lock it.</summary>
        bool SetValue(IEvaluateable newValue);
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
