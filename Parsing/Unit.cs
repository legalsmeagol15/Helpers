using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.Parsing
{
    /// <summary>Indicates feet, inches, kg, etc.</summary>
    public sealed class Unit : IExpression
    {
        private readonly IExpression _Expression;
        private Unit(IExpression expression) { this._Expression = expression; }
        public IExpression Evaluate() => throw new NotImplementedException();

        public override bool Equals(object obj) => throw new NotImplementedException();

        public override int GetHashCode() => _Expression.GetHashCode();
    }
}
