using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Mathematics.Functions;

namespace Mathematics.Calculus
{
    /// <summary>
    /// Represents a quadratic Bezier curve, which is a curve defined by its start and its end, with a single control point determining which 
    /// direction the curve leaves the start and enters the end.
    /// </summary>
    public class BezierQuadratic : IParametric<Point>, IDifferentiable<Point>
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
        /// Returns a Bezier specified by the given polynomials, which are defined as follows:  the co-efficients of the x-polynomial are given 
        /// by Ax, Bx, Cx, and Dx, where Ax is the highest-order coefficient; and the co-efficients of the y-polynomial are similarly given by 
        /// Ay, By, Cy, and Dy.
        /// </summary>
        /// <param name="Ax">The t^2 coefficient of the x-polynomial.</param>
        /// <param name="Bx">The t coefficient of the x-polynomial.</param>
        /// <param name="Cx">The constant (t^0) coefficient of the x-polynoimal.</param>        
        /// <param name="Ay">The t^2 coefficient of the y-polynomial.</param>
        /// <param name="By">The t^1 coefficient of the y-polynomial.</param>
        /// <param name="Cy">The constant (t^0) coefficient of the y-polynoimal.</param>        
        public static BezierQuadratic FromPolynomials(double Ax, double Bx, double Cx, 
                                                      double Ay, double By, double Cy)
        {

            Point start = new Point(Cx, Cy);
            Point control = new Point((Bx / 2) + Cx, (By / 2) + Cy);
            Point end = new Point(Ax + Bx + Cx, Ay + By + Cy);
            return new BezierQuadratic(start, control, end);
        }
        /// <summary>
        /// Returns a Bezier specified by the two given polynomials.  Note that the polynomials themselves are not cached, so future 
        /// calls to this Bezier to retrieve the polynomials will generate them anew.
        /// </summary>
        public static BezierQuadratic FromPolynomials(Polynomial xPolynomial, Polynomial yPolynomial)
        {
            return FromPolynomials(xPolynomial.A, xPolynomial.B, xPolynomial.C, yPolynomial.A, yPolynomial.B, yPolynomial.C);
        }


        /// <summary>
        /// Returns the quadratic Polynomial that describes the x-traversal of this Bezier curve.
        /// </summary>        
        public Polynomial GetXPolynomial()
        {
            return Polynomial.FromQuadratic(End.X - (2 * Control.X) + Start.X, 2 * (Control.X - Start.X), Start.X);
        }

        /// <summary>
        /// Returns the quadratic Polynomial that describes the y-traversal of this Bezier curve.
        /// </summary>        
        public Polynomial GetYPolynomial()
        {
            return Polynomial.FromQuadratic(End.Y - (2 * Control.Y) + Start.Y, 2 * (Control.Y - Start.Y), Start.Y);
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

        IDifferentiable<Point> IDifferentiable<Point>.GetDerivative()
        {
            Point point0 = new Point(2 * (Control.X - Start.X), 2 * (Control.Y - Start.Y));
            Vector vec = new Vector(2 * (End.X - (2 * Control.X) + Start.X), 2 * (End.Y - (2 * Control.Y) + Start.Y));
            return new Line(point0, point0 + vec);            
        }

        IDifferentiable<Point> IDifferentiable<Point>.GetIntegral(double constant)
        {
            throw new NotImplementedException("The integral of a quadratic bezier would be a cubic bezier. Have not yet "
                                              + " implemented this method.");
        }

    

        double IDifferentiable<Point>.GetLength(double starting, double ending)
        {
            throw new NotImplementedException();
        }
    }
}
