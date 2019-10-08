using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency
{
    [Serializable]
    [DebuggerStepThrough]
    public struct Number : ILiteral<decimal>, ITypeGuarantee, IComparable<Number>
    {
        public static readonly Number Zero = new Number(0m);        
        public static readonly Number One = new Number(1m);
        public static readonly Number Pi = new Number((decimal)Math.PI);
        public static readonly Number E = new Number((decimal)Math.E);

        private readonly TypeFlags _TypeFlags;

        internal TypeFlags TypeFlags => _TypeFlags;
        internal decimal CLR_Value;

        public Number(decimal m) { this.CLR_Value = m; this._TypeFlags = (_IsInteger(m)) ? TypeFlags.Integer : TypeFlags.RealAny; }
        
        public Number(double d) : this((decimal)d) { }
        public Number(int i) : this((decimal)i) { }
        
        public static implicit operator Number(int i) => new Number((decimal)i);
        public static implicit operator int(Number n) => (int)n.CLR_Value;

        public static implicit operator Number(double d)
        {
            if (double.IsNaN(d) || double.IsInfinity(d))
                throw new InvalidCastException("Values NaN and infinity cannot be converted to a Number.");
            return new Number(d);
        }
        public static implicit operator double(Number n) => (double)n.CLR_Value;

        public static implicit operator Number(decimal m) => new Number(m);
        public static implicit operator decimal(Number n) => n.CLR_Value;

        public static implicit operator bool(Number n) => n == 0;
        public static implicit operator Number(bool b) => b ? Number.One : Number.Zero;

        public static implicit operator byte(Number n) => (byte)n.CLR_Value;
        public static implicit operator Number(byte b) => new Number((decimal)b);

        public static ILiteral FromDouble(double d)
        {
            if (double.IsNaN(d) || double.IsInfinity(d)) return new InvalidValue("An infinite or NaN Number cannot be created.");
            return new Number(d);
        }

        
        private static bool _IsInteger(decimal m) => (decimal)((int)m) ==  m;
        public bool IsInteger => _IsInteger(CLR_Value);

        

        public static Number operator +(Number a, Number b) => new Number(a.CLR_Value + b.CLR_Value);
        public static Number operator -(Number n) => new Number(-n.CLR_Value);
        public static Number operator -(Number a, Number b) => new Number(a.CLR_Value - b.CLR_Value);
        public static Number operator *(Number a, Number b) => new Number(a.CLR_Value * b.CLR_Value);
        public static Number operator /(Number a, Number b) => new Number(a.CLR_Value / b.CLR_Value);

        internal static bool TryParse(object v, out Number n)
        {
            switch (v)
            {
                case int i: n = i; return true;
                case double d: n = d; return true;
                case decimal m: n = m; return true;
                case Number num: n = num; return true;
                case string s: if (decimal.TryParse(s, out decimal s_m)) { n = s_m; return true; } break;
                case String str: if (decimal.TryParse(str.Value, out decimal str_m)) { n = str_m; return true; } break;
            }
            n = Zero;
            return false;            
        }

        public static Number operator ^(Number a, Number b) => new Number(Math.Pow((double)a.CLR_Value, (double)b.CLR_Value));
        public static bool operator ==(Number a, Number b) => a.CLR_Value == b.CLR_Value;
        public static bool operator ==(Number a, double d) => (double)a.CLR_Value == d;
        public static bool operator ==(Number a, decimal m) => a.CLR_Value == m;
        public static bool operator ==(Number a, int i) => a.CLR_Value == i;

        public static bool operator !=(Number a, Number b) => a.CLR_Value != b.CLR_Value;
        public static bool operator !=(Number a, double d) => (double)a.CLR_Value != d;
        public static bool operator !=(Number a, decimal m) => a.CLR_Value != m;
        public static bool operator !=(Number a, int i) => a.CLR_Value == (decimal)((int)a.CLR_Value) && (int)a.CLR_Value != i;

        public static bool operator >(Number a, Number b) => a.CLR_Value > b.CLR_Value;
        public static bool operator <(Number a, Number b) => a.CLR_Value < b.CLR_Value;

        public override bool Equals(object obj)
        {
            switch (obj)
            {
                case Number n: return this.CLR_Value == n.CLR_Value;
                case double d:return this.CLR_Value == (decimal)d;
                case decimal m:return this.CLR_Value == m;
                case int i:return this.CLR_Value == (decimal)i;
            }            
            return false;
        }

        public override int GetHashCode() => CLR_Value.GetHashCode();
        public override string ToString() => CLR_Value.ToString();
        
        
        decimal ILiteral<decimal>.CLRValue => CLR_Value;

        int IComparable<Number>.CompareTo(Number other) => CLR_Value.CompareTo(other.CLR_Value);

        IEvaluateable IEvaluateable.Value => this;
        TypeFlags ITypeGuarantee.TypeGuarantee => _TypeFlags;
        
    }
}
