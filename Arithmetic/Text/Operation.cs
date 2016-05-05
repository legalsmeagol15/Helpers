using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arithmetic.Text
{
    /// <summary>
    /// A typed token for storing operations.
    /// </summary>
    /// <remarks>Someday, this will be expanded so certain operations will contain their own logic for doing the 
    /// operation the instance describes.</remarks>
    /// <author>Wesley Oates</author>
    /// <date>Sep 24, 2015.</date>
    internal class Operation : IToken
    {
        private char _Operator;
        /// <summary>
        /// The char representing the operation to be performed.
        /// </summary>
        public char Operator { get { return _Operator; } }

        /// <summary>
        /// Creates a new operation token.
        /// </summary>
        /// <param name="oper">The mathematical operation to perform.</param>
        public Operation(char oper) { this._Operator = oper; }

        public override string ToString() { return "" + _Operator; }

        public override bool Equals(object obj)
        {
            Operation other = obj as Operation;
            if (other == null) return false;
            return other._Operator == _Operator;
        }

        public override int GetHashCode() { return _Operator.GetHashCode(); }

        
    }
}
