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
    public sealed class Formula<T>: ISyncUpdater, IAsyncUpdater
    {
        private static readonly InvalidValueError _UncalculatedError = new InvalidValueError("Formula value not calculated yet.");

        private readonly IConverter<T> _Converter;
        private WeakReference<IEvaluateable> _FormulaRef;
        private IEvaluateable _DepValue = _UncalculatedError;
        private readonly Func<IEvaluateable> _Initializer;
        private readonly ReaderWriterLockSlim _ValueLock = new ReaderWriterLockSlim();
        private T _CachedValue;
        public T Value { get => _CachedValue; }
        /// <summary>
        /// Creates a new <see cref="Formula{T}"/> with the given initializer function.  The 
        /// initializer will NOT be checked for circularity when it executes, this is the job of 
        /// the caller.
        /// </summary>
        /// <param name="initializer"></param>
        /// <param name="converter">The converting object that will present values in CLR format.</param>
        public Formula(Func<IEvaluateable> initializer, IConverter<T> converter = null)
        {
            this._Initializer = initializer;
            this._Converter = converter ?? Dependency.Values.Converter<T>.Default;
        }

        ISyncUpdater ISyncUpdater.Parent { get; set; } = null;

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

        bool IAsyncUpdater.AddListener(ISyncUpdater idi) => _Listeners.Add(idi);
        bool IAsyncUpdater.RemoveListener(ISyncUpdater idi) => _Listeners.Remove(idi);
        IEnumerable<ISyncUpdater> IAsyncUpdater.GetListeners() => _Listeners;
        private readonly Update.ListenerManager _Listeners = new Update.ListenerManager();

        public event ValueChangedHandler<IEvaluateable> ValueChanged;
    }
}
