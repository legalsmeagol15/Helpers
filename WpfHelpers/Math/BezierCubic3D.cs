using Mathematics.Functions;
using Mathematics.Calculus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Media.Media3D;

namespace WpfHelpers.Math
{
    public class BezierCubic3D : IDifferentiable<Point3D>
        //TODO:  validate BezierCubic3D
    {


        /// <summary>
        /// The starting anchor Point3D of the Bezier curve, or in other words, the Point3D where t=0.
        /// </summary>
        public Point3D Start { get; }
        /// <summary>
        /// The control Point3D that the curve approaches towards at t=0.
        /// </summary>
        public Point3D ControlA { get; }
        /// <summary>
        /// The control Point3D that the curve approaches from at t=1.
        /// </summary>
        public Point3D ControlB { get; }
        /// <summary>
        /// The ending anchor Point3D of the Bezier curve, or in other words, the Point3D where t=1.
        /// </summary>
        public Point3D End { get; }

        /// <summary>
        /// Creates a new quadratic Bezier curve.
        /// </summary>
        public BezierCubic3D(Point3D start, Point3D controlA, Point3D controlB, Point3D end)
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
        /// <param name="Az">The t^3 coefficient of the z-polynomial.</param>
        /// <param name="Bz">The t^2 coefficient of the z-polynomial.</param>
        /// <param name="Cz">The t coefficient of the z-polynoimal.</param>
        /// <param name="Dz">The constant (t^0) of the z-polynomial.</param>
        public static BezierCubic3D FromPolynomials(double Ax, double Bx, double Cx, double Dx,
                                                  double Ay, double By, double Cy, double Dy,
                                                  double Az, double Bz, double Cz, double Dz)
        {
            Point3D start = new Point3D(Dx, Dy, Dz);
            Point3D controlA = new Point3D((Cx / 3) + Dx, (Cy / 3) + Dy, (Cz / 3) + Dz);
            Point3D controlB = new Point3D(((Bx + (2 * Cx)) / 3) + Dx, ((By + (2 * Cy)) / 3) + Dy, ((Bz + (2 * Cz)) / 3) + Dz);
            Point3D end = new Point3D(Ax + Bx + Cx + Dx, Ay + By + Cy + Dy, Az + Bz + Cz + Dz);
            return new BezierCubic3D(start, controlA, controlB, end);
        }
        /// <summary>
        /// Returns a Bezier specified by the two given polynomials.  Note that the polynomials themselves are not cached, so future 
        /// calls to this Bezier to retrieve the polynomials will generate them anew.
        /// </summary>
        public static BezierCubic3D FromPolynomials(Polynomial xPolynomial, Polynomial yPolynomial, Polynomial zPolynomial)
        {
            return FromPolynomials(xPolynomial.A, xPolynomial.B, xPolynomial.C, xPolynomial.D,
                                   yPolynomial.A, yPolynomial.B, yPolynomial.C, yPolynomial.D,
                                   zPolynomial.A, zPolynomial.B, zPolynomial.C, zPolynomial.D);
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
        /// Returns the cubic Polynomial that describes the z-traversal of this Bezier curve.
        /// </summary>        
        public Polynomial GetZPolynomial()
        {
            double p0 = Start.Z, p1 = ControlA.Z, p2 = ControlB.Z, p3 = End.Z;
            return Polynomial.FromCubic(p3 - p0 + (3 * (p1 - p2)),
                                        3 * (p2 - (2 * p1) + p0),
                                        3 * (p1 - p0),
                                        p0);
        }

        /// <summary>
        /// Returns the Point3D specified by the given traversal value from 0 to 1.
        /// </summary>   
        public Point3D Evaluate(double t)
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

            p0 = Start.Z;
            p1 = ControlA.Z;
            p2 = ControlB.Z;
            p3 = End.Z;
            double z = (tCubed * (p3 - p0 + (3 * (p1 - p2))))
                     + (tSquared * 3 * (p0 - (2 * p1) + p2))
                     + (t * 3 * (p1 - p0))
                     + p0;

            return new Point3D(x, y, z);
        }

        IDifferentiable<Point3D> IDifferentiable<Point3D>.GetDerivative()
        {
            Point3D A = new Point3D(End.X - Start.X + (3 * (ControlA.X - ControlB.X)),
                                    End.Y - Start.Y + (3 * (ControlA.Y - ControlB.Y)),
                                    End.Z - Start.Z + (3 * (ControlA.Z - ControlB.Z)));
            Point3D B = new Point3D(3 * (ControlB.X + Start.X - (2 * ControlA.X)),
                                    3 * (ControlB.Y + Start.Y - (2 * ControlA.Y)),
                                    3 * (ControlB.Z + Start.Z - (2 * ControlA.Z)));
            Point3D C = new Point3D(3 * (ControlA.X - Start.X), 3 * (ControlA.X - Start.X), 3 * (ControlA.X - Start.X));
            Point3D D = Start;

            return BezierQuadratic3D.FromPolynomials(3 * A.X, 2 * B.X, C.X, 3 * A.Y, 2 * B.Y, C.Y, 3 * A.Z, 2 * B.Z, C.Z);

        }

        IDifferentiable<Point3D> IDifferentiable<Point3D>.GetIntegral(double constant)
        {
            throw new NotImplementedException("The integral of a cubic Bezier would be a quartic Bezier.  Have not implemented quartic Beziers.");
        }

        
        public IDifferentiable<Point3D> GetLength()
        {
            throw new NotImplementedException();
        }

        Point3D IDifferentiable<Point3D>.Evaluate(double value)
        {
            throw new NotImplementedException();
        }

        IDifferentiable<Point3D> IDifferentiable<Point3D>.GetLength()
        {
            throw new NotImplementedException();
        }

        IDifferentiable<Point3D> IDifferentiable<Point3D>.GetSum(IDifferentiable<Point3D> other)
        {
            throw new NotImplementedException();
        }

        IDifferentiable<Point3D> IDifferentiable<Point3D>.GetDifference(IDifferentiable<Point3D> other)
        {
            throw new NotImplementedException();
        }

        IDifferentiable<Point3D> IDifferentiable<Point3D>.GetMultiple(IDifferentiable<Point3D> factor)
        {
            throw new NotImplementedException();
        }

        IDifferentiable<Point3D> IDifferentiable<Point3D>.GetQuotient(IDifferentiable<Point3D> divisor)
        {
            throw new NotImplementedException();
        }

        IDifferentiable<Point3D> IDifferentiable<Point3D>.GetNegation()
        {
            throw new NotImplementedException();
        }
    }
}
