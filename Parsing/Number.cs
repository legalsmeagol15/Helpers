using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.Parsing
{
    internal struct Number : IEvaluatable
    {
        public static readonly Number Zero = new Number(0m);
        public static readonly Number Pi = new Number((decimal)Math.PI);
        public static readonly Number E = new Number((decimal)Math.E);

        internal readonly decimal Value;

        internal Number(decimal m) { this.Value = m; }
        internal Number(double d) { this.Value = (decimal)d; }
        IEvaluatable IEvaluatable.Evaluate() => this;

        public static implicit operator Number(double d) => new Number((decimal)d);
        public static implicit operator double(Number n) => (double)n.Value;

        public static implicit operator Number(decimal m) => new Number(m);
        public static implicit operator decimal(Number n) => n.Value;

        public static implicit operator Number(int i) => new Number((decimal)i);
        public static implicit operator int(Number n) => (int)n.Value;

        public static Number operator +(Number a, Number b) => new Number(a.Value + b.Value);
        public static Number operator -(Number n) => new Number(-n.Value);
        public static Number operator -(Number a, Number b) => new Number(a.Value - b.Value);
        public static Number operator *(Number a, Number b) => new Number(a.Value * b.Value);
        public static Number operator /(Number a, Number b) => new Number(a.Value / b.Value);
        public static Number operator ^(Number a, Number b) => new Number(Math.Pow((double)a.Value, (double)b.Value));
        public static bool operator ==(Number a, Number b) => a.Value == b.Value;
        public static bool operator !=(Number a, Number b) => a.Value != b.Value;

        public override bool Equals(object obj)
        {
            if (obj is Number n) return this.Value == n.Value;
            return false;
        }

        public override int GetHashCode() => Value.GetHashCode();        
    }
}
