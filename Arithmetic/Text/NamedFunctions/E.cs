using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arithmetic.Text.NamedFunctions
{
    sealed class E : NamedFunction
    {
        public override int MaximumArgs { get { return 0; } }

        public override int MinimumArgs { get { return 0; } }

        protected override object Operate(IList<object> args)
        {
            return Math.E;
        }

    }
    
}
