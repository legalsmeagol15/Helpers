using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.Satisfiability
{

    /// <summary>
    /// A data structure representing all the possible domains for a set of variables.
    /// </summary>
    public class Hypothesis<TDomain>
    {
        private Dictionary<object, Variable<TDomain>> _Dict = new Dictionary<object, Variable<TDomain>>();

        internal IEnumerable<Variable<TDomain>> Variables { get { return _Dict.Values; } }
        public IEnumerable<object> Tags { get { return _Dict.Keys; } }
       
        internal Variable<TDomain> GetVariable(object tag) { return _Dict[tag]; }
        /// <summary>
        /// Gets or sets the domains for a given variable.
        /// </summary>
        internal IList<TDomain> this[object tag]
        {
            get
            {
                return _Dict[tag].Domains;
            }
        }
        

        /// <summary>
        /// Returns a copy of this hypothesis, referencing the same variables but with copies of the domains assigned to each variable.
        /// </summary>        
        public Hypothesis<TDomain> Copy()
        {
            Hypothesis<TDomain> copy = new Hypothesis<TDomain>();
            foreach (KeyValuePair<object, Variable<TDomain>> kvp in _Dict)
            {
                copy._Dict.Add(kvp.Key, kvp.Value.Copy());
            }
            return copy;
        }

       
        /// <summary>
        /// Returns the portion of this hypothesis that is fully solved.  If any part of the hypothesis is invalid, returns null.
        /// </summary>        
        internal IEnumerable<Variable<TDomain>> GetSolution()
        {
            List<Variable<TDomain>> result = new List<Variable<TDomain>>();
            foreach (KeyValuePair<object, Variable<TDomain>> kvp in _Dict)
            {
                int domains = kvp.Value.Domains.Count;
                if (domains == 0) return null;
                if (domains == 1) result.Add(kvp.Value.Copy());
            }
            return result;            
        }



        public ArcConsistency Status
        {
            get
            {
                foreach (KeyValuePair<object, Variable<TDomain>> kvp in _Dict)
                {
                    int domainCount = kvp.Value.Domains.Count;
                    if (domainCount == 0) return ArcConsistency.INVALIDATED;
                    if (domainCount > 1) return ArcConsistency.PARTIALLY_SOLVED;                    
                }
                return ArcConsistency.SOLVED;                
            }
        }


        public enum ArcConsistency
        {
            INVALIDATED,
            SOLVED,
            PARTIALLY_SOLVED
        }
     
    }
}
