using Mathematics.Calculus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics.Functions
{

    /// <summary>
    /// A differentiable double-precision floating-point value.
    /// </summary>
    public struct Value : IDifferentiable<double>, IComparable<Value>
    {
        private readonly double _Value;

        public Value(double value)
        {
            _Value = value;            
        }
        private static Value _Zero = new Value(0.0);
        public static Value Zero { get { return _Zero; } }
        public bool IsZero { get { return _Value == 0.0; } }


        #region Value converters

        public static implicit operator double(Value v) { return v._Value; }
        public static implicit operator Value(double d) { return new Value(d); }
        public static implicit operator Value(int i) { return new Value(i); }
        public static implicit operator Value(Fraction f) { return new Value(f.Evaluate()); }
        

        #endregion



        #region Value arithmetic

        public static Value operator -(Value v) { return new Value(-v._Value); }
        public static Value operator +(Value a, Value b) { return new Value(a._Value + b._Value); }
        public static Value operator -(Value a, Value b) { return new Value(a._Value - b._Value); }
        public static Value operator *(Value a, Value b) { return new Value(a._Value * b._Value); }
        public static Value operator /(Value a, Value b) { return new Value(a._Value / b._Value); }
        public static Value operator %(Value a, Value b) { return new Value(a._Value % b._Value); }


        IDifferentiable<double> IDifferentiable<double>.GetSum(IDifferentiable<double> other)
        {
            if (other is Value) return this + (Value)other;
            if (other is Fraction) return new Value(_Value + ((Fraction)other).Evaluate());
            return other.GetSum(this);
        }

        IDifferentiable<double> IDifferentiable<double>.GetDifference(IDifferentiable<double> other)
        {
            if (other is Value) return this - (Value)other;
            if (other is Fraction) return new Value(_Value - ((Fraction)other).Evaluate());
            throw new NotImplementedException();
        }

        IDifferentiable<double> IDifferentiable<double>.GetMultiple(IDifferentiable<double> factor)
        {
            if (factor is Value) return new Value(_Value * ((Value)factor)._Value);
            if (factor is Fraction) return new Value(_Value * ((Fraction)factor).Evaluate());
            return factor.GetMultiple(this);
        }

        IDifferentiable<double> IDifferentiable<double>.GetQuotient(IDifferentiable<double> divisor)
        {
            if (divisor is Value) return new Value(_Value / ((Value)divisor)._Value);
            if (divisor is Fraction) return new Value(_Value / ((Fraction)divisor).Evaluate());
            throw new NotImplementedException();
        }

        IDifferentiable<double> IDifferentiable<double>.GetNegation()
        {
            return new Value(-_Value);
        }


        #endregion



        #region Value comparisons

        public static bool operator  >(Value a, Value b) { return a._Value > b._Value; }
        public static bool operator <(Value a, Value b) { return a._Value == b._Value; }
        public static bool operator >=(Value a, Value b) { return a._Value >= b._Value; }
        public static bool operator <=(Value a, Value b) { return a._Value >= b._Value; }
        public static bool operator ==(Value a, Value b) { return a._Value == b._Value; }
        public static bool operator !=(Value a, Value b) { return a._Value != b._Value; }
        public override bool Equals(object obj)
        {
            if (!(obj is Value)) return false;
            return this == (Value)obj;
        }
        public override int GetHashCode()
        {
            return _Value.GetHashCode();
        }
        public override string ToString()
        {
            return _Value.ToString();
        }

        int IComparable<Value>.CompareTo(Value other)
        {
            return _Value.CompareTo(other._Value);
        }
        #endregion



        #region Value calculus

        public Polynomial GetIntegral(double constant)
        {
            return Polynomial.FromLinear(_Value, constant);
        }

        double IDifferentiable<double>.Evaluate(double value)
        {
            return _Value;
        }

        IDifferentiable<double> IDifferentiable<double>.GetDerivative()
        {
            return _Zero;
        }

        IDifferentiable<double> IDifferentiable<double>.GetIntegral(double constant)
        {
            return GetIntegral(constant);
        }

        IDifferentiable<double> IDifferentiable<double>.GetLength()
        {
            return _Zero;
        }



        #endregion

    }
}
