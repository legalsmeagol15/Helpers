using DataStructures;
using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dependency.Variables
{
    /// <summary>
    /// A <see cref="Formula{T}"/> is intended to offer a weak variable representation of a 
    /// complex formula, whose value may changed along with its sources, but whose structure 
    /// remains static.
    /// <para/>For example, the length of a line will depend on the x,y points at either end of 
    /// the line, but the length will always be equal to the square root of the sum of the squares 
    /// of the line's rise and run.  If nothing actually depends on that formula, setting up a 
    /// standard variable which computes the length every time the line changes would mean wasted 
    /// computation.  For that reason, the <see cref="Formula{T}"/> will allow the dependency 
    /// structure to be garbage-collected until a listener actually attempts to bind to the 
    /// <see cref="Formula{T}"/> as a source.
    /// </summary>
    /// <typeparam name="T">A CLR type.</typeparam>
    public sealed class Formula<T> : ISyncUpdater, IAsyncUpdater, ITypeGuarantee
    {
        private static readonly InvalidValueError _UncalculatedError = new InvalidValueError("Formula value not calculated yet.");

        private readonly IConverter<T> _Converter;
        private WeakReference<IEvaluateable> _FormulaRef;
        private IEvaluateable _Hardener = null; // Use this to harden the weak inner reference, but only when we have listeners
        private IEvaluateable _DepValue = _UncalculatedError;
        private readonly Func<IEvaluateable> _Initializer;
        private readonly TypeFlags _TypeGuarantee;
        TypeFlags ITypeGuarantee.TypeGuarantee => _TypeGuarantee;
        private readonly ReaderWriterLockSlim _ValueLock = new ReaderWriterLockSlim();
        private T _CachedValue;
        public T Get() => _CachedValue;  // Call this "get" for continuity with Source{T}
        /// <summary>
        /// Creates a new <see cref="Formula{T}"/> with the given initializer function.  The 
        /// initializer will NOT be checked for circularity when it executes, this is the job of 
        /// the caller.
        /// </summary>
        /// <param name="initializer">The <paramref name="initializer"/> Func will be invoked 
        /// whenever the <seealso cref="Formula{T}"/>'s contents have been garbage-collected for 
        /// lack of a listener, in order to re-establish the <seealso cref="Formula{T}"/>'s 
        /// contents.</param>
        /// <param name="converter">The converting object that will present values in CLR format.</param>
        public Formula(Func<IEvaluateable> initializer, IConverter<T> converter = null)
        {
            this._Initializer = initializer;
            this._Converter = converter ?? Dependency.Values.Converter<T>.Default;
            this._TypeGuarantee = (this._Converter is ITypeGuarantee itg) ? itg.TypeGuarantee : TypeFlags.Any;
        }

        private ISyncUpdater _Parent = null;
        ISyncUpdater ISyncUpdater.Parent
        {
            get => _Parent;
            set
            {
                if (value == null)
                {
                    if (_Listeners.Count == 0)
                        _Hardener = null;
                }
                else if (_Hardener == null)
                    SetValue((_Hardener = GetInnerFormula()).Value);
            }
        }

        IEvaluateable IEvaluateable.Value
        {
            get
            {
                _ValueLock.EnterReadLock();
                try { return _DepValue; }
                finally { _ValueLock.ExitReadLock(); }
            }
        }

        private IEvaluateable GetInnerFormula()
        {
            Update.StructureLock.EnterUpgradeableReadLock();
            try
            {
                IEvaluateable formula;
                if (_FormulaRef == null)
                {
                    Update.StructureLock.EnterWriteLock();
                    try { _FormulaRef = new WeakReference<IEvaluateable>(formula = _Initializer()); }
                    finally { Update.StructureLock.ExitWriteLock(); }
                }
                else if (!_FormulaRef.TryGetTarget(out formula))
                {
                    Update.StructureLock.EnterWriteLock();
                    try { _FormulaRef.SetTarget(formula = _Initializer()); }
                    finally { Update.StructureLock.ExitWriteLock(); }
                }
                return formula;
            }
            finally { Update.StructureLock.ExitUpgradeableReadLock(); }
        }

        bool ISyncUpdater.Update(Update caller, ISyncUpdater updatedChild) => SetValue(GetInnerFormula().Value);

        private bool SetValue(IEvaluateable newValue)
        {
            _ValueLock.EnterUpgradeableReadLock();
            try
            {
                if (newValue.Equals(_DepValue)) return false;
                IEvaluateable oldValue = _DepValue;
                _ValueLock.EnterWriteLock();
                try { _DepValue = newValue; _Converter.TryConvert(_DepValue, out _CachedValue); }
                finally { _ValueLock.ExitWriteLock(); }
                ValueChanged?.Invoke(this, new ValueChangedArgs<IEvaluateable>(oldValue, newValue));
                return true;
            }
            finally { _ValueLock.ExitUpgradeableReadLock(); }
        }


        bool IAsyncUpdater.AddListener(ISyncUpdater idi)
        {
            if (_Hardener == null) _Hardener = GetInnerFormula();
            return _Listeners.Add(idi);
        }
        bool IAsyncUpdater.RemoveListener(ISyncUpdater idi)
        {
            if (!_Listeners.Remove(idi)) return false;
            if (_Listeners.Count == 0 && _Parent == null) _Hardener = null;
            return true;
        }
        IEnumerable<ISyncUpdater> IAsyncUpdater.GetListeners() => _Listeners;
        private readonly Update.ListenerManager _Listeners = new Update.ListenerManager();

        public event ValueChangedHandler<IEvaluateable> ValueChanged;

    }
}
