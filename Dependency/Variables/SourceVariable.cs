using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency.Variables
{







    /// <summary>
    /// A <see cref="SourceVariable{T}"/> is a variable whose contents never change, but the value may.  It maintains 
    /// the <seealso cref="IWeakVariable{T}"/> pattern in that if the <seealso cref="Dependency.Variables.Variable"/> 
    /// is ever garbage-collected, the next attempt to reference it through a dependency structure will re-create the 
    /// <seealso cref="Dependency.Variables.Variable"/> with the same contents (which may evaluate to a different 
    /// value).
    /// </summary>
    public class SourceVariable<T> : IWeakVariable<T>
    {
        private T _Value = default(T);
        private WeakReference<Variable> _Ref;
        private readonly Func<IEvaluateable> _Initializer;
        private Variable _LockedVariable;

        /// <summary>Returns whether this variable is participating currently in the dependency system.</summary>
        public bool IsActive => TryGetSource(out _);

        /// <summary>Creates a new <see cref="SourceVariable{T}"/>.</summary>
        /// <param name="startValue">The starting value for the <see cref="SourceVariable{T}"/>.  This will be 
        /// disregarded if the initialized contents evaluate to a convertible value.</param>
        /// <param name="initializer">The function called every time this variable is initialized.</param>
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
        Variable IWeakVariable<T>.Source => Source;

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

        public bool TryGetSource(out Variable v)
        {
            if (_Ref != null && _Ref.TryGetTarget(out v)) return true;
            v = null;
            return false;
        }

        private event ValueChangedHandler<T> ValueChanged;

        public override string ToString() => TryGetSource(out Variable v) ? v.ToString() : _Value.ToString();
    }


}
