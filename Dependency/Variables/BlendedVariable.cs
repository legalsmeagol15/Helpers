using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency.Variables
{
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
}
