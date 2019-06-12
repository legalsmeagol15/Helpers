using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency
{
    public struct Complex : IEvaluateable, ITypeGuarantee
    {
        internal readonly TypeFlags TypeFlags;
        internal decimal Real;
        internal decimal Imaginary;

        public Complex(decimal real, decimal imaginary)
        {
            this.Real = real;
            this.Imaginary = imaginary;
            TypeFlags tf = 0;
            if (real != 0) tf |= TypeFlags.RealAny;
            if (imaginary != 0) tf |= TypeFlags.ComplexAny;
            this.TypeFlags = tf;
        }

        IEvaluateable IEvaluateable.Value => throw new NotImplementedException();

        TypeFlags ITypeGuarantee.TypeGuarantee => TypeFlags.ComplexAny;

        IEvaluateable IEvaluateable.UpdateValue()
        {
            throw new NotImplementedException();
        }
    }
}
