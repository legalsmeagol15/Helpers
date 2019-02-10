using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency
{
    /// <summary>Indicates feet, inches, kg, etc.</summary>
    public sealed class Unit : IEvaluateable
    {
        private readonly IEvaluateable _Expression;
        private Unit(IEvaluateable expression) { this._Expression = expression; }
        public IEvaluateable Evaluate() => throw new NotImplementedException();

        public override bool Equals(object obj) => throw new NotImplementedException();

        public override int GetHashCode() => _Expression.GetHashCode();
    }
}
