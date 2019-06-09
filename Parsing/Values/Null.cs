using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency
{
    [Serializable]
    public sealed class Null  : ILiteral<object>, ITypeFlag
    {        
        public static readonly Null Instance = new Null();
        
        private Null() { }
        
        public override bool Equals(object obj) => obj != null && obj is Null;
        public override int GetHashCode() => 0;
        public override string ToString() => "<Null>";


        
        object ILiteral<object>.CLRValue => null;
        IEvaluateable IEvaluateable.UpdateValue() => this;
        IEvaluateable IEvaluateable.Value => this;
        TypeFlags ITypeFlag.Flags => TypeFlags.Null;
    }
}
