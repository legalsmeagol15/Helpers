using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency
{
    [Serializable]
    public struct Number : ILiteral<decimal>, ITypeGuarantee, IComparable<Number>
    {
        public static readonly Number Zero = new Number(0m);        
        public static readonly Number One = new Number(1m);
        public static readonly Number Pi = new Number((decimal)Math.PI);
        public static readonly Number E = new Number((decimal)Math.E);

        private readonly TypeFlags _TypeFlags;

        internal TypeFlags TypeFlags => _TypeFlags;
        internal decimal Value;

        public Number(decimal m) { this.Value = m; this._TypeFlags = (_IsInteger(m)) ? TypeFlags.Integer : TypeFlags.RealAny; }
        
        public Number(double d) : this((decimal)d) { }
        public Number(int i) : this((decimal)i) { }
        
        public static implicit operator Number(int i) => new Number((decimal)i);
        public static implicit operator int(Number n) => (int)n.Value;

        public static implicit operator Number(double d) => new Number((decimal)d);
        public static implicit operator double(Number n) => (double)n.Value;

        public static implicit operator Number(decimal m) => new Number(m);
        public static implicit operator decimal(Number n) => n.Value;

        public static implicit operator Boolean(Number n) => (n ==0) ? Boolean.False : Boolean.True;
        public static implicit operator Number(Boolean b) => b.Value ? Number.One : Number.Zero;


        private static bool _IsInteger(decimal m) => (decimal)((int)m) ==  m;
        public bool IsInteger => _IsInteger(Value);

        

        public static Number operator +(Number a, Number b) => new Number(a.Value + b.Value);
        public static Number operator -(Number n) => new Number(-n.Value);
        public static Number operator -(Number a, Number b) => new Number(a.Value - b.Value);
        public static Number operator *(Number a, Number b) => new Number(a.Value * b.Value);
        public static Number operator /(Number a, Number b) => new Number(a.Value / b.Value);

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

        public static Number operator ^(Number a, Number b) => new Number(Math.Pow((double)a.Value, (double)b.Value));
        public static bool operator ==(Number a, Number b) => a.Value == b.Value;
        public static bool operator ==(Number a, double d) => (double)a.Value == d;
        public static bool operator ==(Number a, decimal m) => a.Value == m;
        public static bool operator ==(Number a, int i) => a.Value == i;

        public static bool operator !=(Number a, Number b) => a.Value != b.Value;
        public static bool operator !=(Number a, double d) => (double)a.Value != d;
        public static bool operator !=(Number a, decimal m) => a.Value != m;
        public static bool operator !=(Number a, int i) => a.Value == (decimal)((int)a.Value) && (int)a.Value != i;

        public static bool operator >(Number a, Number b) => a.Value > b.Value;
        public static bool operator <(Number a, Number b) => a.Value < b.Value;

        public override bool Equals(object obj)
        {
            switch (obj)
            {
                case Number n: return this.Value == n.Value;
                case double d:return this.Value == (decimal)d;
                case decimal m:return this.Value == m;
                case int i:return this.Value == (decimal)i;
            }            
            return false;
        }

        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString();
        
        
        decimal ILiteral<decimal>.CLRValue => Value;
        IEvaluateable IEvaluateable.UpdateValue() => this;

        int IComparable<Number>.CompareTo(Number other) => Value.CompareTo(other.Value);

        IEvaluateable IEvaluateable.Value => this;
        TypeFlags ITypeGuarantee.TypeGuarantee => _TypeFlags;
    }
}
