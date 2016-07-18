using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics.Text.NamedFunctions
{
    sealed class MAX : NamedFunction
    {
        public override int MaximumArgs { get { return 2; } }

        public override int MinimumArgs { get { return 2; } }

        protected override object Operate(IList<object> args)
        {
            return Math.Max((double)args[0], (double)args[1]);
        }

    }
}
