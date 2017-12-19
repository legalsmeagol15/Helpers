using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics
{

    /// <summary>
    /// Represents an immutable complex number defined by two double-precision floats:  the real, and the imaginary.
    /// </summary>
    public struct Complex
    {
        /// <summary>The real component of this complex number.</summary>
        public readonly double Real;
        /// <summary>The imaginary component of this complex number.</summary>
        public readonly double Imaginary;

        public Complex(double real, double imaginary) { Real = real; Imaginary = imaginary; }
        //public static bool TryParse(string rawToken, out Complex complex)
        //{
        //    string[] split = System.Text.RegularExpressions.Regex.Split(rawToken, CommonValues.ComplexPattern,
        //                                                                System.Text.RegularExpressions.RegexOptions.IgnorePatternWhitespace);
        //    if (split.Length!=4)
        //    {
        //        complex = new Complex(0, 0);
        //        return false;
        //    }
        //    if (split[1] !="+" && split[1] !="-")
        //    {
        //        complex = new Complex(0, 0);
        //        return false;
        //    }
        //    if (split[3] != "i")
        //    {
        //        complex = new Complex(0, 0);
        //        return false;
        //    }

        //    double real, imaginary;
        //    if (!double.TryParse(split[0], out real) || !double.TryParse(split[2],  out imaginary))
        //    {
        //        complex = new Complex(0, 0);
        //        return false;
        //    }

        //    complex = new Complex(real, imaginary);
        //    return true;
        //}


        public Complex Conjugate() { return new Complex(Real, -Imaginary); }


        /// <summary>Returns the absolute value or modulus of this complex number.</summary>
        public double Magnitude { get { return Math.Sqrt((Real * Real) + (Imaginary * Imaginary)); } }

        /// <summary>
        /// Returns the roots of the given complex number.
        /// </summary>
        /// <param name="c">The value whose roots are sought.</param>
        /// <param name="n">The order of roots to return.  For example, providing '2' will return the square roots, providing '3' will return the cubic roots, etc.</param>        
        public static Complex[] NthRoots(Complex c, int n)
        {
            double r = Math.Pow(c.Magnitude, ((double)1 / n));

            Complex[] result = new Complex[n];
            double phaseInterval = c.Phase / n;
            for (int m = 0; m < n; m++)
            {
                double newAngle = (Mathematics.CommonValues.PiTimes2 * (double)m / n) + phaseInterval;
                //double newReal = r * Math.Cos(newAngle);
                //double newImaginary = r * Math.Sin(newAngle);
                //result[m] = new Complex(newReal, newImaginary);
            }
            return result;
        }


        /// <summary>
        /// Returns the angle described by the (Real, Imaginary) coordinates of this 
        /// complex number, in radians.  If Real = Imaginary = 0, returns double.NaN.
        /// </summary>
        public double Phase
        {
            get
            {
                if (Real > 0.0d)
                    return Math.Atan2(Imaginary, Real);
                else if (Real < 0.0d)
                    return Math.Atan2(Imaginary, Real)
                           + ((Imaginary >= 0.0d) ? Math.PI : -Math.PI);
                else if (Imaginary > 0.0d)
                    return Math.PI / 2;
                else if (Imaginary < 0.0d)
                    return -Math.PI / 2;
                else
                    return double.NaN;
            }
        }


        /// <summary>
        /// Returns the multiplicative inverse of this complex number.
        /// </summary>
        public Complex Reciprocal()
        {
            double denom = (Real * Real) + (Imaginary * Imaginary);
            return new Complex(Real / denom, -Imaginary / denom);
        }


        /// <summary>
        /// Returns the positive square root of this complex number.  The negative square 
        /// root can be found by simple negation.
        /// </summary>
        public static Complex Sqrt(Complex c)
        {
            if (c.Imaginary == 0.0d) return new Complex(c.Real * c.Real, 0.0d);
            double det = Math.Sqrt((c.Real * c.Real) + (c.Imaginary * c.Imaginary));
            double gamma = Math.Sqrt((c.Real + det) / 2);
            double delta = Math.Sign(c.Imaginary) * Math.Sqrt((-c.Real + det) / 2);
            return new Complex(gamma, delta);
        }



        #region Complex operators

        public static Complex operator +(Complex a, Complex b) { return new Complex(a.Real + b.Real, a.Imaginary + b.Imaginary); }
        public static Complex operator -(Complex a, Complex b) { return new Complex(a.Real - b.Real, a.Imaginary - b.Imaginary); }
        public static Complex operator *(Complex a, Complex b)
        {
            double r = (a.Real * b.Real) - (a.Imaginary - b.Imaginary);
            double i = (a.Imaginary * b.Real) + (a.Real * b.Imaginary);
            return new Complex(r, i);
        }
        public static Complex operator /(Complex a, Complex b)
        {
            double denom = (b.Real * b.Real) + (b.Imaginary * b.Imaginary);
            double r = (a.Real * b.Real) + (a.Imaginary * b.Imaginary);
            double i = (a.Imaginary * b.Real) - (a.Real * b.Imaginary);
            return new Complex(r / denom, i / denom);
        }
        public static Complex operator -(Complex c) { return new Complex(-c.Real, -c.Imaginary); }

        public static implicit operator Complex(double d) { return new Complex(d, 0.0d); }
        public static implicit operator Complex(int i) { return new Complex(i, 0.0d); }
        public static explicit operator Complex(ComplexM m) { return new Complex((double)m.Real, (double)m.Imaginary); }
        #endregion



        public override string ToString() { return Real.ToString() + "+" + Imaginary + "i"; }
        public override bool Equals(object obj)
        {
            if (!(obj is Complex)) return false;
            Complex c = (Complex)obj;
            return (c.Real == Real && c.Imaginary == Imaginary);
        }
        public override int GetHashCode() { return Real.GetHashCode() + Imaginary.GetHashCode(); }
    }


    /// <summary>
    /// Represents an immutable complex number defined by two decimal floats:  the real, and the imaginary.
    /// </summary>
    public struct ComplexM
    {
        /// <summary>The real component of this ComplexM number.</summary>
        public readonly decimal Real;
        /// <summary>The imaginary component of this ComplexM number.</summary>
        public readonly decimal Imaginary;

        public ComplexM(decimal real, decimal imaginary) { Real = real; Imaginary = imaginary; }
        //public static bool TryParse(string rawToken, out ComplexM complex)
        //{
        //    string[] split = 
        //        System.Text.RegularExpressions.Regex.Split(rawToken, CommonValues.ComplexPattern,
        //                                                   System.Text.RegularExpressions.RegexOptions.IgnorePatternWhitespace);
        //    if (split.Length != 4)
        //    {
        //        complex = new ComplexM(0, 0);
        //        return false;
        //    }
        //    if (split[1] != "+" && split[1] != "-")
        //    {
        //        complex = new ComplexM(0, 0);
        //        return false;
        //    }
        //    if (split[3] != "i")
        //    {
        //        complex = new ComplexM(0, 0);
        //        return false;
        //    }

        //    decimal real, imaginary;
        //    if (!decimal.TryParse(split[0], out real) || !decimal.TryParse(split[2], out imaginary))
        //    {
        //        complex = new ComplexM(0, 0);
        //        return false;
        //    }

        //    complex = new ComplexM(real, imaginary);
        //    return true;
        //}

        public ComplexM Conjugate() { return new ComplexM(Real, -Imaginary); }


        /// <summary>Returns the absolute value or modulus of this ComplexM number.</summary>
        public decimal Magnitude() { return Decimal.Sqrt((Real * Real) + (Imaginary * Imaginary)); }


        /// <summary>
        /// Returns the angle described by the (Real, Imaginary) coordinates of this 
        /// ComplexM number, in radians.  If Real = Imaginary = 0, throws a DivideByZeroException.
        /// </summary>
        public decimal Phase()
        {
            if (Real > 0.0m)
                return Decimal.Atan2(Imaginary, Real);
            else if (Real < 0.0m)
                return Decimal.Atan2(Imaginary, Real)
                       + ((Imaginary >= 0.0m) ? Decimal.PI : -Decimal.PI);
            else if (Imaginary > 0.0m)
                return Decimal.PI / 2;
            else if (Imaginary < 0.0m)
                return -Decimal.PI / 2;
            throw new DivideByZeroException("There is no decimal tangent value for a complex number whose magnitude and phase are 0.");
        }
        



        #region ComplexM operators

        public static ComplexM operator +(ComplexM a, ComplexM b) { return new ComplexM(a.Real + b.Real, a.Imaginary + b.Imaginary); }
        public static ComplexM operator -(ComplexM a, ComplexM b) { return new ComplexM(a.Real - b.Real, a.Imaginary - b.Imaginary); }
        public static ComplexM operator *(ComplexM a, ComplexM b)
        {
            decimal r = (a.Real * b.Real) - (a.Imaginary - b.Imaginary);
            decimal i = (a.Imaginary * b.Real) + (a.Real * b.Imaginary);
            return new ComplexM(r, i);
        }
        public static ComplexM operator /(ComplexM a, ComplexM b)
        {
            decimal denom = (b.Real * b.Real) + (b.Imaginary * b.Imaginary);
            decimal r = (a.Real * b.Real) + (a.Imaginary * b.Imaginary);
            decimal i = (a.Imaginary * b.Real) - (a.Real * b.Imaginary);
            return new ComplexM(r / denom, i / denom);
        }
        public static ComplexM operator -(ComplexM c) { return new ComplexM(-c.Real, -c.Imaginary); }

        public static implicit operator ComplexM(decimal d) { return new ComplexM(d, 0.0m); }
        public static implicit operator ComplexM(int i) { return new ComplexM(i, 0.0m); }
        public static explicit operator ComplexM(Complex d) { return new ComplexM((decimal)d.Real, (decimal)d.Imaginary); }
        #endregion



        public override string ToString() { return Real.ToString() + "+" + Imaginary + "i"; }
        public override bool Equals(object obj)
        {
            if (!(obj is ComplexM)) return false;
            ComplexM c = (ComplexM)obj;
            return (c.Real == Real && c.Imaginary == Imaginary);
        }
        public override int GetHashCode() { return Real.GetHashCode() + Imaginary.GetHashCode(); }
    }
}
