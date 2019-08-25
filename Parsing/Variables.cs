using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Helpers;
using System.Runtime.CompilerServices;
using System.Collections;
using DataStructures;
using System.Threading;

namespace Dependency
{
    public enum Mobility
    {
        // TODO:  more info will probably be needed
        None = 0,
        Column = 1,
        Row = 2,
        All = ~0
    }
    
    public sealed class Variable : IEvaluateable, IDisposable
    {
        // A variable is apt to have few sources, but many listeners (0 or 100,000 might be reasonable).

        /// <summary>A number the must be given to <seealso cref="SetContents(IEvaluateable, int)"/> to allow contents 
        /// modification.</summary>
        private readonly int ModLock;
        private ISet<Variable> _Sources = new HashSet<Variable>();
        private WeakReferenceSet<Variable> _Listeners = new WeakReferenceSet<Variable>();
        private IEvaluateable _Value = Null.Instance;  // Must be guaranteed never to be CLR null
        private readonly ReaderWriterLockSlim _ValueLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private static readonly ReaderWriterLockSlim _StructureLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);


        public IEvaluateable Value
        {
            get
            {
                _ValueLock.EnterReadLock();
                try { return _Value; }
                finally { _ValueLock.ExitReadLock(); }
            }
        }
        private IEvaluateable _Contents;
        public IEvaluateable Contents
        {
            get
            {
                // Contents defines structure.
                _StructureLock.EnterReadLock();
                try { return _Contents; }
                finally { _StructureLock.ExitReadLock(); }
            }
            set
            {
                // No key is given when Contents is set this way.
                SetContents(value, 0);
            }
        }
        public void SetContents(IEvaluateable value, int modKey = 0)
        {
            if (ModLock != modKey)
                throw new Exception("This Variable has a modification lock.  The correct modification key must be provided.");

            if (value == null) value = Dependency.Null.Instance;

            // First, update the structure-related variables (contents and sources).
            ISet<Variable> newSources = Helpers.GetTerms(value);
            _StructureLock.EnterUpgradeableReadLock();  // Lock 'a' because the structure shouldn't change while we examine it.
            try
            {
                if (TryFindCircularity(this, newSources, out Deque<Variable> path)) throw new CircularDependencyException(path);
                Variable[] oldSources = _Sources.Except(newSources).ToArray();
                _StructureLock.EnterWriteLock();
                try
                {
                    foreach (Variable oldSrc in oldSources) oldSrc._Listeners.Remove(this);
                    foreach (Variable newSrc in newSources) newSrc._Listeners.Add(this);
                    _Sources = newSources;
                    _Contents = value;
                }
                finally { _StructureLock.ExitWriteLock(); }
            }
            catch (CircularDependencyException) { throw; }
            finally { _StructureLock.ExitUpgradeableReadLock(); }

            // Second, update the changed value
            UpdateValue();
        }
        
        public IEvaluateable UpdateValue()
        {
            // This method makes the guess that most updates will NOT change the value.
            IEvaluateable oldValue, newValue;
            _ValueLock.EnterUpgradeableReadLock();
            _StructureLock.EnterReadLock();
            try
            {
                // Presume that a source Variable is already updated, since this method will be 
                // called a lot from the sources and we don't want an exponential function.
                newValue = (_Contents is Variable v) ? v.Value : _Contents.UpdateValue();
                if (newValue == null) newValue = Dependency.Null.Instance;

                // Rule out identical values, which should not invoke any further action.
                oldValue = _Value;
                if (oldValue.Equals(newValue))
                    return oldValue;

                // Update the value.
                _ValueLock.EnterWriteLock();
                _Value = newValue;
                _ValueLock.ExitWriteLock();
                FireValueChanged(oldValue, newValue);

                //Now update the listeners
                List<Task> tasks = new List<Task>();
                foreach (Variable listener in _Listeners)
                    tasks.Add(Task.Run(() => listener.UpdateValue()));
                Task.WaitAll(tasks.ToArray());
            }
            finally { _StructureLock.ExitReadLock(); _ValueLock.ExitUpgradeableReadLock(); }

            //UpdateListeners();
            return newValue;
            
        }

        private static bool TryFindCircularity(Variable target, 
                                                IEnumerable<Variable> sources,
                                                out Deque<Variable> path)
        {
            // TODO:  use a Stack<> object instead of stack frames, because I get a stack overflow at approx. 5000 levels deep
            if (sources != null)
            {
                foreach (Variable src in sources)
                {
                    if (ReferenceEquals(target, src)) { path = new Deque<Variable>(); path.AddFirst(src); return true; }
                    else if (TryFindCircularity(target, src._Sources, out path)) { path.AddFirst(src); return true; }
                }
            }
            path = null;
            return false;
        }
        
        
        /// <summary>Creates the <see cref="Variable"/>.  Does not update the <seealso cref="Variable.Value"/>.</summary>
        public Variable(IEvaluateable contents = null, int modLock = 0)
        {
            this.Contents = contents;
            this.ModLock = modLock;
        }
        
        internal string GetExpressionString(IContext perspective)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Variable> GetTerms() => _Sources;

        public override string ToString() => Contents.ToString() + " = " + Value.ToString();

