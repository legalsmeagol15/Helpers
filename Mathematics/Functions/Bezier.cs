using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Arithmetic;
using System.Windows;

namespace Calculus
{

    /// <summary>
    /// An immutable data structure describing 
    /// </summary>
    public class Bezier : IFunction<Point>
        //TODO:  Validate m-order bazier
    {
        private Point[] _Points;

        public Point Point0 { get { return _Points[0]; } }
        public Point Point1 { get { return _Points[1]; } }
        public Point Point2 { get { return _Points[2]; } }
        public Point Point3 { get { return _Points[3]; } }
        

        private Bezier() { }

        /// <summary>
        /// Creates a single-point Bezier curve.  Why, this is hardly a Bezier curve at all!
        /// </summary>
        public static Bezier FromConstant(Point pt)
        {
            Bezier newBezier = new Bezier();
            newBezier._Points = new Point[1] { pt };
            return newBezier;
        }
        /// <summary>
        /// Creates a two-point Bezier curve.  Lines aren't very Bezier-y, but you can create them this way.
        /// </summary>
        public static Bezier FromLinear(Point ptA, Point ptB)
        {
            Bezier newBezier = new Bezier();
            newBezier._Points = new Point[2] { ptA, ptB };
            return newBezier;
        }
        /// <summary>
        /// Creates a quadratic Bezier curve from ptA to ptC, with ptB specifying the control attraction point.
        /// </summary>
        public static Bezier FromQuadratic(Point ptA, Point ptB, Point ptC)
        {
            Bezier newBezier = new Bezier();
            newBezier._Points = new Point[3] { ptA, ptB, ptC };
            return newBezier;
        }
        /// <summary>
        /// Creates a cubic Bezier curve from ptA to ptD, with ptB and ptC specifying the control attraction points.
        /// </summary>
        public static Bezier FromCubic(Point ptA, Point ptB, Point ptC, Point ptD)
        {
            Bezier newBezier = new Bezier();
            newBezier._Points = new Point[4] { ptA, ptB, ptC, ptD };
            return newBezier;

        }

        /// <summary>
        /// Since the x- and y- polynomials are likely to be used extensively for their calculus functions, cache them.
        /// </summary>
        private Polynomial _XPoly = null, _YPoly = null, _XDeriv = null, _YDeriv = null;

        /// <summary>
        /// Returns the cached x-polynomial that describes this Bezier curve.
        /// </summary>        
        public Polynomial GetXPolynomial()
        {
            if (_XPoly != null) return _XPoly;

            switch (_Points.Length)
            {
                case 1:
                    _XPoly = Polynomial.FromConstant(_Points[0].X);
                    return _XPoly;
                case 2:
                    _XPoly = Polynomial.FromLinear(_Points[1].X - _Points[0].X, _Points[0].X);
                    return _XPoly;
                case 3:
                    double p0 = _Points[0].X;
                    double p1 = _Points[1].X;
                    double p2 = _Points[2].X;
                    _XPoly = Polynomial.FromQuadratic(p0 - (2 * p1) + p2, 2 * (p1 - p0), p0);
                    return _XPoly;
                case 4:
                    double P0 = _Points[0].X;
                    double P0_x_3 = P0 * 3;
                    double P1_x_3 = _Points[1].X * 3;
                    double p2_x_3 = _Points[2].X * 3;

                    _XPoly = Polynomial.FromCubic(-P0 - P1_x_3 - p2_x_3 - _Points[3].X,
                                                  P0_x_3 - (2 * P1_x_3) + p2_x_3,
                                                  -P0_x_3 + P1_x_3,
                                                  P0);
                    return _XPoly;
                default:
                    throw new NotImplementedException("Have not implemented polynomial finding for Bezier curves of order higher than cubic.");
            }            
        }

        /// <summary>
        /// Returns the cached y-polynomial that describes this Bezier curve.
        /// </summary>        
        public Polynomial GetYPolynomial()
        {
            if (_YPoly != null) return _YPoly;

            switch (_Points.Length)
            {
                case 1:
                    _YPoly = Polynomial.FromConstant(_Points[0].Y);
                    return _YPoly;
                case 2:
                    _YPoly = Polynomial.FromLinear(_Points[1].Y - _Points[0].Y, _Points[0].Y);
                    return _YPoly;
                case 3:
                    double p0 = _Points[0].Y;
                    double p1 = _Points[1].Y;
                    double p2 = _Points[2].Y;
                    _YPoly = Polynomial.FromQuadratic(p0 - (2 * p1) + p2, 2 * (p1 - p0), p0);
                    return _YPoly;
                case 4:
                    double P0 = _Points[0].Y;
                    double P0_x_3 = P0 * 3;
                    double P1_x_3 = _Points[1].Y * 3;
                    double p2_x_3 = _Points[2].Y * 3;

                    _YPoly = Polynomial.FromCubic(-P0 - P1_x_3 - p2_x_3 - _Points[3].Y,
                                                  P0_x_3 - (2 * P1_x_3) + p2_x_3,
                                                  -P0_x_3 + P1_x_3,
                                                  P0);
                    return _YPoly;
                default:
                    throw new NotImplementedException("Have not implemented polynomial finding for Bezier curves of order higher than cubic.");
            }
        }

        /// <summary>
        /// Returns a Polynomial describing the x-value derivative of the Bezier curve.
        /// </summary>        
        public Polynomial GetXDerivative()
        {
            if (_XDeriv != null) return _XDeriv;

            else if (_Points.Length == 1)            
                _XDeriv = Polynomial.FromConstant(0);
            
            else if (_Points.Length == 2)
                _XDeriv= Polynomial.FromConstant(_Points[1].X - _Points[0].X);
            
            else if (_Points.Length == 3)
            {
                double P0_x_2 = _Points[0].X * 2;
                double P1_x_2 = _Points[1].X * 2;
                _XDeriv = Polynomial.FromLinear(P0_x_2 - (2 * P1_x_2) + (2 * _Points[2].X), P1_x_2 - P0_x_2);

            }
            else if (_Points.Length == 4)
            {
                double P0_x_3 = _Points[0].X * 3;
                double P1_x_3 = _Points[1].X * 3;
                _XDeriv = Polynomial.FromQuadratic(-P0_x_3 - P1_x_3 - (9 * _Points[2].X) + (3 * _Points[3].X), 
                                                   (2 * P0_x_3) - (4 * P1_x_3) + (6 * _Points[2].X), 
                                                   P1_x_3 - P0_x_3);
            }
            else            
                throw new NotImplementedException("Have not implemented derivative finding for Bezier curve of order " + _Points.Length + ".");

            return _XDeriv;
        }

        /// <summary>
        /// Returns a Polynomial describing the y-value derivative of the Bezier curve.
        /// </summary> 
        public Polynomial GetYDerivative()
        {
            if (_YDeriv != null) return _YDeriv;

            else if (_Points.Length == 1)
                _YDeriv = Polynomial.FromConstant(0);

            else if (_Points.Length == 2)
                _YDeriv = Polynomial.FromConstant(_Points[1].Y - _Points[0].Y);

            else if (_Points.Length == 3)
            {
                double P0_x_2 = _Points[0].Y * 2;
                double P1_x_2 = _Points[1].Y * 2;
                _YDeriv = Polynomial.FromLinear(P0_x_2 - (2 * P1_x_2) + (2 * _Points[2].Y), P1_x_2 - P0_x_2);

            }
            else if (_Points.Length == 4)
            {
                double P0_x_3 = _Points[0].Y * 3;
                double P1_x_3 = _Points[1].Y * 3;
                _YDeriv = Polynomial.FromQuadratic(-P0_x_3 - P1_x_3 - (9 * _Points[2].Y) + (3 * _Points[3].Y),
                                                   (2 * P0_x_3) - (4 * P1_x_3) + (6 * _Points[2].Y),
                                                   P1_x_3 - P0_x_3);
            }
            else
                throw new NotImplementedException("Have not implemented derivative finding for Bezier curve of order " + _Points.Length + ".");

            return _YDeriv;
        }



        /// <summary>
        /// Returns the point on this bezier somewhere within 0..t..1.
        /// </summary>
        /// <param name="t">The parameterized fraction of the distance between the start and end points.</param>        
        public Point Evaluate(double t)
        {
            
            //Set up arrays specifying the 't' term an its inverse at different 'i' levels, to avoid the expense of raising t^2, t^3, etc.
            double[] tTerms = new double[_Points.Length];
            double[] tInverseTerms = new double[_Points.Length];            
            double notT = 1 - t;
            double tTerm = 1;
            double tInverse = 1;
            for (int i = 0; i < tTerms.Length; i++)
            {
                tTerms[i] = tTerm;
                tInverseTerms[tInverseTerms.Length - 1 - i] = tInverse;
                tTerm *= t;
                tInverse *= notT;
            }

            //Sum up the different terms.
            double resultX = 0, resultY = 0;
            for (int i = 0; i<tTerms.Length; i++)
            {
                int ncr = Operations.NCR(tTerms.Length, i);
                double termT = tTerms[i];
                double termInv = tInverseTerms[i];
                resultX += ncr * termT * termInv * _Points[i].X;
                resultY += ncr * termT * termInv * _Points[i].Y;
            }
            return new Point(resultX, resultY);
            
            
        }
    }
}
