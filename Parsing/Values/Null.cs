using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing
{
    [Serializable]
    internal class Null : IEvaluateable
    {
        IEvaluateable IEvaluateable.Evaluate() => this;
        public override bool Equals(object obj) => obj != null && obj is Null;
        public override int GetHashCode() => 0;
    }
}
