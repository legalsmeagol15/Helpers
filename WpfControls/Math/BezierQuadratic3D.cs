using Mathematics.Calculus;
using Mathematics.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace WpfHelpers.Math
{
    public class BezierQuadratic3D : IParametric<Point3D>, IDifferentiable<Point3D>
    {
        /// <summary>
        /// The starting anchor Point3D of the Bezier curve, or in other words, the Point3D where t=0.
        /// </summary>
        public Point3D Start { get; }
        /// <summary>
        /// The control Point3D that the curve approaches towards at t=0, and enters from at t=1.
        /// </summary>
        public Point3D Control { get; }
        /// <summary>
        /// The ending anchor Point3D of the Bezier curve, or in other words, the Point3D where t=1.
        /// </summary>
        public Point3D End { get; }

        /// <summary>
        /// Creates a new quadratic Bezier curve.
        /// </summary>
        public BezierQuadratic3D(Point3D start, Point3D control, Point3D end)
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
        /// <param name="Az">The t^2 coefficient of the z-polynomial.</param>
        /// <param name="Bz">The t coefficient of the z-polynomial.</param>
        /// <param name="Cz">The constant (t^0) coefficient of the z-polynoimal.</param>  
        public static BezierQuadratic3D FromPolynomials(double Ax, double Bx, double Cx,
                                                        double Ay, double By, double Cy,
                                                        double Az, double Bz, double Cz)
        {

            Point3D start = new Point3D(Cx, Cy, Cz);
            Point3D control = new Point3D((Bx / 2) + Cx, (By / 2) + Cy, (Bz / 2) + Cz);
            Point3D end = new Point3D(Ax + Bx + Cx, Ay + By + Cy, Az + Bz + Cz);
            return new BezierQuadratic3D(start, control, end);
        }
        /// <summary>
        /// Returns a Bezier specified by the two given polynomials.  Note that the polynomials themselves are not cached, so future 
        /// calls to this Bezier to retrieve the polynomials will generate them anew.
        /// </summary>
        public static BezierQuadratic3D FromPolynomials(Polynomial xPolynomial, Polynomial yPolynomial, Polynomial zPolynomial)
        {
            return FromPolynomials(xPolynomial.A, xPolynomial.B, xPolynomial.C,
                                   yPolynomial.A, yPolynomial.B, yPolynomial.C,
                                   zPolynomial.A, zPolynomial.B, zPolynomial.C);
        }


       
        /// <summary>
        /// Returns the Point3D specified by the given traversal value from 0 to 1.
        /// </summary>   
        public Point3D Evaluate(double t)
        {
            double tSquared = t * t;
            double tTimes2 = 2 * t;
            double x = (tSquared * (Start.X - (2 * Control.X) + End.X) + (tTimes2 * (Control.X - Start.X)) + Start.X);
            double y = (tSquared * (Start.Y - (2 * Control.Y) + End.Y) + (tTimes2 * (Control.Y - Start.Y)) + Start.Y);
            double z = (tSquared * (Start.Z - (2 * Control.Z) + End.Z) + (tTimes2 * (Control.Z - Start.Z)) + Start.Z);
            return new Point3D(x, y, z);
        }

        IDifferentiable<Point3D> IDifferentiable<Point3D>.GetDerivative()
        {
            Point3D point0 = new Point3D(2 * (Control.X - Start.X), 2 * (Control.Y - Start.Y), 2 * (Control.Z - Start.Z));
            Vector3D vec = new Vector3D(2 * (End.X - (2 * Control.X) + Start.X), 
                                        2 * (End.Y - (2 * Control.Y) + Start.Y), 
                                        2 * (End.Z - (2 * Control.Z) + Start.Z));
            //return new Line(point0, point0 + vec);

            throw new NotImplementedException("The derivative of a quadratic bezier would be a linear bezier, or a line. "
                                              + " Have not yet implemented this method.");
        }

        IDifferentiable<Point3D> IDifferentiable<Point3D>.GetIntegral(double constant)
        {
            throw new NotImplementedException("The integral of a quadratic bezier would be a cubic bezier. Have not yet "
                                              + " implemented this method.");
        }

      

        double IDifferentiable<Point3D>.GetLength(double starting, double ending)
        {
            throw new NotImplementedException();
        }
    }
}
