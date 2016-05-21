using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using Calculus;
using Arithmetic;

namespace Calculus
{
    /// <summary>
    /// A lightweight, publicly immutable data structure that represents polynomial expressions.
    /// </summary>
    public class Polynomial : Calculus.IDifferentiable
    {

        double[] _Coeffs;


        /// <summary>
        /// Returns the order of the Polynomial, which is the one higher than the highest exponent to which the 'x' variable will be raised on evaluation.  For example, 
        /// the Polynomial y=1 will be order 0, and the Polynomial y=(x^4)+(x^2)+1 will be order 5.
        /// </summary>
        public int Order { get { return _Coeffs.Length; } }


        public override string ToString()
        {
            //Simple cases - either somehow the coefficients is empty, or there is but a single item there.
            if (_Coeffs.Length == 0) return "Error - Empty Polynomial.";
            if (_Coeffs.Length == 1) return _Coeffs[0].ToString();

            //Since the coefficients size must be at least two, start adding the higher-order powers.
            StringBuilder sb = new StringBuilder();
            int i = _Coeffs.Length - 1;
            double current = _Coeffs[i];
            if (current < 0.0) sb.Append("-");  //Leading with a negative should include the minus sign.
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

        /// <summary>
        /// Returns the highest order coefficient of this Polynomial.
        /// </summary>
        public double A { get { return _Coeffs[_Coeffs.Length - 1]; } }
        /// <summary>
        /// Returns the second highest-order coefficient of this Polynomial.
        /// </summary>
        public double B { get { return _Coeffs[_Coeffs.Length - 2]; } }
        /// <summary>
        /// Returns the third highest-order coefficient of this Polynomial.
        /// </summary>
        public double C { get { return _Coeffs[_Coeffs.Length - 3]; } }
        /// <summary>
        /// Returns the fourth highest-order coefficient of this Polynomial.
        /// </summary>
        public double D { get { return _Coeffs[_Coeffs.Length - 4]; } }
        /// <summary>
        /// Returns the fifth highest-order coefficient of this Polynomial.
        /// </summary>
        public double E { get { return _Coeffs[_Coeffs.Length - 5]; } }
       

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
            
            //Edge case - if this Polynomial is a mere constant, then the derivative is also a constant with value = 0.0.
            if (_Coeffs.Length == 1)
            {
                deriv._Coeffs = new double[1] { 0.0 };
                return deriv;
            }

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
            for  (int exp= 1; exp < _Coeffs.Length; exp++)            
                integ._Coeffs[exp] = _Coeffs[exp-1] / exp;
            integ._Coeffs[0] = constant;
            return integ;            
        }


        /// <summary>
        /// Returns the arc length of the polynomial from the given starting x-value to the given ending x-value.
        /// </summary>
        public double GetLength(double starting, double ending)
        {
            Polynomial deriv = GetDerivative();
            Polynomial derivSquare = deriv * deriv;
            derivSquare += 1;
            throw new NotImplementedException();
        }



        /// <summary>
        /// Returns the local maximum x-value between the given starting and ending x-values.
        /// </summary> 
        /// <param name="starting">The smaller bracketing x-value to examine.</param>
        /// <param name="ending">The larger bracketing x-value to examine.</param>
        /// <param name="x">This 'out' parameter returns the x-value where the local maximum was found.</param>
        /// <returns>Returns the local maximum between the bracketing x-values.</returns>
        public double GetMaximum(double starting, double ending, out double x)
        {
            //First, determine if we're dealing with a mere constant or a line.
            if (_Coeffs.Length == 0)
                throw new InvalidOperationException("No minimum or maximum for an empty Polynomial."); //Does this ever happen?
            if (_Coeffs.Length == 1) //A constant.
            {
                x = starting;
                return _Coeffs[0];
            }
            if (_Coeffs.Length == 2)
            {
                x = _Coeffs[1] >= 0 ? ending : starting;
                return Evaluate(x);
            }

            Polynomial deriv = GetDerivative();
            Complex[] roots = deriv.GetRoots();

            double maximum = double.NegativeInfinity;
            x = double.NaN;
            foreach (Complex root in roots)
            {
                if (root.Imaginary == 0.0 && root.Real >= starting && root.Real <= ending)
                {
                    double value = Evaluate(root.Real);
                    if (value > maximum)
                    {
                        maximum = value;
                        x = root.Real;
                    }
                }
            }

            double startValue = Evaluate(starting);
            if (startValue >= maximum)
            {
                maximum = startValue;
                x = starting;
            }

            double endValue = Evaluate(ending);
            if (endValue > maximum)
            {
                maximum = endValue;
                x = ending;
            }

            return maximum;
        }



