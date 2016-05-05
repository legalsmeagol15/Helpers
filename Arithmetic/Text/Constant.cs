using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arithmetic.Text
{
    /// <summary>
    /// A type-safe immutable token for storing simple double values.
    /// </summary>
    internal class Constant : IEvaluateable
    {
        public double Value { get; }

        public Constant(double value)
        {
            this.Value = value;
        }

        public double Evaluate(Func<string, double> lookupFunction = null)
        {
            return this.Value;
        }

        public override string ToString()
        {
            if (Value < 0.0) return " " + Value;
            return "" + Value;
        }

        /// <summary>
        /// Returns true if the value of the other given constant is equal; false otherwise.
        /// </summary>  
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is double || obj is int || obj is decimal)
                return this.Value == (double)obj;
            Constant other = obj as Constant;
            if (other == null) return false;
            return other.Value == this.Value;
        }
        /// <summary>
        /// Returns the hash code of the value of this constant.
        /// </summary>            
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
