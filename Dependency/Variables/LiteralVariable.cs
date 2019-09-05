using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency.Variables
{
    /// <summary>
    /// A <see cref="LiteralVariable{T}"/> blends the notion of CLR and literal dependency variables.  The current 
    /// value of type <typeparamref name="T"/> is maintained and always available.  It maintains the 
    /// <seealso cref="IWeakVariable{T}"/> pattern in that the dependency variable may expire due to garbage-
    /// collection if it has no listeners, but the CLR value will continue to be available.  A 
    /// <see cref="LiteralVariable{T}"/> will never change value in response to an update because its contents are 
    /// literal.
    /// </summary>
    public class LiteralVariable<T> : IWeakVariable<T> , IDynamicItem
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
                IDynamicItem parent = Parent;
                while (parent != null && parent.Update()) parent = parent.Parent;
            }
        }

        public LiteralVariable(T startValue, Func<T, ILiteral> converter)
        {
            this._Converter = converter ?? Dependency.Helpers.Obj2Eval;
            ContentValue = startValue;
            _Ref = null;
            _LockedVariable = null;
        }

        Variable IWeakVariable<T>.Source => Source;

        /// <summary>Creates or retrieves the existing <seealso cref="Variable"/>.</summary>
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

        internal IDynamicItem Parent { get; set; }
        IDynamicItem IDynamicItem.Parent { get => Parent; set => Parent = value; }

        public bool TryGetSource(out Variable v)
        {
            if (_Ref != null && _Ref.TryGetTarget(out v)) return true;
            v = null;
            return false;
        }
        
        public void SetLock(bool locked) => _LockedVariable = (locked) ? Source : null;


        public override string ToString() => ContentValue.ToString();

        bool IDynamicItem.Update()
        {
            if (TryGetSource(out Variable v)) return Variable.UpdateValue(v);
            return true;
        }

        public static implicit operator T(LiteralVariable<T> b) => b.ContentValue;
    }

    /// <summary>A <seealso cref="LiteralVariable{T}"/> optimized for <seealso cref="double"/> values.</summary>
    public sealed class LiteralDouble : LiteralVariable<double>
    {
        public LiteralDouble(double startValue = 0) : base(startValue, Number.FromDouble) { }
    }

    /// <summary>A <seealso cref="LiteralVariable{T}"/> optimized for <seealso cref="int"/> values.</summary>
    public sealed class LiteralInt : LiteralVariable<int>
    {
        public LiteralInt(int startValue = 0) : base(startValue, (i) => new Number(i)) { }
    }

    /// <summary>A <seealso cref="LiteralVariable{T}"/> optimized for <seealso cref="string"/> values.</summary>
    public sealed class LiteralString : LiteralVariable<string>
    {
        public LiteralString(string startValue = "") : base(startValue, (s) => new Dependency.String(s)) { }
    }
}
