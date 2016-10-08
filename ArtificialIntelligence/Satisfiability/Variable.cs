using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.Satisfiability
{
    internal class Variable<TDomain>
    {
        public readonly object Tag;
        public readonly List<TDomain> Domains = new List<TDomain>();
        public Variable(object tag, IEnumerable<TDomain> domains)
        {
            Tag = tag;
            Domains = domains.ToList();
        }

        public Variable<TDomain> Copy()
        {
            return new Variable<TDomain>(Tag, Domains);
        }

        public bool IsSolved { get { return Domains.Count == 1; } }
        public bool IsInvalid { get { return Domains.Count == 0; } }
    }
}