        /// <summary>
        /// Returns the local minimum x-value between the given starting and ending x-values.
        /// </summary> 
        /// <param name="starting">The smaller bracketing x-value to examine.</param>
        /// <param name="ending">The larger bracketing x-value to examine.</param>
        /// <param name="x">This 'out' parameter returns the x-value where the local minimum was found.</param>
        /// <returns>Returns the local minimum between the bracketing x-values.</returns>
        public double GetMinimum(double starting, double ending, out double x)
        {
            //First, determine if we're dealing with a mere constant or a line.
            if (_Coeffs.Length == 0)
                throw new InvalidOperationException("No minimum or maximum for an empty Polynomial."); //Does this ever happen?
            if (_Coeffs.Length == 1) //A constant.
            {
                x = starting;                                
                return _Coeffs[0];
            }
            if (_Coeffs.Length == 2)
            {
                x = _Coeffs[1] >= 0 ? starting : ending;
                return Evaluate(x);
            }

            Polynomial deriv = GetDerivative();
            Complex[] roots = deriv.GetRoots();

            double minimum = double.NegativeInfinity;
            x = double.NaN;
            foreach (Complex root in roots)
            {
                if (root.Imaginary == 0.0 && root.Real >= starting && root.Real <= ending)
                {
                    double value = Evaluate(root.Real);
                    if (value < minimum)
                    {
                        minimum = value;
                        x = root.Real;
                    }
                }
            }

            double startValue = Evaluate(starting);
            if (startValue <= minimum)
            {
                minimum = startValue;
                x = starting;
            }

            double endValue = Evaluate(ending);
            if (endValue < minimum)
            {
                minimum = endValue;
                x = ending;
            }

            return minimum;
        }

        /// <summary>
        /// Returns the local minimum and maximum values of this polynomial, in the range between the given starting and ending.
        /// </summary>
        /// <param name="starting">The smaller bracketing x-value to examine.</param>
        /// <param name="ending">The larger bracketing x-value to examine.</param>   
        /// <returns>Returns a tuple specifying the minimum and maximum (in that order) local values in the bracketed range.</returns>
        public Tuple<double, double> GetMinMax(double starting, double ending)
        {
            double xMin, xMax;
            return GetMinMax(starting, ending, out xMin, out xMax);
        }
        /// <summary>
        /// Returns the local minimum and maximum values of this polynomial, in the range between the given starting and ending.
        /// </summary>
        /// <param name="starting">The smaller bracketing x-value to examine.</param>
        /// <param name="ending">The larger bracketing x-value to examine.</param>
        /// <param name="xMin">This 'out' parameter returns the minimum x-value in the bracketed range.</param>
        /// <param name="xMax">This 'out' parameter returns the maximum x-value in the bracketed range.</param>
        /// <returns>Returns a tuple specifying the minimum and maximum (in that order) local values in the bracketed range.</returns>
        public Tuple<double,  double> GetMinMax(double starting, double ending, out double xMin, out double xMax)
        {
            //First, determine if we're dealing with a mere constant or a line.
            if (_Coeffs.Length == 0)
                throw new InvalidOperationException("No minimum or maximum for an empty Polynomial."); //Does this ever happen?
            if (_Coeffs.Length == 1) //A constant.
            {
                xMin = starting;
                xMax = ending;
                double y = _Coeffs[0];
                return new Tuple<double, double>(y, y);
            }            
            if (_Coeffs.Length == 2)
            {
                double slope = _Coeffs[1];
                if (slope >=0)
                {
                    xMin = starting;
                    xMax = ending;
                    return new Tuple<double, double>(Evaluate(xMin), Evaluate(xMax));
                }
                xMin = ending;
                xMax = starting;
                return new Tuple<double, double>(Evaluate(xMin), Evaluate(xMax));
            }

            Polynomial deriv = GetDerivative();            
            Complex[] roots = deriv.GetRoots();

            double minimum = double.PositiveInfinity;
            double maximum = double.NegativeInfinity;

            xMin = double.NaN;
            xMax = double.NaN;

            foreach (Complex root in roots)
            {
                if (root.Imaginary == 0.0 && root.Real >= starting && root.Real <= ending)
                {
                    double value = Evaluate(root.Real);
                    if (value < xMin)
                    {
                        minimum = value;
                        xMin = root.Real;
                    }
                    if (value > xMax)
                    {
                        maximum = value;
                        xMax = root.Real;
                    }
                }
            }

            double startValue = Evaluate(starting);
            if (startValue <= minimum)
            {
                minimum = startValue;
                xMin = starting;
            }
            else if (startValue >= maximum)
            {
                maximum = startValue;
                xMax= starting;
            }

            double endValue = Evaluate(ending);
            if (endValue < minimum)
            {
                minimum = endValue;
                xMin = ending;
            }
            else if (endValue > maximum)
            {
                maximum = endValue;
                xMax = ending;
            }

            return new Tuple<double, double>(xMin, xMax);
        }
        

