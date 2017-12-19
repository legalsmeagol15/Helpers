using Mathematics.Calculus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Mathematics.Functions
{
    public struct Fraction : IDifferentiable<double>, IComparable<Fraction>
    {
        public int Numerator { get; private set; }
        public int Denominator { get; private set; }

        public Fraction(int numerator, int denominator)
        {
            if (denominator == 0) throw new DivideByZeroException("Fractions cannot have a 0 denominator.");
            Numerator = Math.Abs(numerator) * (((numerator < 0) ^ (denominator < 0)) ? -1 : 1);
            Denominator = Math.Abs(denominator);
            Simplify();
        }
        public Fraction(int number) : this(number, 1) { }

        public Fraction(double number)
        {
            if (double.IsNaN(number)) throw new ArgumentException("Double.NaN cannot be converted to a fraction.");
            if (double.IsInfinity(number)) throw new ArgumentException("Double infinity values cannot be converted to a fraction.");
            int asInt = (int)number;
            if (asInt == number)
            {
                Numerator = asInt;
                Denominator = 1;
            }
            else
                throw new NotImplementedException("Have not implemented double-to-Fraction conversion.");
            Simplify();
        }


        //public static IDifferentiable<double> Zero { get { return Value.Zero; } }


        public double Evaluate() { return ((double)Numerator) / Denominator; }
        double IDifferentiable<double>.Evaluate(double value) { return Evaluate(); }

        IDifferentiable<double> IDifferentiable<double>.GetDerivative() { throw new NotImplementedException();  }//return Zero; }

        IDifferentiable<double> IDifferentiable<double>.GetIntegral(double constant)
        {
            return Polynomial.FromLinear(((IDifferentiable<double>)this).Evaluate(0.0), constant);            
        }

        IDifferentiable<double> IDifferentiable<double>.GetLength() { throw new NotImplementedException(); } //return Zero; }


        private void Simplify()
        {
            if (Numerator == 0)
            {
                Denominator = 1;
                return;
            }
            int gcd = (int)BigInteger.GreatestCommonDivisor(Numerator, Denominator);
            if (gcd != 1)
            {
                Numerator /= gcd;
                Denominator /= gcd;
            }
        }


        #region Fraction arithmetic


        public static Fraction operator +(Fraction a, Fraction b)
        {
            int newDenom = a.Denominator * b.Denominator;
            return new Fraction((a.Numerator * b.Denominator) + (b.Numerator * a.Denominator), newDenom);            
        }
        public static Fraction operator +(Fraction fraction, int integer)
        {
            return new Fraction((integer * fraction.Denominator) + fraction.Numerator, fraction.Denominator);
        }
        public static Fraction operator +(int integer, Fraction fraction)
        {
            return fraction + integer;
        }
       

        public static Fraction operator -(Fraction fraction)
        {           
            return new Fraction(-fraction.Numerator, fraction.Denominator);
        }
        public static Fraction operator -(Fraction a, Fraction b)
        {
            int newDenom = a.Denominator * b.Denominator;
            return new Fraction((a.Numerator * b.Denominator) - (b.Numerator * a.Denominator), newDenom);                        
        }
        public static Fraction operator -(Fraction fraction, int integer)
        {
            return new Fraction(fraction.Numerator - (fraction.Denominator * integer), fraction.Denominator);
        }
        public static Fraction operator -(int integer, Fraction fraction)
        {
            return new Fraction((fraction.Denominator * integer) - fraction.Numerator, fraction.Denominator);
        }


        public static Fraction operator *(Fraction a, Fraction b)
        {
            return new Fraction(a.Numerator * b.Numerator, a.Denominator * b.Denominator);            
        }
        public static Fraction operator *(Fraction fraction, int integer)
        {
            return new Fraction(fraction.Numerator * integer, fraction.Denominator);
        }
        public static Fraction operator *(int integer, Fraction fraction)
        {
            return fraction * integer;
        }

        public static Fraction operator /(Fraction a, Fraction b)
        {
            return new Fraction(a.Numerator * b.Denominator, a.Denominator * b.Numerator);
        }
        public static  Fraction operator /(Fraction fraction, int integer)
        {
            return new Fraction(fraction.Numerator, fraction.Denominator * integer);
        }
        public static  Fraction operator /(int integer, Fraction fraction)
        {
            return new Fraction(fraction.Denominator * integer, fraction.Numerator);
        }



        IDifferentiable<double> IDifferentiable<double>.GetSum(IDifferentiable<double> other)
        {
            if (other is Fraction) return (Fraction)other + this;
            //if (other is Value) return (Fraction)other + this;
            return other.GetSum(this);
        }

        IDifferentiable<double> IDifferentiable<double>.GetDifference(IDifferentiable<double> other)
        {
            if (other is Fraction) return (Fraction)other - this;
            //if (other is Value) return (Fraction)other - this;
            throw new NotImplementedException();
        }

        IDifferentiable<double> IDifferentiable<double>.GetMultiple(IDifferentiable<double> factor)
        {
            if (factor is Fraction) return (Fraction)factor * this;
            //if (factor is Value) return (Fraction)factor * this;
            return factor.GetMultiple(this);
        }

        IDifferentiable<double> IDifferentiable<double>.GetQuotient(IDifferentiable<double> divisor)
        {
            if (divisor is Fraction) return (Fraction)divisor / this;
            //if (divisor is Value) return (Fraction)divisor / this;
            throw new NotImplementedException();
        }

        IDifferentiable<double> IDifferentiable<double>.GetNegation()
        {
            return -this;
        }


        #endregion



        #region Fraction comparison


        int IComparable<Fraction>.CompareTo(Fraction other)
        {
            return (this > other) ? 1 : (this == other) ? 0 : -1;
        }

        public static bool operator >(Fraction a, Fraction b)
        {
            return (a.Numerator * b.Denominator) > (b.Numerator * a.Denominator);
        }
        public static bool operator <(Fraction a, Fraction b)
        {
            return (a.Numerator * b.Denominator) < (b.Numerator * a.Denominator);
        }
        public static bool operator ==(Fraction a, Fraction b)
        {
            return a.Numerator == b.Numerator && a.Denominator == b.Denominator;
        }
        public static bool operator !=(Fraction a, Fraction b)
        {
            return a.Numerator != b.Numerator || a.Denominator != b.Denominator;
        }
        public static bool  operator >=(Fraction a, Fraction b)
        {
            return (a.Numerator * b.Denominator) >= (b.Numerator * a.Denominator);
        }
        public static bool operator  <=(Fraction a,  Fraction b)
        {
            return (a.Numerator * b.Denominator) <= (b.Numerator * a.Denominator);
        }


        public override bool Equals(object obj)
        {
            if (!(obj is Fraction)) return false;
            Fraction other = (Fraction)obj;
            return this == other;
        }

        #endregion



        #region Fraction conversion


        private const int MAX_CONVERSION_DENOMINATOR = 500;
        private const double CONVERSION_ACCURACY_RATIO = 1d / (1 << 10);
        /// <summary>
        /// Attempts to parse the given double into a fraction.
        /// </summary>
        /// <param name="d">The double number to parse.</param>
        /// <param name="result">The fractional value to return.  If the double cannot be converted, the value passed will be 1/1.</param>
        /// <param name="greatestDenominator">The greatest allowable denominator for the resulting fraction.  For example, if this number 
        /// is set to 500, then the resultant fraction will be in terms of 1/500ths or larger.</param>
        /// <param name="conversionAccuracyRatio">The accuracy with which to convert to a fraction.</param>
        /// <returns>Returns true if the given double can be converted with the given parameters to a fraction; otherwise, returns false.  
        /// If the method returns false, the value of the fraction will be set to a 1/1.</returns>
        public static bool TryParse(double d, out Fraction result, int greatestDenominator = MAX_CONVERSION_DENOMINATOR, 
                                    double conversionAccuracyRatio = CONVERSION_ACCURACY_RATIO)
        {
            //TODO:  Fraction - a better algorithm for TryParse.            
            int whole = (int)d;            
            d -= whole;

            //If there is no difference between original double and whole, then the double was a whole number.  Just return true.
            if  (d==0.0d)
            {
                result = new Fraction(whole, 1);
                return true;
            }

            //Try different denominators.
            int denom = 1;            
            while (denom<=greatestDenominator)
            {                
                double term = 1d / ++denom;
                double ratio = d / term;
                int numer = (int)ratio;

                if (ratio - numer < (term * conversionAccuracyRatio))
                {
                    result = new Fraction((whole * denom) + numer, denom);                    
                    return true;
                }
            }

            result = new Fraction(1, 1);
            return false;
        }

     

        public override int GetHashCode()
        {
            return Math.Abs(Numerator + Denominator);
        }

        public override string ToString()
        {
            return Numerator + "/" + Denominator;
        }


        public static implicit operator double(Fraction f) { return ((double)f.Numerator) / f.Denominator; }
        public static implicit operator Fraction(double d)
        {
            Fraction result;
            if (!TryParse(d, out result)) throw new ArgumentException("Cannot convert given double " + d + " to a Fraction.");
            return result;
        }
        public static implicit operator Fraction(int i) { return new Fraction(i, 1); }
        //public static implicit operator Fraction(Value v)
        //{
        //    Fraction result;
        //    if (!TryParse(v, out result)) throw new ArgumentException("Cannot convert given Value " + v + " to a Fraction.");
        //    return result;
        //}

        #endregion




    }
}
