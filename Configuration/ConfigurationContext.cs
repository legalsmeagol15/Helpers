using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers
{
    public sealed class ConfigurationContext
    {
        /// <summary>
        /// When configuration is occurring, this is the object being configured.
        /// </summary>
        public object Host { get; internal set; }

        /// <summary>
        /// When configuration is occurring, this is the original pre-configuration value on the host.  It will be replaced by the 
        /// return value of the <seealso cref="ConfigurationConverter.ConvertFrom(ConfigurationContext, string, KeyValuePair{string, string}[])"/>
        /// call.
        /// </summary>
        public object Preconfigured { get; internal set; }

        /// <summary>
        /// An object to be passed to the configuration converters.
        /// </summary>
        public object Opaque { get; internal set; }

        public IDictionary<string, string> XPaths { get; internal set; }

        public ConfigurationContext() { }
    }
}