        public event ValueChangedHandler<IEvaluateable> ValueChanged;
        private void FireValueChanged(IEvaluateable oldValue, IEvaluateable newValue)
            => ValueChanged?.Invoke(this, new ValueChangedArgs<IEvaluateable>(oldValue, newValue));

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // This is to prevent a memory leak in the form of an invalid WeakReference
                    foreach (var src in _Sources) src._Listeners.Remove(this);
                }
                disposedValue = true;
            }
        }
        void IDisposable.Dispose() { Dispose(true); }

        #endregion

    }









    public interface IWeakVariable<T>
    {   
        Variable Variable { get; }
        void SetLock(bool locked);
        T Value { get; }
        bool TryGetVariable(out Variable v);
    }

    /// <summary>
    /// A <see cref="BlendedVariable{T}"/> blends the notion of CLR and dependency variables.  The current value of 
    /// type <typeparamref name="T"/> is maintained and always available.  It maintains the 
    /// <seealso cref="IWeakVariable{T}"/> pattern in that the dependency variable may expire due to garbage 
    /// collection if it has no listeners.
    /// </summary>
    public struct BlendedVariable<T> : IWeakVariable<T>
    {
        private T _ContentValue;    // Will be both the Variable's Contents and its Value
        private Func<T, IEvaluateable> _Converter;
        private WeakReference<Variable> _Ref;
        private Variable _LockedVariable;
        public T Value
        {
            get => _ContentValue;
            set
            {
                _ContentValue = value;
                if (_Ref != null && _Ref.TryGetTarget(out Variable v)) v.Contents = _Converter(value);
            }
        }
        
        public BlendedVariable(T startValue, Func<T, IEvaluateable> converter = null)
        {
            this._Converter = converter ?? Dependency.Helpers.Obj2Eval;
            _ContentValue = startValue;
            _Ref = null;
            _LockedVariable = null;
        }

        public Variable Variable
        {
            get
            {
                if (_Ref == null)
                {
                    Variable vNew = new Variable(_Converter(_ContentValue));
                    _Ref = new WeakReference<Variable>(vNew);
                    return vNew;
                }
                else if (!_Ref.TryGetTarget(out Variable vExisting))
                {
                    _Ref.SetTarget(vExisting = new Variable(_Converter(_ContentValue)));
                    return vExisting;
                }
                else
                    return vExisting;
            }
        }
        public bool TryGetVariable(out Variable v)
        {
            if (_Ref != null && _Ref.TryGetTarget(out v)) return true;
            v = null;
            return false;
        }

        public void SetLock(bool locked) => _LockedVariable = (locked) ? Variable : null;
       

        public override string ToString() => _ContentValue.ToString();
    }
    
    /// <summary>
    /// A <see cref="SourceVariable{T}"/> is a variable whose contents never change, but the value may.  It maintains 
    /// the <seealso cref="IWeakVariable{T}"/> pattern in that if the <seealso cref="Dependency.Variable"/> is ever 
    /// garbage-collected, the next attempt to reference it through a dependency structure will re-create the 
    /// <seealso cref="Dependency.Variable"/> with the same contents (which may evaluate to a different value).
    /// </summary>
    public class SourceVariable<T> : IWeakVariable<T> // where T : struct
    {
        private T _Value;
        private WeakReference<Variable> _Ref;
        private readonly Func<IEvaluateable> _Initializer;
        private readonly Func<IEvaluateable, T> _Converter;
        private Variable _LockedVariable;

        /// <summary>Creates a new <see cref="SourceVariable{T}"/>.</summary>
        /// <param name="startValue">The starting value for the <see cref="SourceVariable{T}"/>.  This will be 
        /// disregarded if the initialized contents evaluate to a convertible value.</param>
        /// <param name="initializer">The function called every time this variable is initialized.</param>
        /// <param name="converter">The function which converts an <seealso cref="IEvaluateable"/> into the given type.
        /// </param>
        public SourceVariable(T startValue,  Func<IEvaluateable> initializer, Func<IEvaluateable, T> converter)
        {
            this._Initializer = initializer;
            this._Converter = converter;
            this._Ref = null;
            this._Value = TryConvert(_Initializer(), out T v) ? v : startValue;        
        }

        private bool TryConvert(IEvaluateable iev, out T value)
        {
            try
            {
                value = _Converter(iev);
                return true;
            } catch
            {
                value = _Value;
                return false;
            }
        }

        public Variable Variable
        {
            get
            {
                Variable v;
                if (_Ref == null)
                {
                    v = new Variable(_Initializer());
                    _Ref = new WeakReference<Variable>(v);
                    v.ValueChanged += On_Value_Changed;
                } else if (!_Ref.TryGetTarget(out v))
                {
                    _Ref.SetTarget(v = new Variable(_Initializer()));
                    v.ValueChanged += On_Value_Changed;
                }
                return v;
            }
        }

        public T Value => _Value;

        private void On_Value_Changed(object sender, ValueChangedArgs<IEvaluateable> e)
        {            
            TryConvert(e.After, out T newValue);
            if (_Value.Equals( newValue)) return;
            T oldValue = _Value;
            _Value = newValue;
            ValueChanged?.Invoke(this, new ValueChangedArgs<T>(oldValue, newValue));
        }

        void IWeakVariable<T>.SetLock(bool locked) => _LockedVariable = (locked) ? Variable : null;

        public bool TryGetVariable(out Variable v)
        {
            if (_Ref != null && _Ref.TryGetTarget(out v)) return true;
            v = null;
            return false;
        }

        private event ValueChangedHandler<T> ValueChanged;
    }
    


    /// <summary>An exception thrown when an invalid circular dependency is added to a DependencyGraph.</summary>
    public class CircularDependencyException : InvalidOperationException
    {
        IEnumerable<Variable> Path;
        /// <summary>Creates a new CircularDependencyException.</summary>
        public CircularDependencyException(IEnumerable<Variable> path, string message = "Circular reference identified.") : base(message) { this.Path = path; }
    }

}
