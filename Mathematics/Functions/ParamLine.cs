using Mathematics.Calculus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Mathematics.Functions
{
    public class ParamLine : IParametric<Point>
    {

        public Point PointA { get; }
        public Point  PointB { get; }

        public double Slope { get { return (PointB.Y - PointA.Y) / (PointB.X - PointA.X); } }
        public double YIntercept
        {
            get
            {                                
                return PointA.Y - (Slope * PointA.X);
            }
        }
        public double XIntercept
        {
            get
            {
                return PointA.X - (PointA.Y / Slope);
            }
        }

        public ParamLine(Point pointA, Point pointB)
        {
            this.PointA = pointA;
            this.PointB = pointB;
        }

        public Point Evaluate(double t)
        {
            double x = PointA.X + ((PointB.X - PointA.X) * t);
            double y = PointA.Y + ((PointB.Y - PointA.Y) * t);
            return new Point(x, y);
        }
        
        
        /// <summary>
        /// Returns a constant polynomial describing the derivative (slope) of this line.  If the line is vertical, throws a divide-by-zero exception.
        /// </summary>        
        public IDifferentiable GetDerivative()
        {            
            return Polynomial.FromConstant(Slope);
        }

        /// <summary>
        /// Returns a quadratic polynomial describing the integral of this line.  If the line is vertical, throws a divide-by-zero exception.
        /// </summary>
        /// <param name="constant"></param>
        /// <returns></returns>
        public IDifferentiable GetIntegral(double constant = 0)
        {
            return Polynomial.FromQuadratic(Slope / 2, YIntercept, constant);
        }
        
    }
}
