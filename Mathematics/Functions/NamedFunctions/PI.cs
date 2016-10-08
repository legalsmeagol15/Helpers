using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics.Functions.NamedFunctions
{
    sealed class PI : AbstractNamedFunction
    {
        public override int MaximumArgs { get { return 0; } }

        public override int MinimumArgs { get { return 0; } }

        protected override object Operate(IList<object> args)
        {
            return Math.PI;
        }

    }
}
