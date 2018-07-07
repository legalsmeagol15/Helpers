using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing.Values
{
    public struct Fraction : IEvaluateable
    {

        private readonly Number _Numerator, _Denominator;

        public Fraction (Number numerator, Number denominator) { this._Numerator = numerator; this._Denominator = denominator; }

        IEvaluateable IEvaluateable.Evaluate() => this;

        public override string ToString() => _Numerator + " / " + _Denominator;

        public Number ToNumber() => _Numerator / _Denominator;

        /// <summary>Divides numerator and denominator by their greatest common denominator, and returns a new fraction.  If the numerator 
        /// denominator is not an integer, returns the instant fraction.</summary>        
        public Fraction ToSimplified()
        {
            if (!_Denominator.IsInteger || !_Numerator.IsInteger) return this;
            int numer = (int)_Numerator;
            int denom = (int)_Denominator;
            int gcd = Mathematics.Primes.GetGreatestCommonDenominator(numer, denom);


            // TODO - get the actual least common multiple
            throw new NotImplementedException();
        }

        public static Fraction operator + (Fraction a, Fraction b)
        {
            Number a_numer = a._Numerator, b_numer = b._Numerator;
            Number d = a._Denominator;
            if (d != b._Denominator)
            {
                d *= b._Denominator;
                a_numer *= b._Denominator;
            }
            return new Fraction(a_numer + b_numer, d).ToSimplified();
        }
        public static Fraction operator -(Fraction a, Fraction b)
        {
            Number a_numer = a._Numerator, b_numer = b._Numerator;
            Number d = a._Denominator;
            if (d != b._Denominator)
            {
                d *= b._Denominator;
                a_numer *= b._Denominator;
            }
            return new Fraction(a_numer - b_numer, d).ToSimplified();
        }
    }
}
