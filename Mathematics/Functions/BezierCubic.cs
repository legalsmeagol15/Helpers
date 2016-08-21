
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
    /// Represents a cubic Bezier curve, which is a curve defined by a start and end point, and two control points.  The first control point 
    /// determines the direction the curve leaves the start point, and the second control point determines the direction from which the curve 
    /// enters the end point.
    /// </summary>
    public class BezierCubic : IParametric<Point>, IDifferentiable<Point>
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
        /// Returns a Bezier specified by the given polynomials, which are defined as follows:  the co-efficients of the x-polynomial are given 
        /// by Ax, Bx, Cx, and Dx, where Ax is the highest-order coefficient; and the co-efficients of the y-polynomial are similarly given by 
        /// Ay, By, Cy, and Dy.
        /// </summary>
        /// <param name="Ax">The t^3 coefficient of the x-polynomial.</param>
        /// <param name="Bx">The t^2 coefficient of the x-polynomial.</param>
        /// <param name="Cx">The t coefficient of the x-polynoimal.</param>
        /// <param name="Dx">The constant (t^0) of the x-polynomial.</param>
        /// <param name="Ay">The t^3 coefficient of the y-polynomial.</param>
        /// <param name="By">The t^2 coefficient of the y-polynomial.</param>
        /// <param name="Cy">The t coefficient of the y-polynoimal.</param>
        /// <param name="Dy">The constant (t^0) of the y-polynomial.</param>
        public static BezierCubic FromPolynomials(double Ax, double Bx, double Cx, double Dx, double Ay, double By, double Cy, double Dy)
        {
            Point start = new Point(Dx, Dy);
            Point controlA = new Point((Cx / 3) + Dx, (Cy / 3) + Dy);
            Point controlB = new Point(((Bx + (2 * Cx)) / 3) + Dx, ((By + (2 * Cy)) / 3) + Dy);
            Point end = new Point(Ax + Bx + Cx + Dx, Ay + By + Cy + Dy);
            return new BezierCubic(start, controlA, controlB, end);
        }
        /// <summary>
        /// Returns a Bezier specified by the two given polynomials.  Note that the polynomials themselves are not cached, so future 
        /// calls to this Bezier to retrieve the polynomials will generate them anew.
        /// </summary>
        public static BezierCubic FromPolynomials(Polynomial xPolynomial, Polynomial yPolynomial)
        {
            return FromPolynomials(xPolynomial.A, xPolynomial.B, xPolynomial.C, xPolynomial.D, 
                                   yPolynomial.A, yPolynomial.B, yPolynomial.C, yPolynomial.D);
        }

        /// <summary>
        /// Returns the cubic Polynomial that describes the x-traversal of this Bezier curve.
        /// </summary>        
        public Polynomial GetXPolynomial()
        {
            double p0 = Start.X, p1 = ControlA.X, p2 = ControlB.X, p3 = End.X;
            return Polynomial.FromCubic(p3 - p0 + (3 * (p1 - p2)),
                                        3 * (p2 - (2 * p1) + p0),
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
                                        3 * (p2 - (2 * p1) + p0),
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

        IDifferentiable<Point> IDifferentiable<Point>.GetDerivative()
        {
            Point A = new Point(End.X - Start.X + (3 * (ControlA.X - ControlB.X)), End.Y - Start.Y + (3 * (ControlA.Y - ControlB.Y)));
            Point B = new Point(3 * (ControlB.X + Start.X - (2 * ControlA.X)), 3 * (ControlB.Y + Start.Y - (2 * ControlA.Y)));
            Point C = new Point(3 * (ControlA.X - Start.X), 3 * (ControlA.Y - Start.Y));
            Point D = Start;

            return BezierQuadratic.FromPolynomials(A.X * 3, B.X * 2, C.X, A.Y * 3, B.Y * 2, C.Y);            
        }

        IDifferentiable<Point> IDifferentiable<Point>.GetIntegral(double constant)
        {
            throw new NotImplementedException("The integral of a cubic Bezier would be a quartic Bezier.  Have not implemented quartic Beziers.");
        }
        

        double IDifferentiable<Point>.GetLength(double starting, double ending)
        {
            throw new NotImplementedException();
        }
        
      
        
    }
}
