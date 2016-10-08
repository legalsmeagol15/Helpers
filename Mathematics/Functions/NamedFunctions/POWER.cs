using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics.Functions.NamedFunctions
{
    sealed class POWER : AbstractNamedFunction
    {
        public override int MaximumArgs { get { return 2; } }

        public override int MinimumArgs { get { return 2; } }

        protected override object Operate(IList<object> args)
        {
            return Math.Pow((double)args[0], (double)args[1]);
        }

    }
}
