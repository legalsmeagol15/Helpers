using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Arithmetic
{
    /// <summary>
    /// A lightweight, publicly immutable data structure that represents polynomial expressions.
    /// </summary>
    public class Polynomial
    {

        double[] _Coeffs;


        /// <summary>
        /// Returns the order of the Polynomial, which is the one higher than the highest exponent to which the 'x' variable will be raised on evaluation.  For example, 
        /// the Polynomial y=1 will be order 0, and the Polynomial y=(x^4)+(x^2)+1 will be order 5.
        /// </summary>
        public int Order
        {
            get
            {
                return _Coeffs.Length;
            }
        }

        public override string ToString()
        {
            //Simple cases - either somehow the coefficients is empty, or there is but a single item there.
            if (_Coeffs.Length == 0) return "Error - Empty Polynomial.";
            if (_Coeffs.Length == 1) return _Coeffs[0].ToString();

            //Since the coefficeints size must be at least two, start adding the higher-order powers.
            StringBuilder sb = new StringBuilder();
            int i = _Coeffs.Length - 1;
            double current = _Coeffs[i];
            if (current < 0.0) sb.Append("-");
            while (i >= 2)
            {
                if (Math.Abs(current) == 1.0) sb.Append("x^" + i);
                else sb.Append(Math.Abs(current) + "x^" + i);

                double next = _Coeffs[--i];
                while (i >= 2 && next == 0.0) next = _Coeffs[--i];  //Empty coefficients should be skipped.
                if (next > 0.0) sb.Append(" + ");
                else if (next < 0.0) sb.Append(" - ");              

                current = next;
            }

            //The x^1 is typically presented without the exponent.
            if (_Coeffs[1] != 0.0)
            {
                if (Math.Abs(_Coeffs[1]) == 1.0) sb.Append("x");
                else sb.Append(Math.Abs(_Coeffs[1]) + "x");                
            }
            
            //Lastly, the constant.
            if (_Coeffs[0] != 0.0)
            {
                if (_Coeffs[0] > 0.0) sb.Append(" + ");
                else sb.Append(" - ");
                sb.Append(Math.Abs(_Coeffs[0]));
            }
            

            return sb.ToString();
        }


        #region Polynomial constructors


        private Polynomial() { }

        public static Polynomial FromConstant(double constant)
        {
            Polynomial p = new Polynomial();
            p._Coeffs = new double[] { constant };            
            return p;
        }

        /// <summary>
        /// Creates a polynomial of the form ax + b.
        /// </summary>
        public static Polynomial FromLinear(double a, double b)
        {
            if (a == 0.0) return FromConstant(b);
            Polynomial p = new Polynomial();
            p._Coeffs = new double[] { b, a };         
            return p;
        }

        /// <summary>
        /// Creates a polynomial of the form ax^2 + bx + c.
        /// </summary>
        public static Polynomial FromQuadratic(double a, double b, double c)
        {
            if (a == 0.0) return FromLinear(b, c);
            Polynomial p = new Polynomial();
            p._Coeffs = new double[] { c, b, a };            
            return p;
        }

        /// <summary>
        /// Creates a polynomial of the form ax^3 + bx^2 + cx + d.
        /// </summary>
        public static Polynomial FromCubic(double a, double b, double c, double d)
        {
            if (a == 0.0) return FromQuadratic(b, c, d);
            Polynomial p = new Polynomial();
            p._Coeffs = new double[] { d, c, b, a };            
            return p;
        }

        /// <summary>
        /// Creates a polynomial of the form ax^4 + bx^3 + cx^2 + dx + e.
        /// </summary>
        public static Polynomial FromQuartic(double a, double b, double c, double d, double e)
        {
            if (a == 0.0) return FromCubic(b, c, d, e);
            Polynomial p = new Polynomial();
            p._Coeffs = new double[] { e, d, c, b, a };            
            return p;
        }

        private Polynomial Copy()
        {
            Polynomial copy = new Polynomial();
            copy._Coeffs = new double[_Coeffs.Length];
            _Coeffs.CopyTo(copy._Coeffs, 0);
            return copy;
        }

        /// <summary>
        /// If the higher-order coefficients are 0's, simplifies the Polynomial coefficient array.
        /// </summary>
        /// <returns>Returns true if the simplification change the coefficients array; otherwise, returns false.</returns>
        private bool Simplify()
        {
            if (_Coeffs.Length == 1) return false;

            //What is the actual order?
            int order = _Coeffs.Length;
            for (order = _Coeffs.Length; order>0; order--)
            {
                if (_Coeffs[order-1] != 0.0) break;
            }
            if (order == _Coeffs.Length) return false;
            if (order == 0)            
                _Coeffs = new double[1] { 0.0 };            

            //Since there were some 0.0's padding the coefficient array, simplify the array.
            double[] newCoeffs = new double[order];
            for (int i = 0; i < order; i++) newCoeffs[i] = _Coeffs[i];
            _Coeffs = newCoeffs;
            return true;
        }

        #endregion




     

        #region Polynomial arithmetic

        public static Polynomial operator -(Polynomial a)
        {
            Polynomial result = new Polynomial();
            result._Coeffs = new double[a._Coeffs.Length];
            for (int i = 0; i<result._Coeffs.Length; i++)            
                result._Coeffs[i] = -a._Coeffs[i];
            return result;
        }

        /// <summary>
        /// Returns the evaluation of the polynomial at the given x-value.
        /// </summary>
        public double Evaluate(double xValue)
        {
            double result = 0.0;
            for (int exp = 0; exp < _Coeffs.Length; exp++)
                result += (_Coeffs[exp] * Math.Pow(xValue, exp));
            return result;
        }


        public static Polynomial operator +(Polynomial a, Polynomial b)
        {   
            Polynomial result = new Polynomial();
            result._Coeffs = new double[Math.Max(a._Coeffs.Length, b._Coeffs.Length)];
            for (int i = 0; i < result._Coeffs.Length; i++)
            {
                //Optimize - possibility of thrashing?
                double coeff = 0;
                if (i < a._Coeffs.Length) coeff += a._Coeffs[i];
                if (i < b._Coeffs.Length) coeff += b._Coeffs[i];
                result._Coeffs[i] = coeff;
            }
            result.Simplify();
            return result;
        }

        public static Polynomial operator +(Polynomial polynomial, double d)
        {
            Polynomial result = polynomial.Copy();
            result._Coeffs[0] += d;
            result.Simplify();
            return result;
        }

        public static Polynomial operator +(double d, Polynomial polynomial)
        {
            return polynomial + d;
        }

        public static Polynomial operator -(Polynomial a, Polynomial b)
        {
            Polynomial result = new Polynomial();
            result._Coeffs = new double[Math.Max(a._Coeffs.Length, b._Coeffs.Length)];
            for (int i = 0; i < result._Coeffs.Length; i++)
            {
                double coeff = 0;
                if (i < a._Coeffs.Length) coeff -= a._Coeffs[i];
                if (i < b._Coeffs.Length) coeff -= b._Coeffs[i];
                result._Coeffs[i] = coeff;
            }
            result.Simplify();
            return result;
        }

        public static Polynomial operator -(Polynomial polynomial, double d)
        {
            Polynomial result = polynomial.Copy();
            result._Coeffs[0] -= d;
            result.Simplify();
            return result;
        }

        public static Polynomial operator -(double d, Polynomial polynomial)
        {
            return polynomial - d;
        }


        public static Polynomial operator *(Polynomial a, Polynomial b)
        {
            Polynomial result = new Polynomial();
            result._Coeffs = new double[a._Coeffs.Length * b._Coeffs.Length];
            throw new NotImplementedException();
        }

        public static Polynomial operator *(Polynomial polynomial, double d)
        {
            Polynomial result = polynomial.Copy();
            for (int i = 0; i < result._Coeffs.Length; i++)            
                result._Coeffs[i] *= d;
            result.Simplify();
            return result;
        }

        public static Polynomial operator *(double d, Polynomial polynomial)
        {
            return polynomial * d;
        }

        public static void Divide (Polynomial a, Polynomial b, out Polynomial quotient, out Polynomial remainder)
        {
            throw new NotImplementedException();
        }

        public static Polynomial operator /(Polynomial a, Polynomial b)
        {
            Polynomial quotient, remainder;
            Divide(a, b, out quotient, out remainder);
            quotient.Simplify();
            return quotient;
        }
        public static Polynomial operator /(Polynomial polynomial, double d)
        {
            Polynomial result = polynomial.Copy();
            for (int i = 0; i < result._Coeffs.Length; i++)            
                result._Coeffs[i] /= d;
            return result;            
        }
        public static Polynomial operator %(Polynomial a, Polynomial b)
        {
            Polynomial quotient, remainder;
            Divide(a, b, out quotient, out remainder);
            remainder.Simplify();
            return remainder;
        }
    
        public static Polynomial operator /(double d, Polynomial polynomial)
        {
            if (polynomial._Coeffs.Length == 1) return d / polynomial._Coeffs[0];
            return d;
        }
        /// <summary>
        /// If the polynomial is but a constant, converts it to a double.  Otherwise, throws an exception.
        /// </summary>
        /// <param name="polynomial"></param>
        public static explicit operator double(Polynomial polynomial)
        {
            if (polynomial._Coeffs.Length == 1) return polynomial._Coeffs[0];
            throw new InvalidCastException("Cannot cast non-constant polynomial to a double.");            
        }
        /// <summary>
        /// Creates and returns a constant Polynomial from the given double.
        /// </summary>
        public static implicit operator Polynomial(double d)
        {
            Polynomial p = new Polynomial();
            if (d != 0.0) p._Coeffs = new double[1] { d };
            else p._Coeffs = new double[0];
            return p;
        }

        #endregion



        #region Polynomial calculus members

        /// <summary>
        /// Returns a Polynomial describing the derivative of this Polynomial.
        /// </summary>
        /// <returns></returns>
        public Polynomial GetDerivative()
        {
            Polynomial deriv = new Polynomial();
            deriv._Coeffs = new double[_Coeffs.Length - 1];
            for (int exp = 1; exp < _Coeffs.Length; exp++)            
                deriv._Coeffs[exp - 1] = _Coeffs[exp] * exp;
            
            return deriv;
        }

        /// <summary>
        /// Returns a Polynomial describing the integral of this Polynomial.
        /// </summary>
        /// <param name="constant">Optional.  The constant (x^0) added to the integral Polynomial.  If omitted, the x^0 amount will be 0.</param>
        public Polynomial GetIntegral(double constant = 0.0)
        {
            Polynomial integ = new Polynomial();
            integ._Coeffs = new double[_Coeffs.Length + 1];
            for  (int exp= 0; exp < _Coeffs.Length; exp++)            
                integ._Coeffs[exp + 1] = _Coeffs[exp] / (exp+1);
            integ._Coeffs[0] = constant;
            return integ;            
        }

        #endregion



        #region Polynomial root finding


        /// <summary>
        /// Returns the roots of this Polynomial, or in other words, the values of 'x' when descending exponents of 'x' times the respective coefficients are set equal to 
        /// 0.
        /// </summary>        
        public Complex[] GetRoots()
        {
            switch (_Coeffs.Length)
            {
                case 0: throw new ArithmeticException("There are no roots for a Polynomial with no coefficients.");
                case 1: throw new ArithmeticException("There are no roots for a Polynomial with a single coefficient.");
                case 2: return GetRoots(_Coeffs[1], _Coeffs[0]);
                case 3: return GetRoots(_Coeffs[2], _Coeffs[1], _Coeffs[0]);
                case 4: return GetRoots(_Coeffs[3], _Coeffs[2], _Coeffs[1], _Coeffs[0]);
                case 5: return GetRoots(_Coeffs[4], _Coeffs[3], _Coeffs[2], _Coeffs[1], _Coeffs[0]);
                default:
                    throw new NotImplementedException("Abel-Ruffini theorem holds it is impossible to find the roots of a quintic Polynomial or higher algebraically.");
            }
        }

      
        /// <summary>
        /// Finds the root of the given linear expression, in the form ax + b.
        /// </summary>        
        /// <param name="a">The coefficient of the linear term in the polynomial.</param>
        /// <param name="b">The constant term in the polynomial.</param> 
        public static Complex[] GetRoots(double a, double b)
        {
            if (a == 0)
            {
                if (b == 0) return new Complex[1] { 0 };
                return new Complex[0];
            }
            Complex[] result = new Complex[1];
            result[0] = -b / a;
            return result;
        }

        /// <summary>
        /// Finds the roots of the given quadratic expression, in the form (ax^2) + (bx) + c.
        /// </summary>        
        /// <param name="a">The coefficient of the squared term in the polynomial.</param>
        /// <param name="b">The coefficient of the linear term in the polynomial.</param>
        /// <param name="c">The constant term in the polynomial.</param>        
        public static Complex[] GetRoots(double a, double b, double c)
        {
            if (a == 0) return GetRoots(b, c);
            double discrim = (b * b) - (4 * a * c);
            double aTimes2 = 2 * a;
            if (discrim > 0)
            {
                //If the discrim  >0, there are two real roots.
                discrim = Math.Sqrt(discrim);
                return new Complex[2] { (-b - discrim) / aTimes2, (-b + discrim) / aTimes2 };
            }
            else if (discrim < 0)
            {
                // If the discrim < 0, it means there are two imaginary roots.
                Complex complexDiscrim = Complex.Sqrt(discrim);
                return new Complex[2] { (-b - complexDiscrim) / aTimes2, (-b + complexDiscrim) / aTimes2 };
            }
            else
            {
                // If the discrim==0, then there is a multi-root (2 roots of identical value).
                return new Complex[2] { -b / aTimes2, -b / aTimes2 };
            }
                
        }

        /// <summary>
        /// Finds the roots of the given cubic expression, in the form (ax^3) + (bx^2) + (cx) + d.
        /// </summary>
        /// <param name="a">The coefficient of the cubed term in the polynomial.</param>
        /// <param name="b">The coefficient of the squared term in the polynomial.</param>
        /// <param name="c">The coefficient of the linear term in the polynomial.</param>
        /// <param name="d">The constant term in the polynomial.</param>     
        /// <remarks>This method is implemented based on the presentation of Cardano's method for solving cubic equations, found at:
        /// https://brilliant.org/wiki/cardano-method/.  Note that the way the discriminant is presented in sources like 
        /// https://en.wikipedia.org/wiki/Discriminant is actually sign flipped from the easiest means of calculation, so the discriminant tests will be sign-flipped 
        /// from what would appear there.</remarks>
        public static Complex[] GetRootsA(double a, double b, double c, double d)
        {

            double Q = ((3 * a * c) - (b * b)) / (9 * a * a);
            double R = ((9 * a * b * c) - (27 * a * a * d) - (2 * b * b * b)) / (54 * a * a * a);
            double discrim = (Q * Q * Q) + (R * R);
            double bOver3a = b / (3 * a);
            if (discrim > 0)
            {
                //1 real and 2 imaginary roots.
                double sqRtDiscrim = Math.Sqrt(discrim);
                Complex S = Complex.Pow(R + sqRtDiscrim, Arithmetic.OneThird);
                Complex T = Complex.Pow(R - sqRtDiscrim, Arithmetic.OneThird);
                Complex x1 = S + T - bOver3a;                
                Complex x2 = -((S + T) / 2) - bOver3a + ((S - T) * new Complex(0, Math.Sqrt(3) / 2));
                Complex x3 = -((S + T) / 2) - bOver3a - ((S - T) * new Complex(0, Math.Sqrt(3) / 2));

                return new Complex[3] { x1, x2, x3 };
            }
            else
            {
                //All three roots are real.

                Complex sqRtDiscrim = Complex.Sqrt(discrim);
                Complex S = Complex.Pow(R + sqRtDiscrim, Arithmetic.OneThird);
                Complex T = Complex.Pow(R - sqRtDiscrim, Arithmetic.OneThird);
                Complex x1 = S + T - (b / (3 * a));
                Complex x2 = -((S + T) / 2) - bOver3a + ((S - T) * new Complex(0, Math.Sqrt(3) / 2));
                Complex x3 = -((S + T) / 2) - bOver3a - ((S - T) * new Complex(0, Math.Sqrt(3) / 2));

                return new Complex[3] { new Complex(x1.Real, 0), new Complex(x2.Real, 0), new Complex(x3.Real, 0) };
            }
            
        }


        /// <summary>
        /// Finds the roots of the given cubic expression, in the form (ax^3) + (bx^2) + (cx) + d.
        /// </summary>
        /// <param name="a">The coefficient of the cubed term in the polynomial.</param>
        /// <param name="b">The coefficient of the squared term in the polynomial.</param>
        /// <param name="c">The coefficient of the linear term in the polynomial.</param>
        /// <param name="d">The constant term in the polynomial.</param>     
        /// <remarks>This method is implemented based on the presentation of Cardano's method for solving cubic equations, found at:
        /// https://brilliant.org/wiki/cardano-method/.  Note that the way the discriminant is presented in sources like 
        /// https://en.wikipedia.org/wiki/Discriminant is actually sign flipped from the easiest means of calculation, so the discriminant tests will be sign-flipped 
        /// from what would appear there.</remarks>
        public static Complex[] GetRoots(double a, double b, double c, double d)
        {
            double f, g, discrim;
            double bSquared = b * b;
            double aSquared = a * a;
            f = ((3 * c / a) - (bSquared / aSquared)) / 3;
            g = (((2 * bSquared * b) / (aSquared * a)) - (9 * b * c / aSquared) + (27 * d / a)) / 27;
            discrim = ((g * g) / 4) + ((f * f * f) / 27);

            if (discrim==0 && f==0 && g == 0)
            {
                /// A single multi-root.  An exceedingly rare edge case. 
                double result = -Math.Pow(d / a, Arithmetic.OneThird);
                return new Complex[3] { result, result, result };
            }
            else if (discrim >= 0)
            {
                double r, rCubeRoot=0, t, tCubeRt=0, bOver3a;

                r = -(g / 2) + Math.Pow(discrim, 0.5);                
                //Get the singular real cube root of r, and store it in s.
                Complex[] roots = Arithmetic.NthRoots(r, 3); 
                double smallestImaginary = double.PositiveInfinity;
                foreach (Complex root in roots)
                {
                    if (Math.Abs(root.Imaginary) < smallestImaginary)
                    {
                        smallestImaginary = Math.Abs(root.Imaginary);
                        rCubeRoot = root.Real;
                    }
                }


                t = (-g / 2) - Math.Pow(discrim, 0.5);
                //Get the singular real cube root of t, and store it in u.
                roots = Arithmetic.NthRoots(t, 3);
                smallestImaginary = double.PositiveInfinity;
                foreach (Complex root in roots)
                {
                    if (Math.Abs(root.Imaginary) < smallestImaginary)
                    {
                        smallestImaginary = Math.Abs(root.Imaginary);
                        tCubeRt = root.Real;
                    }
                }
                
                bOver3a = -(b / (3 * a));
                Complex x1 = new Complex( rCubeRoot + tCubeRt + bOver3a,0);
                Complex x2 = new Complex((-(rCubeRoot + tCubeRt) / 2) + bOver3a, (rCubeRoot - tCubeRt) * Arithmetic.SqRt3 / 2);
                Complex x3 = new Complex((-(rCubeRoot + tCubeRt) / 2) + bOver3a, -(rCubeRoot - tCubeRt) * Arithmetic.SqRt3 / 2);
                return new Complex[3] { x1, x2, x3 };
            }
            
            else
            {
                //Three real roots.
                double i, iCubeRt, k, m, n, bOver3a;
                i = Math.Pow((g * g / 4) - discrim, 0.5);
                iCubeRt = Math.Pow(i, Arithmetic.OneThird);
                k = Math.Acos(-(g / (2 * i)));

                //Use trig to find the other two roots.
                double kOneThird = k/3;
                m = Math.Cos(kOneThird);
                n = Arithmetic.SqRt3 * Math.Sin(kOneThird);

                bOver3a = -(b / (3 * a));
                double x1 = (2 * iCubeRt * m) + bOver3a;
                double x2 = (-iCubeRt * (m + n)) + bOver3a;
                double x3 = (-iCubeRt * (m - n)) + bOver3a;

                return new Complex[3] { x1, x2, x3 };
            }
            
        }




        /// <summary>
        /// Finds the roots of the given cubic expression, in the form (ax^3) + (bx^2) + (cx) + d.
        /// </summary>
        /// <param name="a">The coefficient of the fourth-power term in the polynomial.</param>
        /// <param name="b">The coefficient of the cubed term in the polynomial.</param>
        /// <param name="c">The coefficient of the squared term in the polynomial.</param>
        /// <param name="d">The coefficient of the linear term in the polynomial.</param>
        /// <param name="e">The constant term in the polynomial.</param>
        public static Complex[] GetRoots(double a, double b, double c, double d, double e)
        {
            if (a == 0) return GetRoots(b, c, d, e);
            throw new NotImplementedException("Have not implemented root-finding for quartic polynomial expressions.  Good luck, mate.");
        }

        #endregion

    }
}