        #endregion




        #region Polynomial root finding


        /// <summary>
        /// Returns the roots of this Polynomial, or in other words, the values of 'x' when descending exponents of 'x' times the respective coefficients are set equal to 
        /// 0.
        /// </summary>        
        public Complex[] GetRoots(bool realOnly = false)
        {
            switch (_Coeffs.Length)
            {
                case 0: throw new ArithmeticException("There are no roots for a Polynomial with no coefficients.");
                case 1: throw new ArithmeticException("There are no roots for a Polynomial with a single coefficient.");
                case 2: return GetRoots(_Coeffs[1], _Coeffs[0]);
                case 3: return GetRoots(_Coeffs[2], _Coeffs[1], _Coeffs[0], realOnly);
                case 4: return GetRoots(_Coeffs[3], _Coeffs[2], _Coeffs[1], _Coeffs[0], realOnly);
                case 5: return GetRoots(_Coeffs[4], _Coeffs[3], _Coeffs[2], _Coeffs[1], _Coeffs[0], realOnly);
                default:
                    throw new NotImplementedException("Abel-Ruffini theorem holds it is impossible to find the roots of a quintic Polynomial (or higher) algebraically.");
            }
        }

        /// <summary>
        /// Returns only the real roots of this Polynomial.
        /// </summary>
        /// <remarks>The roots returned will all be real, and there will not be multiple instances of the same root in any case.  If there are no real roots for a 
        /// Polynomial, returns an empty array.
        /// <para/>This method is included because not everyone wants to include a reference to the System.Numerics.Complex data structure.</remarks>
        public double[] GetRealRoots()
        {            
            Complex[] complexRoots = GetRoots(true);
            double[] result = new double[complexRoots.Length];
            for (int i = 0; i < complexRoots.Length; i++) result[i] = complexRoots[i].Real;
            return result;
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
        public static Complex[] GetRoots(double a, double b, double c, bool realOnly = false)
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
                if (realOnly) return new Complex[0];
                Complex complexDiscrim = Complex.Sqrt(discrim);
                return new Complex[2] { (-b - complexDiscrim) / aTimes2, (-b + complexDiscrim) / aTimes2 };
            }
            else
            {
                // If the discrim==0, then there is a multi-root (2 roots of identical value).
                if (realOnly) return new Complex[1] { (-b - discrim) / aTimes2 };
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
        public static Complex[] GetRoots(double a, double b, double c, double d, bool realOnly = false)
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
                double result = -Math.Pow(d / a, CommonValues.OneThird);
                if (realOnly) return new Complex[1] { result };
                return new Complex[3] { result, result, result };
            }
            else if (discrim >= 0)
            {
                double r, rCubeRoot=0, t, tCubeRt=0, bOver3a;

                r = -(g / 2) + Math.Pow(discrim, 0.5);                
                //Get the singular real cube root of r, and store it in s.
                Complex[] roots = Operations.NthRoots(r, 3); 
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
                roots = Arithmetic.Operations.NthRoots(t, 3);
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
                if (realOnly) return new Complex[1] { x1 };
                Complex x2 = new Complex((-(rCubeRoot + tCubeRt) / 2) + bOver3a, (rCubeRoot - tCubeRt) * CommonValues.SqRt3 / 2);
                Complex x3 = new Complex((-(rCubeRoot + tCubeRt) / 2) + bOver3a, -(rCubeRoot - tCubeRt) * CommonValues. SqRt3 / 2);
                return new Complex[3] { x1, x2, x3 };
            }
            
            else
            {
                //Three real roots.
                double i, iCubeRt, k, m, n, bOver3a;
                i = Math.Pow((g * g / 4) - discrim, 0.5);
                iCubeRt = Math.Pow(i, CommonValues.OneThird);
                k = Math.Acos(-(g / (2 * i)));

                //Use trig to find the other two roots.
                double kOneThird = k/3;
                m = Math.Cos(kOneThird);
                n = CommonValues.SqRt3 * Math.Sin(kOneThird);

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
        public static Complex[] GetRoots(double a, double b, double c, double d, double e, bool realOnly = false)
        {
            if (a == 0) return GetRoots(b, c, d, e);
            throw new NotImplementedException("Have not implemented root-finding for quartic polynomial expressions.  Good luck, mate.");
        }

        IDifferentiable IDifferentiable.GetDerivative()
        {
            return GetDerivative();
        }

        IDifferentiable IDifferentiable.GetIntegral(double constant)
        {
            return GetIntegral(constant);
        }


        double[] IDifferentiable.GetRoots()
        {
            return GetRealRoots();            
        }



        #endregion

    }
}
