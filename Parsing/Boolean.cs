using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.Parsing
{
    internal struct Boolean : IExpression
    {
        internal readonly bool Value;

        internal Boolean(bool b) { this.Value = b; }
        IExpression IExpression.Evaluate() => this;

        public static implicit operator Boolean(bool b) => new Boolean(b);
        public static implicit operator bool(Boolean n) => n.Value;

        public static implicit operator Boolean(Number n) => new Boolean(n.Value != 0m);
        public static implicit operator Number(Boolean n) => new Number(n.Value ? 1m : 0m);
        
        public static bool operator ==(Boolean a, Boolean b) => a.Value == b.Value;
        public static bool operator !=(Boolean a, Boolean b) => a.Value != b.Value;
        public static Boolean operator !(Boolean a) => new Boolean(!a.Value);
        public static Boolean operator |(Boolean a, Boolean b) => new Boolean(a.Value | b.Value);
        public static Boolean operator &(Boolean a, Boolean b) => new Boolean(a.Value & b.Value);

        public override bool Equals(object obj)
        {
            if (obj is Boolean n) return this.Value == n.Value;
            return false;
        }

        public override int GetHashCode() => Value.GetHashCode();
    }
}
