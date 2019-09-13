using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency.Variables
{

    /// <summary>
    /// A dynamic variable can have its contents set to any <seealso cref="IEvaluateable"/>, from
    /// <seealso cref="Number"/>s to <seealso cref="Dependency.Expression"/>s.  Yet, it implements the 
    /// <seealso cref="IWeakVariable{T}"/> pattern, so its CLR value continues to be valid (both settable and 
    /// gettable) even when the related <seealso cref="Dependency.Variables.Variable"/> has been garbage-collected.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DynamicVariable<T> : IWeakVariable<T>, IDynamicItem
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
                _LockedVariable = Source;
                _LockedVariable.Contents = value;
            }
        }

        public Variable Source
        {
            get
            {
                if (_Ref == null)
                {
                    Variable vNew = new Variable(_ToIEval(_Value));
                    _Ref = new WeakReference<Variable>(vNew);
                    vNew.Parent = this;
                    return vNew;
                }
                else if (!_Ref.TryGetTarget(out Variable vExisting))
                {
                    _Ref.SetTarget(vExisting = new Variable(_ToIEval(_Value)));
                    vExisting.Parent = this;
                    //vExisting.ValueChanged += On_Value_Changed;
                    return vExisting;
                }
                else
                    return vExisting;
            }
        }
        public bool TryGetSource(out Variable v)
        {
            if (_Ref != null && _Ref.TryGetTarget(out v)) return true;
            v = null;
            return false;
        }

        public T Value => _Value;

        
        public DynamicVariable(T defaultValue, Func<IEvaluateable, T> toCLR, Func<T, IEvaluateable> toIEval = null)
        {
            this._ToIEval = toIEval ?? Dependency.Helpers.Obj2Eval;
            this._ToClr = toCLR;
            this._DefaultValue = defaultValue;
            this._Value = Value;
            this._LockedVariable = null;
            this._Ref = null;
        }

        public void Clear()
        {
            if (_LockedVariable == null) return;
            _LockedVariable.Contents = _ToIEval(_Value);
            _LockedVariable = null;
        }

        public void SetLock(bool locked) => _LockedVariable = (locked) ? Source : null;
        
        

        public static implicit operator T(DynamicVariable<T> d) => d.Value;

        public override string ToString() => TryGetSource(out Variable v) ? v.ToString() : _Value.ToString();

        bool IDynamicItem.Update()
        {
            if (TryGetSource(out Variable v)) return Variable.UpdateValue(v);
            return true;
        }

        internal IDynamicItem Parent { get; set; }
        IDynamicItem IDynamicItem.Parent { get => Parent; set => Parent = value; }

    }

    public sealed class DynamicBool : DynamicVariable<bool>
    {
        public DynamicBool(bool defaultValue = false)
            : base(defaultValue,
                  (iev) => (iev is Dependency.Boolean b) ? b.Value : false,
                  (b) => b ? Dependency.Boolean.True : Dependency.Boolean.False)
        { }
    }
    public sealed class DynamicByte : DynamicVariable<byte>
    {
        public DynamicByte(byte defaultValue = 0)
            : base(defaultValue,
                  (iev) => (iev is Dependency.Number n) ? (byte)n : (byte)Number.Zero,
                  (b) => new Number(b))
        { }
    }
    public sealed class DynamicDouble : DynamicVariable<double>
    {
        public DynamicDouble(double defaultValue = 0.0d)
            : base(defaultValue,
                  (iev) => (iev is Dependency.Number n) ? (double)n : 0d,
                  Number.FromDouble)
        { }
    }
    public sealed class DynamicInt : DynamicVariable<int>
    {
        public DynamicInt(int defaultValue = 0)
            : base(defaultValue,
                  (iev) => (iev is Dependency.Number n) ? (int)n : 0,
                  (i) => new Number(i))
        { }
    }
    public sealed class DynamicString : DynamicVariable<string>
    {
        public DynamicString(string defaultValue = "")
            : base(defaultValue,
                  (iev) => iev.ToString(),
                  (s) => new Dependency.String(s))
        { }
    }
}
