using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arithmetic.Text.NamedFunctions
{
    sealed class LOG : NamedFunction
    {
        public override int MaximumArgs { get { return 2; } }

        public override int MinimumArgs { get { return 2; } }

        protected override object Operate(IList<object> args)
        {
            return Math.Log((double)args[0], (double)args[1]);
        }

    }
}
