using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers
{
    /// <summary>
    /// Holds all the necessary data from a loaded configuration file.  Creating a plan should 
    /// succeed if and only if it can be applied successfully to a designated 
    /// <see cref="Host"/>.  If it would fail, it should always throw a 
    /// <seealso cref="ConfigurationException"/>.
    /// <para/>Once the plan is created, don't forget to <seealso cref="Apply"/> it.
    /// </summary>
    public sealed class ConfigurationPlan
    {
        public bool ThrowOnMissing;
        public readonly object Host;
        public readonly Version Source;
        private readonly ConfigNode _Node;
        internal ConfigurationPlan(object host, Version source, ConfigNode node) { this.Host = host; this.Source = source; this._Node = node; }
        public void Apply() { _Node.ApplyTo(Host); }
    }
}
