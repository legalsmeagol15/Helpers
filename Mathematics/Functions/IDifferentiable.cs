using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics.Functions
{
    public interface IDifferentiable <TDomain, TRange>
    {
        ///// <summary>Evaluates the differentiable function, with the given input.</summary>
        ///// <param name="atValue">The value at which to evaluate the differentiable function.</param>
        ///// <returns>Returns the value of the differentiable function at the given input.</returns>
        //TRange Evaluate(TDomain atValue);

        /// <summary>Returns the function that is the definite integral of this differentiable function.</summary>
        /// <param name="constant">Optional.  The constant offset for the definite integral.</param>
        IDifferentiable<TDomain, TRange> GetIntegral(TDomain constant, IEnumerable<IVariable<TDomain>> integratingVariables);

        /// <summary>
        /// Returns the blah blah bah
        /// </summary>
        /// <param name="variables"></param>
        /// <returns></returns>
        object GetDerivative(IEnumerable<IVariable<TDomain>> differentiatingVariables);

        TRange this[TDomain input] { get; }
        
    }
}
