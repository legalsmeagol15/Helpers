using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helpers;
using DataStructures;

namespace Dependency.Variables
{
    public sealed class Source<T> : IAsyncUpdater, IVariable, INotifyUpdates<T>, IUpdatedVariable, ITypeGuarantee
    {
        private T _Value;
        private readonly IConverter<T> _Converter;
        private readonly TypeFlags _TypeGuarantee;

        TypeFlags ITypeGuarantee.TypeGuarantee => _TypeGuarantee;

        IEvaluateable IEvaluateable.Value => _Converter.ConvertFrom(_Value);

        IEvaluateable IVariable.Contents => _Converter.ConvertFrom(_Value);

        public Source(T startingValue, IConverter<T> converter = null)
        {
            this._Converter = converter ?? Dependency.Values.Converter<T>.Default;
            this._TypeGuarantee = (this._Converter is ITypeGuarantee itg) ? itg.TypeGuarantee : TypeFlags.Any;
            this._Value = startingValue;
        }

        public T Get() => _Value;
        public T Set(T newValue)
        {
            // Any change?
            if (newValue == null)
            {
                if (_Value == null) return default(T);
            }
            else if (newValue.Equals(_Value))
                return _Value;

            // Though the value is changed in the CLR, kick off an update to notify listeners of any change.
            T oldValue = _Value;
            _Value = newValue;
            IEvaluateable newContents = _Converter.ConvertFrom(newValue);
            Update update = Update.ForVariable(this, newContents);
            update.Execute();
            
            // Notify any CLR listeners to a change.
            ValueChanged?.Invoke(this, new ValueChangedArgs<T>(oldValue, newValue));

            return _Value;
        }


        bool IAsyncUpdater.AddListener(ISyncUpdater idi) => _Listeners.Add(idi);
        bool IAsyncUpdater.RemoveListener(ISyncUpdater idi) => _Listeners.Remove(idi);
        IEnumerable<ISyncUpdater> IAsyncUpdater.GetListeners() => _Listeners;
        private readonly Update.ListenerManager _Listeners = new Update.ListenerManager();

        void IUpdatedVariable.SetContents(IEvaluateable newContent) { }

        bool IUpdatedVariable.SetValue(IEvaluateable newValue) => true;

        public event ValueChangedHandler<T> ValueChanged;
    }
}
