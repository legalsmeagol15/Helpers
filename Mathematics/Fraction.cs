using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace Mathematics
{

    /// <summary>
    /// An immutable structure that maintains a fractional value.
    /// </summary>
    public struct Fraction
        //TODO:  Validate entire Fraction structure.
    {
        /// <summary>
        /// The top number of the fraction.
        /// </summary>
        public readonly int Numerator;

        /// <summary>
        /// The bottom number of the fraction.
        /// </summary>
        public readonly int Denominator;

        /// <summary>
        /// Creates a new fraction, in simplest terms.
        /// </summary> 
        public Fraction(int numerator, int denominator)
        {
            int sign = Math.Sign(numerator) * Math.Sign(denominator);
            numerator = Math.Abs(numerator);
            denominator = Math.Abs(denominator);
            int gcd = (int)BigInteger.GreatestCommonDivisor(numerator, denominator);
            this.Numerator = (numerator / gcd) * sign;
            this.Denominator = (denominator / gcd);
        }


        #region Fraction arithmetic operators

        public static Fraction operator +(Fraction a, Fraction b)
        {
            int aNumer = a.Numerator * b.Denominator, bNumer = b.Numerator * a.Denominator;
            return new Fraction(aNumer + bNumer, a.Denominator * b.Denominator);
        }
        public static Fraction operator -(Fraction a, Fraction b)
        {
            int aNumer = a.Numerator * b.Denominator, bNumer = b.Numerator * a.Denominator;
            return new Fraction(aNumer - bNumer, a.Denominator * b.Denominator);
        }
        public static Fraction operator -(Fraction a)
        {
            return new Fraction(-a.Numerator, a.Denominator);
        }
        public static Fraction operator *(Fraction a, Fraction b)
        {
            return new Fraction(a.Numerator * b.Numerator, a.Denominator * b.Denominator);
        }
        public static Fraction operator /(Fraction a, Fraction b)
        {
            int sign = Math.Sign(a.Numerator) * Math.Sign(b.Numerator);
            return new Fraction(sign * Math.Abs(a.Numerator * b.Denominator), Math.Abs(a.Denominator * b.Numerator));
        }

        #endregion



        #region Fraction comparison operators

        public override bool Equals(object obj)
        {
            if (!(obj is Fraction)) return false;
            return this == (Fraction)obj;
        }
        public override int GetHashCode()
        {
            return Math.Abs(Numerator + Denominator);
        }

        public static bool operator <(Fraction a, Fraction b)
        {
            return (a.Numerator * b.Denominator) < (b.Numerator * a.Denominator);
        }
        public static bool operator >(Fraction a, Fraction b)
        {
            return (a.Numerator * b.Denominator) > (b.Numerator * a.Denominator);
        }
        public static bool operator ==(Fraction a, Fraction b)
        {
            return (a.Denominator == b.Denominator) && (a.Numerator == b.Numerator);
        }
        public static bool operator !=(Fraction a, Fraction b)
        {
            return (a.Denominator != b.Denominator) || (a.Numerator != b.Numerator);
        }

        public static bool operator >(Fraction fraction, int i)
        {
            int divided = fraction.Numerator / fraction.Denominator;
            if (divided > i) return true;
            if (divided < i) return false;
            int mod = fraction.Numerator % fraction.Denominator;
            return (mod != 0);
        }
        public static bool operator <(Fraction fraction, int i)
        {
            int divided = fraction.Numerator / fraction.Denominator;
            if (divided < i) return true;
            if (divided > i) return false;
            int mod = fraction.Numerator % fraction.Denominator;
            return (mod == 0);
        }
        public static bool operator >(int i, Fraction fraction)
        {
            return fraction < i;
        }
        public static bool operator <(int i, Fraction fraction)
        {
            return fraction > i;
        }

        #endregion



        #region Fraction conversion operators

        public static explicit operator int(Fraction f)
        {
            return f.Numerator / f.Denominator;
        }
        public static explicit operator double(Fraction f)
        {
            return (double)f.Numerator / (double)f.Denominator;
        }
        public static explicit operator decimal(Fraction f)
        {
            return (decimal)f.Numerator / (decimal)f.Denominator;
        }
        
        public override string ToString()
        {
            return Numerator + " / " + Denominator;
        }

        public string ToString(bool complex)
        {
            if (complex)
            {
                int basenum = Numerator / Denominator;
                if (basenum == 0) return ToString();
                int smallNumer = Numerator - (basenum * Denominator);
                return basenum + " " + Math.Abs(smallNumer) + " / " + Denominator;
            }
            return ToString();
            
        }

        public string ToBase10()
        {
            return ((decimal)this).ToString();
        }


        #endregion


    }
}
