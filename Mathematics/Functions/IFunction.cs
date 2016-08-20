using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics.Functions
{
    /// <summary>
    /// Represents a mathematical function that can be evaluated at a given parametric value.  The return type may be a double, a Point, or any 
    /// other result for a function.
    /// </summary>
    public interface IParametric<T>
    {
        /// <summary>
        /// Finds and returns the value of the function at the given 't'.
        /// </summary>
        T Evaluate(double t);
    }
}
