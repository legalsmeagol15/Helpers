using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing
{
    [Serializable]
    internal class String : IEvaluateable
    {
        internal readonly string Value;

        internal String(string str) { this.Value = str; }
        IEvaluateable IEvaluateable.Evaluate() => this;

        public static implicit operator String(string str) => new String(str);
        public static implicit operator string(String s) => s.Value;

        public static implicit operator String(Number n) => new String(n.ToString());
        //public static implicit operator IExpression(String s) => Decimal.TryParse(s.Value, out decimal m) ? new Number(m) : new Error("Cannot convert string "+s.Value + " to number.");

        public static bool operator ==(String a, String b) => a.Value == b.Value;
        public static bool operator !=(String a, String b) => a.Value != b.Value;
        public static String operator +(String a, String b) => new String(a.Value + b.Value);

        public override bool Equals(object obj)
        {
            if (obj is String n) return this.Value == n.Value;
            return false;
        }

        public override int GetHashCode() => Value.GetHashCode();
    }
}
