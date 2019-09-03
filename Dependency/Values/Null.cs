using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency
{
    [Serializable]
    public struct Null  : ILiteral<object>, ITypeGuarantee
    {        
        public static readonly Null Instance = new Null();
        
        public override bool Equals(object obj) => obj is Null;
        public override int GetHashCode() => 0;
        public override string ToString() => "<Null>";
        
        object ILiteral<object>.CLRValue => null;
        
        IEvaluateable IEvaluateable.Value => this;
        TypeFlags ITypeGuarantee.TypeGuarantee => TypeFlags.Null;
    }
}
