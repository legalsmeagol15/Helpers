﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics.Functions.NamedFunctions
{
    sealed class TAN : AbstractNamedFunction
    {
        public override int MaximumArgs { get { return 1; } }

        public override int MinimumArgs { get { return 1; } }

        protected override object Operate(IList<object> args)
        {
            return Math.Tan((double)args[0]);
        }

    }
}
