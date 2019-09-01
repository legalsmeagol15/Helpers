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

    public sealed class Variable : IDynamicItem, IVariable, IEvaluateable
    // DO NOT implement IDisposable to clean up listeners.  The listeners will expire via garbage collection.
    {
        // A variable is apt to have few sources, but many listeners (0 or 100,000 might be reasonable).

        IDynamicItem IDynamicItem.Parent { get=>null; set=> throw new InvalidOperationException(); }
        private readonly WeakReferenceSet<Reference> _Listeners = new WeakReferenceSet<Reference>();
        private IEvaluateable _Value = Null.Instance;  // Must be guaranteed never to be CLR null
        private readonly ReaderWriterLockSlim _ValueLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private static readonly ReaderWriterLockSlim _StructureLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        bool IVariable.AddListener(Reference r) => _Listeners.Add(r);
        bool IVariable.RemoveListener(Reference r) => _Listeners.Remove(r);
        IEnumerable<Reference> IVariable.GetReferences() => GetReferences();

        internal IEnumerable<Reference> GetReferences()
        {
            if (Contents == null) yield break;
            Stack<IEvaluateable> stack = new Stack<IEvaluateable>();
            stack.Push(this.Contents);
            while (stack.Count > 0)
            {
                IEvaluateable focus = stack.Pop();
                switch (focus)
                {
                    case IExpression exp: stack.Push(exp.Contents); continue;
                    case IFunction f: foreach (var input in f.Inputs) stack.Push(f); continue;
                    case Reference r: yield return r; continue;
                }
                
            }
        }
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
                if (value is Expression exp) value = exp.Contents;
                SetContents(value, 0);
            }
        }


        /// <summary>Creates the <see cref="Variable"/>.  Does not update the <seealso cref="Variable.Value"/>.</summary>
        public Variable(IEvaluateable contents = null) {
            this.Contents = contents;
        }

        public void SetContents(IEvaluateable newContents, int modKey = 0)
        {
            if (newContents == null) newContents = Dependency.Null.Instance;

            // First, update the structure-related variables (contents and sources).
            ISet<Reference> newRefs = new HashSet<Reference>(Helpers.GetReferences(newContents));
            _StructureLock.EnterUpgradeableReadLock();  // Lock 'a' because the structure shouldn't change while we examine it.
            try
            {
                ISet<Reference> oldRefs = new HashSet<Reference>(Helpers.GetReferences(_Contents));

                //if (TryFindCircularity(this, newSources, out Deque<IVariable> path)) throw new CircularDependencyException(path);
                _StructureLock.EnterWriteLock();
                try
                {
                    foreach (Reference oldRef in oldRefs)
                    {
                        if (!newRefs.Remove(oldRef) && oldRef.HeadProperty != null && oldRef.HeadProperty is IVariable v)
                            v.RemoveListener(oldRef);
                    }
                    foreach (Reference newRef in newRefs)
                        if (newRef != null && newRef.HeadProperty is IVariable v)
                            v.AddListener(newRef);
                    _Contents = newContents;
                }
                finally { _StructureLock.ExitWriteLock(); }
            }
            catch (CircularDependencyException) { throw; }
            finally { _StructureLock.ExitUpgradeableReadLock(); }

            // Second, update the changed value
            Update();
        }

        public bool Update()
        {
            // This method makes the guess that most updates will NOT change the value.
            IEvaluateable oldValue, newValue;
            _ValueLock.EnterUpgradeableReadLock();
            _StructureLock.EnterReadLock();
            try
            {
                // Presume that a source Variable is already updated, since this method will be 
                // called a lot from the sources and we don't want an exponential function.
                newValue = _Contents.Value;
                if (newValue == null) newValue = Dependency.Null.Instance;

                // Rule out identical values, which should not invoke any further action.
                oldValue = _Value;
                if (oldValue.Equals(newValue))
                    return false;

                // Update the value.
                _ValueLock.EnterWriteLock();
                _Value = newValue;
                _ValueLock.ExitWriteLock();
                FireValueChanged(oldValue, newValue);

                //Now update the listeners
                List<Task> tasks = new List<Task>();
                foreach (Reference r in _Listeners)
                    tasks.Add(Task.Run(() => r.Update()));
                Task.WaitAll(tasks.ToArray());
            }
            finally { _StructureLock.ExitReadLock(); _ValueLock.ExitUpgradeableReadLock(); }

            return true;


        }

        //private static bool TryFindCircularity(Variable target,
        //                                        IEnumerable<IVariable> sources,
        //                                        out Deque<IVariable> path)
        //{
        //    // TODO:  use a Stack<> object instead of stack frames, because I get a stack overflow at approx. 5000 levels deep
        //    // TODO:  I'm not caching the sources anywhere, so it would optimize this method to cache the variables' sources.
        //    if (sources != null)
        //    {
        //        foreach (IVariable src in sources)
        //        {
        //            if (ReferenceEquals(target, src)) { path = new Deque<IVariable>(); path.AddFirst(src); return true; }
        //            else if (TryFindCircularity(target, Helpers.GetTerms(src), out path)) { path.AddFirst(src); return true; }
        //        }
        //    }
        //    path = null;
        //    return false;
        //}



        public override string ToString() => Contents.ToString() + " = " + Value.ToString();

        public event ValueChangedHandler<IEvaluateable> ValueChanged;
        private void FireValueChanged(IEvaluateable oldValue, IEvaluateable newValue)
            => ValueChanged?.Invoke(this, new ValueChangedArgs<IEvaluateable>(oldValue, newValue));


    }









    public interface IWeakVariable<T>
    {
        Variable Variable { get; }
        void SetLock(bool locked);
        T Value { get; }
        bool TryGetVariable(out Variable v);
    }



    /// <summary>
    /// A <see cref="BlendedVariable{T}"/> blends the notion of CLR and literal dependency variables.  The current 
    /// value of type <typeparamref name="T"/> is maintained and always available.  It maintains the 
    /// <seealso cref="IWeakVariable{T}"/> pattern in that the dependency variable may expire due to garbage-
    /// collection if it has no listeners, but the CLR value will continue to be available.
    /// </summary>
    public class BlendedVariable<T> : IWeakVariable<T>
    {
        protected T ContentValue;    // Will be both the Variable's Contents and its Value
        private readonly Func<T, IEvaluateable> _Converter;
        private WeakReference<Variable> _Ref;
        private Variable _LockedVariable;
        public T Value
        {
            get => ContentValue;
            set
            {
                ContentValue = value;
                if (_Ref != null && _Ref.TryGetTarget(out Variable v)) v.Contents = _Converter(value);
            }
        }

        public BlendedVariable(T startValue, Func<T, ILiteral> converter)
        {
            this._Converter = converter ?? Dependency.Helpers.Obj2Eval;
            ContentValue = startValue;
            _Ref = null;
            _LockedVariable = null;
        }

        Variable IWeakVariable<T>.Variable => Source;
        public Variable Source
        {
            get
            {
                if (_Ref == null)
                {
                    Variable vNew = new Variable(_Converter(ContentValue));
                    _Ref = new WeakReference<Variable>(vNew);
                    return vNew;
                }
                else if (!_Ref.TryGetTarget(out Variable vExisting))
                {
                    _Ref.SetTarget(vExisting = new Variable(_Converter(ContentValue)));
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

        public void SetLock(bool locked) => _LockedVariable = (locked) ? Source : null;


        public override string ToString() => ContentValue.ToString();

        public static implicit operator T(BlendedVariable<T> b) => b.ContentValue;
    }

    /// <summary>A <seealso cref="BlendedVariable{T}"/> optimized for <seealso cref="double"/> values.</summary>
    public sealed class BlendedDouble : BlendedVariable<double>
    {
        public BlendedDouble(double startValue = 0) : base(startValue, Number.FromDouble) { }
    }

    /// <summary>A <seealso cref="BlendedVariable{T}"/> optimized for <seealso cref="int"/> values.</summary>
    public sealed class BlendedInt : BlendedVariable<int>
    {
        public BlendedInt(int startValue = 0) : base(startValue, (i) => new Number(i)) { }
    }

    /// <summary>A <seealso cref="BlendedVariable{T}"/> optimized for <seealso cref="string"/> values.</summary>
    public sealed class BlendedString : BlendedVariable<string>
    {
        public BlendedString(string startValue = "") : base(startValue, (s) => new Dependency.String(s)) { }
    }



    /// <summary>
    /// A dynamic variable can have its contents set to any <seealso cref="IEvaluateable"/>, from
    /// <seealso cref="Number"/>s to <seealso cref="Dependency.Expression"/>s.  Yet, it implements the 
    /// <seealso cref="IWeakVariable{T}"/> pattern, so its CLR value continues to be valid (both settable and 
    /// gettable) even when the related <seealso cref="Dependency.Variable"/> has been garbage-collected.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DynamicVariable<T> : IWeakVariable<T>
    {
        private readonly T _DefaultValue;
        private T _Value;
        private Variable _LockedVariable;
        private readonly Func<T, IEvaluateable> _ToIEval;
        private readonly Func<IEvaluateable, T> _ToClr;
        private WeakReference<Variable> _Ref;

        public IEvaluateable Contents
        {
            get
            {
                if (_Ref != null && _Ref.TryGetTarget(out Variable v)) return v.Contents;
                return _ToIEval(_Value);
            }
            set
            {
                _LockedVariable = Variable;
                _LockedVariable.Contents = value;
            }
        }

        public Variable Variable
        {
            get
            {
                if (_Ref == null)
                {
                    Variable vNew = new Variable(_ToIEval(_Value));
                    _Ref = new WeakReference<Variable>(vNew);
                    vNew.ValueChanged += On_Value_Changed;
                    return vNew;
                }
                else if (!_Ref.TryGetTarget(out Variable vExisting))
                {
                    _Ref.SetTarget(vExisting = new Variable(_ToIEval(_Value)));
                    vExisting.ValueChanged += On_Value_Changed;
                    return vExisting;
                }
                else
                    return vExisting;
            }
        }

        public T Value => _Value;

        public DynamicVariable(T defaultValue, Func<T, IEvaluateable> toIEval = null)
        {
            this._ToIEval = toIEval ?? Dependency.Helpers.Obj2Eval;
            this._DefaultValue = defaultValue;
            this._Value = Value;
            this._LockedVariable = null;
            this._Ref = null;
        }

        public void Clear()
        {
            if (_LockedVariable == null) return;
            _LockedVariable.Contents = _ToIEval(_Value = _DefaultValue);
            _LockedVariable = null;
        }

        public void SetLock(bool locked) => _LockedVariable = (locked) ? Variable : null;

        public bool TryGetVariable(out Variable v)
        {
            if (_Ref != null && _Ref.TryGetTarget(out v)) return true;
            v = null;
            return false;
        }


        private void On_Value_Changed(object sender, ValueChangedArgs<IEvaluateable> e)
        {
            T oldValue = _Value, newValue = default(T);
            try
            {
                newValue = _ToClr(e.After);
                if (newValue.Equals(_Value)) return;
            }
            catch (InvalidCastException)
            {
                return;
            }

            if (oldValue == null)
            {
                if (newValue == null) return;
            }
            else if (oldValue.Equals(newValue))
                return;

            _Value = newValue;
            ValueChanged?.Invoke(this, new ValueChangedArgs<T>(oldValue, newValue));
        }

        public event ValueChangedHandler<T> ValueChanged;

        public static implicit operator T(DynamicVariable<T> d) => d.Value;

        public override string ToString() => TryGetVariable(out Variable v) ? v.ToString() : _Value.ToString();
    }

    public sealed class DynamicBool : DynamicVariable<bool>
    {
        public DynamicBool(bool defaultValue = false) : base(defaultValue, (b) => b ? Dependency.Boolean.True : Dependency.Boolean.False) { }
    }
    public sealed class DynamicByte : DynamicVariable<byte>
    {
        public DynamicByte(byte defaultValue = 0) : base(defaultValue, (b) => new Number(b)) { }
    }
    public sealed class DynamicDouble : DynamicVariable<double>
    {
        public DynamicDouble(double defaultValue = 0.0d) : base(defaultValue, Number.FromDouble) { }
    }
    public sealed class DynamicInt : DynamicVariable<int>
    {
        public DynamicInt(int defaultValue = 0) : base(defaultValue, (i) => new Number(i)) { }
    }
    public sealed class DynamicString : DynamicVariable<string>
    {
        public DynamicString(string defaultValue = "") : base(defaultValue, (s) => new Dependency.String(s)) { }
    }



    /// <summary>
    /// A <see cref="SourceVariable{T}"/> is a variable whose contents never change, but the value may.  It maintains 
    /// the <seealso cref="IWeakVariable{T}"/> pattern in that if the <seealso cref="Dependency.Variable"/> is ever 
    /// garbage-collected, the next attempt to reference it through a dependency structure will re-create the 
    /// <seealso cref="Dependency.Variable"/> with the same contents (which may evaluate to a different value).
    /// </summary>
    public class SourceVariable<T> : IWeakVariable<T>
    {
        private T _Value = default(T);
        private WeakReference<Variable> _Ref;
        private readonly Func<IEvaluateable> _Initializer;
        private Variable _LockedVariable;

        /// <summary>Returns whether this variable is participating currently in the dependency system.</summary>
        public bool IsActive => TryGetVariable(out _);

        /// <summary>Creates a new <see cref="SourceVariable{T}"/>.</summary>
        /// <param name="startValue">The starting value for the <see cref="SourceVariable{T}"/>.  This will be 
        /// disregarded if the initialized contents evaluate to a convertible value.</param>
        /// <param name="initializer">The function called every time this variable is initialized.</param>
        /// </param>
        public SourceVariable(T startValue, Func<IEvaluateable> initializer)
        {
            this._Initializer = initializer;
            this._Ref = null;
            this._Value = TryConvert(_Initializer(), out T v) ? v : startValue;
        }

        private bool TryConvert(IEvaluateable iev, out T value)
        {
            try
            {
                value = (T)iev;
                return true;
            }
            catch (InvalidCastException)
            {
                value = _Value;
                return false;
            }
        }

        public Variable Source
        {
            get
            {
                Variable v;
                if (_Ref == null)
                {
                    v = new Variable(_Initializer());
                    _Ref = new WeakReference<Variable>(v);
                    v.ValueChanged += On_Value_Changed;
                }
                else if (!_Ref.TryGetTarget(out v))
                {
                    _Ref.SetTarget(v = new Variable(_Initializer()));
                    v.ValueChanged += On_Value_Changed;
                }
                return v;
            }
        }
        Variable IWeakVariable<T>.Variable => Source;

        public T Value => _Value;

        private void On_Value_Changed(object sender, ValueChangedArgs<IEvaluateable> e)
        {
            TryConvert(e.After, out T newValue);

            if (_Value == null)
            {
                if (newValue == null) return;
            }
            else if (_Value.Equals(newValue)) return;
            T oldValue = _Value;
            _Value = newValue;
            ValueChanged?.Invoke(this, new ValueChangedArgs<T>(oldValue, newValue));
        }

        void IWeakVariable<T>.SetLock(bool locked) => _LockedVariable = (locked) ? Source : null;

        public bool TryGetVariable(out Variable v)
        {
            if (_Ref != null && _Ref.TryGetTarget(out v)) return true;
            v = null;
            return false;
        }

        private event ValueChangedHandler<T> ValueChanged;

        public override string ToString() => TryGetVariable(out Variable v) ? v.ToString() : _Value.ToString();
    }



    /// <summary>An exception thrown when an invalid circular dependency is added to a DependencyGraph.</summary>
    internal class CircularDependencyException : InvalidOperationException
    {
        IEnumerable<IVariable> Path;
        /// <summary>Creates a new CircularDependencyException.</summary>
        public CircularDependencyException(IEnumerable<IVariable> path, string message = "Circular reference identified.") : base(message) { this.Path = path; }
    }

}
