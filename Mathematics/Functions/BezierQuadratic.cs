using Mathematics;
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
    /// Represents a quadratic Bezier curve, which is a curve defined by its start and its end, with a single control point determining which direction the curve 
    /// leaves the start and enters the end.
    /// </summary>
    public class BezierQuadratic : IFunction<Point>
        //TODO:  Validate BezierQuadratic
    {
        /// <summary>
        /// The starting anchor point of the Bezier curve, or in other words, the point where t=0.
        /// </summary>
        public Point Start { get; }
        /// <summary>
        /// The control point that the curve approaches towards at t=0, and enters from at t=1.
        /// </summary>
        public Point Control { get; }
        /// <summary>
        /// The ending anchor point of the Bezier curve, or in other words, the point where t=1.
        /// </summary>
        public Point End { get; }

        /// <summary>
        /// Creates a new quadratic Bezier curve.
        /// </summary>
        public BezierQuadratic(Point start, Point control, Point end)
        {
            this.Start = start;
            this.Control = control;
            this.End = end;
        }

        /// <summary>
        /// Returns the quadratic Polynomial that describes the x-traversal of this Bezier curve.
        /// </summary>        
        public Polynomial GetXPolynomial()
        {
            return Polynomial.FromQuadratic(Start.X - (2 * Control.X) + End.X, 2 * (Control.X - Start.X), Start.X);            
        }

        /// <summary>
        /// Returns the quadratic Polynomial that describes the y-traversal of this Bezier curve.
        /// </summary>        
        public Polynomial GetYPolynomial()
        {
            return Polynomial.FromQuadratic(Start.Y - (2 * Control.Y) + End.Y, 2 * (Control.Y - Start.Y), Start.Y);
        }

        /// <summary>
        /// Returns the linear Polynomial that describes the x-derivative of this Bezier curve.
        /// </summary>        
        public Polynomial GetXDerivative()
        {
            double P0_x_2 = Start.X * 2;
            double P1_x_2 = Control.X * 2;
            return Polynomial.FromLinear(P0_x_2 - (2 * P1_x_2) + (2 * End.X), P1_x_2 - P0_x_2);
        }
        /// <summary>
        /// Returns the linear Polynomial that describes the y-derivative of this Bezier curve.
        /// </summary>   
        public Polynomial GetYDerivative()
        {
            double P0_x_2 = Start.Y * 2;
            double P1_x_2 = Control.Y * 2;
            return Polynomial.FromLinear(P0_x_2 - (2 * P1_x_2) + (2 * End.Y), P1_x_2 - P0_x_2);
        }
        /// <summary>
        /// Returns the point specified by the given traversal value from 0 to 1.
        /// </summary>   
        public Point Evaluate(double t)
        {
            double tSquared = t * t;
            double tTimes2 = 2 * t;
            double x = (tSquared * (Start.X - (2 * Control.X) + End.X) + (tTimes2 * (Control.X - Start.X)) + Start.X);
            double y = (tSquared * (Start.Y - (2 * Control.Y) + End.Y) + (tTimes2 * (Control.Y - Start.Y)) + Start.Y);
            return new Point(x, y);
        }

    }
}
