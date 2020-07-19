using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helpers;
using DataStructures;

namespace Dependency.Variables
{
    /// <summary>
    /// A dependency variable which can NEVER have a dependee, and whose value is always 
    /// guaranteed to be convertible to a CLR value.
    /// </summary>
    public sealed class Source<T> : IAsyncUpdater, IVariable, IUpdatedVariable, ITypeGuarantee, ISyncUpdater
    {
        ISyncUpdater ISyncUpdater.Parent { get; set; }
        ITrueSet<IEvaluateable> ISyncUpdater.Update(Update caller, ISyncUpdater updatedChild)
            => Dependency.Variables.Update.UniversalSet;

        private T _Value;
        private readonly IConverter<T> _Converter;
        private readonly TypeFlags _TypeGuarantee;

        TypeFlags ITypeGuarantee.TypeGuarantee => _TypeGuarantee;

        IEvaluateable IEvaluateable.Value => _Converter.ConvertUp(_Value);

        IEvaluateable IVariable.Contents => _Converter.ConvertUp(_Value);

        public Source(T startingValue, IConverter<T> converter = null)
        {
            this._Converter = converter ?? Dependency.Values.Converter<T>.Default;
            this._TypeGuarantee = (this._Converter is ITypeGuarantee itg) ? itg.TypeGuarantee : TypeFlags.Any;
            this._Value = startingValue;
        }



        public T Get() => _Value;
        public void Set(T newValue)
        {
            // Any change?
            T oldValue = _Value;
            if (newValue == null)
            {
                _Value = newValue;
                if (oldValue == null) return;
            }
            else if (newValue.Equals(_Value))
                return;

            // Though the value is changed in the CLR, kick off an update to notify listeners of any change.

            _Value = newValue;
            IEvaluateable newContents = _Converter.ConvertUp(newValue);
            Update update = Update.ForVariable(this, newContents);
            update.Execute();

            // Notify any CLR listeners to a change.
            Updated?.Invoke(this, new ValueChangedArgs<IEvaluateable>(_Converter.ConvertUp(oldValue), newContents));

            return;
        }


        bool IAsyncUpdater.AddListener(ISyncUpdater isu) => _Listeners.Add(isu);
        bool IAsyncUpdater.RemoveListener(ISyncUpdater isu) => _Listeners.Remove(isu);
        IEnumerable<ISyncUpdater> IAsyncUpdater.GetListeners() => _Listeners;
        private readonly Update.ListenerManager _Listeners = new Update.ListenerManager();

        bool IUpdatedVariable.CommitContents(IEvaluateable newContent) => true;

        bool IUpdatedVariable.CommitValue(IEvaluateable newValue) => true;

        public event ValueChangedHandler<IEvaluateable> Updated;
    }
}
