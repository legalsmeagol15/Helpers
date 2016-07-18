﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics.Text.NamedFunctions
{
    sealed class ASIN : NamedFunction
    {
        public override int MaximumArgs { get { return 1; } }

        public override int MinimumArgs { get { return 1; } }

        protected override object Operate(IList<object> args)
        {
            return Math.Asin((double)args[0]);
        }

    }
}
