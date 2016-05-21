using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arithmetic.Text
{
    /// <summary>
 /// An interface enforcing that an object may be evaluated to a double.
 /// </summary>
    internal interface IEvaluateable : IToken
    {
        double Evaluate(Func<string, double> lookupFunction = null);
    }
}
