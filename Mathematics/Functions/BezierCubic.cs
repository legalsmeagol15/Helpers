using Arithmetic;
using Calculus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Calculus
{
    /// <summary>
    /// Represents a cubic Bezier curve, which is a curve defined by a start and end point, and two control points.  The first control point determines the direction the 
    /// curve leaves the start point, and the second control point determines the direction from which the curve enters the end point.
    /// </summary>
    public class BezierCubic : IFunction<Point>
        //TODO:  validative BezierCubic
    {
        /// <summary>
        /// The starting anchor point of the Bezier curve, or in other words, the point where t=0.
        /// </summary>
        public Point Start { get; }
        /// <summary>
        /// The control point that the curve approaches towards at t=0.
        /// </summary>
        public Point ControlA { get; }
        /// <summary>
        /// The control point that the curve approaches from at t=1.
        /// </summary>
        public Point ControlB { get; }
        /// <summary>
        /// The ending anchor point of the Bezier curve, or in other words, the point where t=1.
        /// </summary>
        public Point End { get; }

        /// <summary>
        /// Creates a new quadratic Bezier curve.
        /// </summary>
        public BezierCubic(Point start, Point controlA, Point controlB, Point end)
        {
            this.Start = start;
            this.ControlA = controlA;
            this.ControlB = controlB;
            this.End = end;
        }

        /// <summary>
        /// Returns the cubic Polynomial that describes the x-traversal of this Bezier curve.
        /// </summary>        
        public Polynomial GetXPolynomial()
        {
            double p0 = Start.X, p1 = ControlA.X, p2 = ControlB.X, p3 = End.X;
            return Polynomial.FromCubic(p3 - p0 + (3 * (p1 - p2)),
                                        3 * (p0 - (2 * p1) + p2),
                                        3 * (p1 - p0),
                                        p0);            
        }

        /// <summary>
        /// Returns the cubic Polynomial that describes the y-traversal of this Bezier curve.
        /// </summary>        
        public Polynomial GetYPolynomial()
        {
            double p0 = Start.Y, p1 = ControlA.Y, p2 = ControlB.Y, p3 = End.Y;
            return Polynomial.FromCubic(p3 - p0 + (3 * (p1 - p2)),
                                        3 * (p0 - (2 * p1) + p2),
                                        3 * (p1 - p0),
                                        p0);
        }

        /// <summary>
        /// Returns the quadratic Polynomial that describes the x-derivative of this Bezier curve.
        /// </summary>        
        public Polynomial GetXDerivative()
        {
            double p0 = Start.X, p1 = ControlA.X, p2 = ControlB.X, p3 = End.X;
            return Polynomial.FromQuadratic(3 * (-p0 + (3 * p1) - (3 * p2) + p3), 
                                            6 * (p0 - (2 * p1) + p2), 
                                            3 * (p1 - p0));            
        }
        /// <summary>
        /// Returns the quadratic Polynomial that describes the y-derivative of this Bezier curve.
        /// </summary>   
        public Polynomial GetYDerivative()
        {
            double p0 = Start.Y, p1 = ControlA.Y, p2 = ControlB.Y, p3 = End.Y;
            return Polynomial.FromQuadratic(3 * (-p0 + (3 * p1) - (3 * p2) + p3),
                                            6 * (p0 - (2 * p1) + p2),
                                            3 * (p1 - p0));
        }
        /// <summary>
        /// Returns the point specified by the given traversal value from 0 to 1.
        /// </summary>   
        public Point Evaluate(double t)
        {
            double tSquared = t * t;
            double tCubed = tSquared * t;

            //Find the 'x'.
            double p0 = Start.X, p1 = ControlA.X, p2 = ControlB.X, p3 = End.X;
            double x = (tCubed * (p3 - p0 + (3 * (p1 - p2)))) 
                     + (tSquared * 3 * (p0 - (2 * p1) + p2)) 
                     + (t * 3 * (p1 - p0)) 
                     + p0;
            
            //Find the 'y'.
            p0 = Start.Y;
            p1 = ControlA.Y;
            p2 = ControlB.Y;
            p3 = End.Y;
            double y = (tCubed * (p3 - p0 + (3 * (p1 - p2))))
                     + (tSquared * 3 * (p0 - (2 * p1) + p2))
                     + (t * 3 * (p1 - p0))
                     + p0;
            
            return new Point(x, y);
        }
    }
}
