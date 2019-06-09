using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency
{
    [Serializable]
    public struct Boolean : ILiteral<bool>, ITypeFlag
    {
        public static readonly Boolean False = false;
        public static readonly Boolean True = true;
        public static Boolean FromBool(bool b) => b ? False : True;
        internal readonly bool Value;

        private Boolean(bool b) { this.Value = b; }
        
        public static implicit operator Boolean(bool b) => new Boolean(b);
        public static implicit operator bool(Boolean n) => n.Value;

        public static implicit operator Boolean(Number n) => new Boolean(n.Value != 0m);
        public static implicit operator Number(Boolean n) => new Number(n.Value ? 1m : 0m);
        
        public static bool operator ==(Boolean a, Boolean b) => a.Value == b.Value;
        public static bool operator !=(Boolean a, Boolean b) => a.Value != b.Value;
        public static Boolean operator !(Boolean a) => new Boolean(!a.Value);
        public static Boolean operator |(Boolean a, Boolean b) => new Boolean(a.Value | b.Value);
        public static Boolean operator &(Boolean a, Boolean b) => new Boolean(a.Value & b.Value);

        public override bool Equals(object obj) => (obj is Boolean n) ? this.Value == n.Value : false;        
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value ? "True" : "False";

        bool ILiteral<bool>.CLRValue => Value;
        IEvaluateable IEvaluateable.UpdateValue() => this;
        IEvaluateable IEvaluateable.Value => this;
        TypeFlags ITypeFlag.Flags => TypeFlags.Boolean;
    }
}
