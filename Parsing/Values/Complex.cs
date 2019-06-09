using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency
{
    public struct Complex : IEvaluateable, ITypeFlag
    {
        internal readonly TypeFlags TypeFlags;
        internal decimal Real;
        internal decimal Imaginary;

        public Complex(decimal real, decimal imaginary)
        {
            this.Real = real;
            this.Imaginary = imaginary;
            TypeFlags tf = 0;
            if (real != 0) tf |= TypeFlags.Number | Number.GetValueFlags(real);
            if (imaginary != 0) tf |= TypeFlags.Imaginary | Number.GetValueFlags(imaginary);
            this.TypeFlags = tf;
        }

        IEvaluateable IEvaluateable.Value => throw new NotImplementedException();

        TypeFlags ITypeFlag.Flags => TypeFlags.ComplexAny;

        IEvaluateable IEvaluateable.UpdateValue()
        {
            throw new NotImplementedException();
        }
    }
}
