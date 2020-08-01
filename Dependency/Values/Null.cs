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

    /// <summary>
    /// Used to invalidate contents or value in a way that users should never see.
    /// </summary>
    internal struct NoEqual : IEvaluateable
    {
        IEvaluateable IEvaluateable.Value => this;
        public override bool Equals(object obj) => false; // NEVER equals anything.
        public override int GetHashCode() => throw new InvalidOperationException();
        public override string ToString() => "<NoEqual>";
    }
}
