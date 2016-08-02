using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Mathematics.Text
{
    /// <summary>
    /// Contains methods for parsing text to Expressions.
    /// </summary>
    public static class Parsing
    {



        /// <summary>
        /// Attempts to parse the given text expression into a Point.
        /// </summary>
        /// <param name="expression">The text expression to parse.</param>
        /// <param name="point">This out parameter returns the point that would be parsed from the given text.</param>
        /// <param name="functionDictionary">Optional.  The dictionary to use to interpret functions like 'sin' or 'sqrt'.</param>
        /// <returns>Returns true if the the given text can be parsed to a Point.  Otherwise, returns false.</returns>
        public static bool TryParseToPoint(string expression, out Point point, IDictionary<string, NamedFunction> functionDictionary = null, Func<string, double> unitFactor = null)
        //TODO:  validate Arithmetic.Text.Parsing.TryParseToPoint
        {
            //Step #1 - split the given text into segments that were divided by ',' commas.
            string[] coords = expression.Split(',');

            //At this point, there will be some number of items in coords[] that were originally divided by ',' commas.  Each segment may be convertible in 
            //itself to an expression representing either 'x' or 'y', or multiple segments may have to be combined to represent an 'x' or a 'y'.  Obviously, 
            //there must be both an 'x' and a 'y' to return a valid point.  So, first see how many segments must be combined (between 1 and the count of 
            //coords[]) to make the 'x' coordinate, and then take the remainder of the segments and combine them to make the 'y'.  If this cannot be done, 
            //then the given text simply cannot be parsed into a point.

            //Step #2 - find how many segments are needed to make a coherent 'x' coordinate.
            Mathematics.Text.Expression xExpression = null, yExpression = null;
            int xSegs;
            for (xSegs = 1; xSegs < coords.Length; xSegs++)
            {
                string xText = string.Join(",", coords, 0, xSegs);
                if (Expression.TryParse(xText, out xExpression, null, null, functionDictionary, unitFactor))
                    break;

                //Otherwise, do nothing.  Just keep trying with the next segment.
            }

            //Step #3 - Now, the rest of coords[] should make a coherent 'y' coordinate.
            string yText = string.Join(",", coords, xSegs, (coords.Length - xSegs));
            if (!Expression.TryParse(yText, out yExpression, null, null, functionDictionary, unitFactor))
            {
                point = new Point(double.NaN, double.NaN);
                return false;
            }


            //Step #4 - Success!  just evaluate the expressions, and return the new point with a true result.
            try
            {
                double x = (double)xExpression.Evaluate();
                double y = (double)yExpression.Evaluate();
                point = new Point(x, y);
                return true;
            }
            catch
            {
                point = new Point(double.NaN, double.NaN);
                return false;
            }




        }

        /// <summary>
        /// Attempts to parse the given text expression into a double.
        /// </summary>
        /// <param name="expression">The text expression to parse.</param>
        /// <param name="value">This out parameter returns the double that would be parsed from the given text.</param>
        /// <param name="functionDictionary">Optional.  The dictionary to use to interpret functions like 'sin' or 'sqrt'.</param>
        /// <returns>Returns true if the the given text can be parsed to a double.  Otherwise, returns false.</returns>
        public static bool TryParseToDouble(string expression, out double value, IDictionary<string, NamedFunction> functionDictionary = null, Func<string, double> unitFactor = null)
        //TODO:  validate Arithmetic.Text.Parsing.TryParseToDouble
        {

            Expression exp;
            if (!Expression.TryParse(expression, out exp, null, null, functionDictionary, unitFactor))
            {
                value = double.NaN;
                return false;
            }
            try
            {
                value = (double)exp.Evaluate();
                return true;
            }
            catch
            {
                value = double.NaN;
                return false;
            }
        }


    }
}
