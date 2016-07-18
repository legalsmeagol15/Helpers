using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mathematics.Text
{
    /// <summary>
    /// A type-safe token whose name is immutable, but whose value may change.
    /// </summary>
    /// <author>Wesley Oates</author>
    /// <date>Sep 24, 2015.</date>
    internal class Variable : IEvaluateable
    {


        /// <summary>
        /// The indexing name of this variable, that will be used to find its value via a lookup function.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Used for negation, not for implicit multiplication.
        /// </summary>
        private double _Scalar;

        /// <summary>
        /// Creates a new variable with the given name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="scalar"></param>
        public Variable(string name, double scalar = 1.0)
        {
            this.Name = name;
            this._Scalar = scalar;
        }

        /// <summary>
        /// Looks up and returns the value of this variable given its name, through the lookup function 
        /// provided.
        /// </summary>     
        public double Evaluate(Func<string, double> lookupFunction = null)
        {
            return _Scalar * lookupFunction(Name);
        }

        public override string ToString()
        {
            if (_Scalar == -1.0) return " -" + Name;
            else if (_Scalar == 1.0) return Name;
            else if (_Scalar < 0.0) return " " + _Scalar + Name;
            else return _Scalar + Name;

        }

        public override bool Equals(object obj)
        {
            Variable other = obj as Variable;
            if (other == null) return false;
            return other.Name == Name;
        }

        public override int GetHashCode() { return Name.GetHashCode(); }
    }
}
