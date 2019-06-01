using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency
{
    [Serializable]
    public class Null : IEvaluateable
    {
        
        public static readonly Null Instance = new Null();

        private Null() { }
        IEvaluateable IEvaluateable.Value() => this;
        public override bool Equals(object obj) => obj != null && obj is Null;
        public override int GetHashCode() => 0;
        public override string ToString() => "<Null>";
    }
}
