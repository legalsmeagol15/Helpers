using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helpers;

namespace Dependency.Variables
{
    internal interface IBlendedVariable<T> : IAsyncUpdater, ISyncUpdater, IUpdatedVariable, INotifyUpdates<IEvaluateable>
    {
        T Get();
        void Set(T newValue);

        IEvaluateable Dependency { get; set; }
    }
    
    /// <summary>A dependency variable whose role is to store a CLR value.</summary>
    public sealed class Blended<T> : Variable, IBlendedVariable<T>
    {
        private static readonly IConverter<T> _Converter = Dependency.Values.Converter<T>.Default;
        static Blended() { IConverter<T> c = Dependency.Values.Converter<T>.Default; }

        private T _ClrValue;

        IEvaluateable IBlendedVariable<T>.Dependency { get => Contents; set => Contents = value; }

        public Blended(T value) { _ClrValue = value; }

        public T Get() => _ClrValue;

        public void Set(T newValue)
        {
            _ClrValue = newValue;
            if (Listeners.Count > 0 || Parent != null)
                Contents = _Converter.ConvertFrom(newValue);
        }

        internal override bool OnAddListener(ISyncUpdater isu)
        {
            if (Listeners.Count == 0 && Parent == null)
                SetContents(_Converter.ConvertFrom(_ClrValue));
            return base.OnAddListener(isu);
        }

        internal override bool OnRemoveListener(ISyncUpdater isu)
        {
            if (base.OnRemoveListener(isu))
            {
                if (Listeners.Count == 0 && Parent == null)
                    SetContents(Static_ND);
            }
            return false;
        }

        internal override void OnParentChanged(ISyncUpdater oldParent, ISyncUpdater newParent)
        {
            if (oldParent == null)
                if (Listeners.Count == 0)
                    SetContents(_Converter.ConvertFrom(_ClrValue));
            if (newParent == null)
                if (Listeners.Count == 0)
                    SetContents(Static_ND);
            base.OnParentChanged(oldParent, newParent);
        }

        protected override void OnValueChanged(IEvaluateable oldValue, IEvaluateable newValue)
        {
            if (_Converter.TryConvertTo(newValue, out T newClr))
                _ClrValue = newClr;
            base.OnValueChanged(oldValue, newValue);
        }

        private static readonly NoDependency Static_ND = new NoDependency();
        /// <summary>The dependency value that sits in the <see cref="Blended{T}"/> when it has 
        /// listeners and no parent.</summary>
        private struct NoDependency : IEvaluateable
        {
            IEvaluateable IEvaluateable.Value => this;
        }
    }

}
