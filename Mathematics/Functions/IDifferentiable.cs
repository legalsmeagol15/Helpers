using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics.Calculus
{

    /// <summary>
    /// Represents a differentiable function, including common calculus operations such as returning derivatives, returning integrals, finding local maxima and minima, 
    /// and so forth.
    /// </summary>
    public interface IDifferentiable : Mathematics.Functions.IParametric<double>
    {
        /// <summary>
        /// Returns the derivative of this differentiable function.
        /// </summary>
        IDifferentiable GetDerivative();

        /// <summary>
        /// Returns the integral of this differentiable function, plus the given constant.
        /// </summary>
        /// <param name="constant">Optional.  If omitted, the integral will be added to a constant of 0.</param>        
        IDifferentiable GetIntegral(double constant = 0.0);

        /// <summary>
        /// Returns the minimum value of the differentiable function, starting and the given 'x' value and ending at the given 'x' value.
        /// </summary>
        /// <param name="starting">The starting 'x' value for the locality where the minimum value is sought.</param>
        /// <param name="ending">The ending 'x' value for the locality where the minimum value is sought.</param>
        /// <param name="x">Returns the 'x' value where the local minimum is found.</param>
        /// <returns>Returns the minimum value within the specified locality.</returns>
        double GetMinimum(double starting, double ending, out double x);

        /// <summary>
        /// Returns the maximum value of the differentiable function, starting and the given 'x' value and ending at the given 'x' value.
        /// </summary>
        /// <param name="starting">The starting 'x' value for the locality where the maximum value is sought.</param>
        /// <param name="ending">The ending 'x' value for the locality where the maximum value is sought.</param>
        /// <param name="x">Returns the 'x' value where the local maximum is found.</param>
        /// <returns>Returns the maximum value within the specified locality.</returns>
        double GetMaximum(double starting, double ending, out double x);

        /// <summary>
        /// Returns the arc length of the function, starting and ending at the given 'x' values.
        /// </summary>
        double GetLength(double starting, double ending);

        /// <summary>
        /// Returns the real roots of the given differentiable function, or those 'x' values where the value of the function is equal to 0.
        /// </summary>        
        IEnumerable<double> GetRoots();


        IDifferentiable GetDifference(double d);
        IDifferentiable GetSum(double d);
    }
}
