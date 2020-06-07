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

        // TODO:  allow a Number to be infinite?
        public static readonly Number MaxValue = new Number(decimal.MaxValue, true);
        public static readonly Number MinValue = new Number(decimal.MinValue, true);
        public static readonly Number Zero = new Number(0m, true);
        public static readonly Number One = new Number(1m, true);
        public static readonly Number Pi = new Number((decimal)Math.PI, false);
        public static readonly Number E = new Number((decimal)Math.E, false);

        private readonly TypeFlags _TypeFlags;

        internal TypeFlags TypeFlags => _TypeFlags;
        internal decimal CLR_Value;

        public Number(decimal m) : this(m, _IsInteger(m)) { }
        private Number(decimal m, bool isInt) // Some m's shouldn't check if they're ints, like MaxValue or MinValue
        {
            this.CLR_Value = m;
            this._TypeFlags = isInt ? TypeFlags.Integer : TypeFlags.RealAny;
        }

        public Number(double d) :
            this((double.IsNaN(d) || double.IsInfinity(d)) ? throw new InvalidCastException("Values NaN and infinity cannot be converted to a " + typeof(Number).Name + ".")
                                                           : (decimal)d)
        { }
        public Number(int i) : this((decimal)i) { }

        public static implicit operator Number(int i) => new Number((decimal)i, false);
        public static implicit operator int(Number n) => (int)n.CLR_Value;

        public static implicit operator Number(double d) => new Number(d);
        public static implicit operator double(Number n) => (double)n.CLR_Value;

        public static implicit operator Number(decimal m) => new Number(m);
        public static implicit operator decimal(Number n) => n.CLR_Value;

        public static implicit operator bool(Number n) => n == 0;
        public static implicit operator Number(bool b) => b ? Number.One : Number.Zero;

        public static implicit operator byte(Number n) => (byte)n.CLR_Value;
        public static implicit operator Number(byte b) => new Number((decimal)b, false);

        public static ILiteral FromDouble(double d)
        {
            if (double.IsNaN(d) || double.IsInfinity(d))
                return new InvalidValueError("An infinite or NaN Number cannot be created.");
            return new Number(d);
        }

        /// <summary>Returns whether the given value is an integer.</summary>
        /// <exception cref="OverflowException">Thrown if the given value is too large/small to be 
        /// an int.</exception>
        private static bool _IsInteger(decimal m) => (decimal)((int)m) == m;
        public bool IsInteger => _IsInteger(CLR_Value);



        public static Number operator +(Number a, Number b) => new Number(a.CLR_Value + b.CLR_Value);
        public static Number operator -(Number n) => new Number(-n.CLR_Value);
        public static Number operator -(Number a, Number b) => new Number(a.CLR_Value - b.CLR_Value);
        public static Number operator *(Number a, Number b) => new Number(a.CLR_Value * b.CLR_Value);
        public static Number operator /(Number a, Number b) => new Number(a.CLR_Value / b.CLR_Value);

        public static bool TryParse(string str, out Number n)
        {
            if (!decimal.TryParse(str, out decimal m)) { n = Zero; return false; }
            n = new Number(m);
            return true;
        }

        public static Number operator ^(Number a, Number b) => new Number(Math.Pow((double)a.CLR_Value, (double)b.CLR_Value));
        public static bool operator ==(Number a, Number b) => a.CLR_Value == b.CLR_Value;
        public static bool operator ==(Number a, double d) => (double)a.CLR_Value == d;
        public static bool operator ==(Number a, decimal m) => a.CLR_Value == m;
        public static bool operator ==(Number a, int i) => a.CLR_Value == i;

        public static bool operator !=(Number a, Number b) => a.CLR_Value != b.CLR_Value;
        public static bool operator !=(Number a, double d) => (double)a.CLR_Value != d;
        public static bool operator !=(Number a, decimal m) => a.CLR_Value != m;
        public static bool operator !=(Number a, int i) => !a.IsInteger || (int)a.CLR_Value != i;

        public static bool operator >(Number a, Number b) => a.CLR_Value > b.CLR_Value;
        public static bool operator <(Number a, Number b) => a.CLR_Value < b.CLR_Value;

        public override bool Equals(object obj)
        {
            switch (obj)
            {
                case Number n: return this.CLR_Value == n.CLR_Value;
                case double d: return this.CLR_Value == (decimal)d;
                case decimal m: return this.CLR_Value == m;
                case int i: return this.CLR_Value == (decimal)i;
            }
            return false;
        }

        public override int GetHashCode() => CLR_Value.GetHashCode();
        public override string ToString() => CLR_Value.ToString();
        public int ToInt() => (int)CLR_Value;
        public decimal ToDecimal() => CLR_Value;
        public double ToDouble() => (double)CLR_Value;
        public byte ToByte() => (byte)CLR_Value;


        decimal ILiteral<decimal>.CLRValue => CLR_Value;

        public int CompareTo(Number other) => CLR_Value.CompareTo(other.CLR_Value);

        IEvaluateable IEvaluateable.Value => this;
        TypeFlags ITypeGuarantee.TypeGuarantee => _TypeFlags;

    }
}
