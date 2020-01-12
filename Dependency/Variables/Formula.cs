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
    public sealed class Formula: ISyncUpdater, IAsyncUpdater
    {
        private static readonly InvalidValueError _UncalculatedError = new InvalidValueError("Formula value not calculated yet.");

        private WeakReference<IEvaluateable> _FormulaRef;
        private IEvaluateable _Value = _UncalculatedError;
        private readonly Func<IEvaluateable> _Initializer;
        private readonly ReaderWriterLockSlim _ValueLock = new ReaderWriterLockSlim();

        /// <summary>
        /// Creates a new <see cref="Formula"/> with the given initializer function.  The 
        /// initializer will NOT be checked for circularity when it executes, this is the job of 
        /// the caller.
        /// </summary>
        /// <param name="initializer"></param>
        public Formula(Func<IEvaluateable> initializer) { this._Initializer = initializer; }
        
        ISyncUpdater ISyncUpdater.Parent { get; set; } = null;

        IEvaluateable IEvaluateable.Value
        {
            get
            {
                _ValueLock.EnterReadLock();
                try { return _Value; }
                finally { _ValueLock.ExitReadLock(); }
            }
        }

        private IEvaluateable GetFormula()
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

        bool ISyncUpdater.Update(Update caller, ISyncUpdater updatedChild) => SetValue(GetFormula().Value);

        private bool SetValue(IEvaluateable newValue)
        {
            _ValueLock.EnterUpgradeableReadLock();
            try
            {
                if (newValue.Equals(_Value)) return false;
                IEvaluateable oldValue = _Value;
                _ValueLock.EnterWriteLock();
                try { _Value = newValue; }
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
