using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers.Parsing
{
    /// <summary>Indicates feet, inches, kg, etc.</summary>
    public sealed class Unit : IEvaluatable
    {
        private readonly IEvaluatable _Expression;
        private Unit(IEvaluatable expression) { this._Expression = expression; }
        public IEvaluatable Evaluate() => throw new NotImplementedException();

        public override bool Equals(object obj) => throw new NotImplementedException();

        public override int GetHashCode() => _Expression.GetHashCode();
    }
}
