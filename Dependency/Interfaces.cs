﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dependency.Functions;
using Dependency.Variables;
using Helpers;
using System.Threading;
using Mathematics;

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
        bool RemoveListener(ISyncUpdater listener);

        /// <summary>Add record of a listener to this <see cref="IAsyncUpdater"/>.  When the <see cref="IAsyncUpdater"/> is 
        /// updated, the listeners should then be updated (ideally asynchronously).</summary>
        /// <returns>True if the listener set was changed; if the listener already existed there, returns false.
        /// </returns>
        bool AddListener(ISyncUpdater listener);

        IEnumerable<ISyncUpdater> GetListeners();
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
        bool TryGetSubcontext(string path, out IContext ctxt);

        bool TryGetProperty(string path, out IEvaluateable property);
    }

    ///// <summary>Generic objects of this class are used to make another object into an IContext.
    ///// </summary>
    ///// <typeparam name="T">Must be a struct</typeparam>
    //public interface IContextualizer<T> : IContext, INotifyUpdates<T>
    //{
    //    /// <summary>When a host context value changes, it should apply the change to all sub-part 
    //    /// <seealso cref="Variable"/> objects.</summary>
    //    /// <param name="newCLR">The new CLR value of the host.</param>
    //    /// <returns>Returns true if a sub-variable's contents were updated; otherwise, returns 
    //    /// false.</returns>
    //    bool ApplyContents(T newCLR);

    //    /// <summary>When a host's sub-part changes, it should generate a new value from the sub-
    //    /// parts.</summary>
    //    IEvaluateable ComposeValue();

        

    //    /// <summary>Converts from the given object to a dependency value.  Cannot be allowed to 
    //    /// fail.</summary>
    //    IEvaluateable ConvertUp(T obj);

    //    /// <summary>
    //    /// Converts from the given dependency value to the target type <typeparamref name="T"/>.
    //    /// Not all dependency values may so convert, so it is possible for this to fail.
    //    /// </summary>
    //    bool TryConvertDown(IEvaluateable ie, out T target);
    //}

    /// <summary>
    /// Converts values to and from a CLR value of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IConverter<T>
    {
        bool CanConvertDown(IEvaluateable ie);
        bool TryConvertDown(IEvaluateable ie, out T target);
        IEvaluateable ConvertUp(T item);
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

    public interface IIndexable 
    {
        void IndexedContentsChanged(IEvaluateable index, IEvaluateable value);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ordinal">The evaluated ordinal value.</param>
        /// <param name="val"></param>
        /// <returns></returns>
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


    public interface IReference : IEvaluateable
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

        /// <summary>Compel the <seealso cref="ISyncUpdater"/> to update its stored value.  This 
        /// method is called after the given <paramref name="updatedChild"/> has been updated with 
        /// a new value for the given <paramref name="indexDomain"/>.</summary>
        /// <param name="updatedChild">The child that was updated who is passing on the update to 
        /// this <seealso cref="ISyncUpdater"/>.  If null, no child was update to cause this call 
        /// to <see cref="Update(Update,ISyncUpdater,ITrueSet{IEvaluateable})"/>.</param>
        /// <param name="caller">The <seealso cref="Dependency.Variables.Update"/> which is managing the update 
        /// procedure.</param>
        /// <param name="indexDomain">The indices of the update from the 
        /// <paramref name="updatedChild"/> below.</param>
        /// <returns>Returns the set for which the update is applicable.</returns>
        ITrueSet<IEvaluateable> Update(Update caller, ISyncUpdater updatedChild, ITrueSet<IEvaluateable> indexDomain);
    }


    internal interface ITypeGuarantee
    {
        TypeFlags TypeGuarantee { get; }
    }


    public interface IVariable  : IEvaluateable
    {
        IEvaluateable Contents { get; }
    }
    public interface IVariable<T> : IVariable
    {
        T Native { get; set; }
    }

    internal interface IVariableCollection : IEnumerable<IEvaluateable>
    {
        Vector Contents { get; }

    }


    internal interface IUpdatedVariable : IVariable
    {
        /// <summary>ONLY sets the contents.  Does nothing else.  This is called only within a 
        /// <seealso cref="Update.StructureLock"/> write lock so don't lock again.  The method 
        /// should return whether update succeeded or not.</summary>
        bool CommitContents(IEvaluateable newContent);
        /// <summary>ONLY sets the value.  Does nothing else.  This is called only outside a 
        /// <seealso cref="Update.StructureLock"/> write lock, but within a read lock.  If the 
        /// <seealso cref="IUpdatedVariable"/>'s value requires locking, it is safe to lock within 
        /// the implementation of this method.</summary>
        bool CommitValue(IEvaluateable newValue);
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
