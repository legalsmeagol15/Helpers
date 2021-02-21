using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

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
        internal readonly ImportFlags ImportFlags;
        public readonly object Host;
        public readonly Version Source;
        private readonly ConfigNode _Node;
        internal ConfigurationPlan(object host, Version source, ConfigNode node, ImportFlags importFlags) { 
            this.Host = host; 
            this.Source = source; 
            this._Node = node;
            this.ImportFlags = importFlags;
        }
        public void Apply() { _Node.ApplyTo(Host); }

        /// <summary>
        /// Create a <seealso cref="ConfigurationPlan"/> object, incorporating the values supplied 
        /// by the source <paramref name="reader"/>, that would be applied to the given 
        /// <paramref name="host"/>.  
        /// </summary>
        /// <returns>A plan that can be <see cref="ConfigurationPlan.Apply"/>'ed.</returns>
        /// <exception cref="ConfigurationException">Thrown when configuration could not be 
        /// applied successfully to the given <paramref name="host"/>.</exception>
        public static ConfigurationPlan Plan(object host, XmlReader reader, ImportFlags flags = ImportFlags.AllErrors)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(reader);
            XmlNode verNode = doc.SelectSingleNode("//versions");
            if (verNode == null)
                throw new ConfigurationException("Configuration XML must contain version info.");
            if (verNode.NodeType != XmlNodeType.Element)
                throw new ConfigurationException("Configuration XML must begin with element.");
            Version ver;
            try { ver = new Version(verNode.Attributes["contents"].Value); }
            catch { throw new ConfigurationException("Failed to identify configuration XML contents version."); }
            ConfigNode cn = new ConfigNode(host.GetType().Name, null, null);

            XmlNode xmlNode = doc.SelectSingleNode("//" + host.GetType().Name);
            if (xmlNode == null)
                throw new ConfigurationException("Host type name mismatch (\"" + reader.Name + "\" vs. \"" + host.GetType().Name + "\")");
            cn.Import(xmlNode, host, ver, doc, flags);

            return new ConfigurationPlan(host, ver, cn, flags);
        }
        /// <summary>
        /// Create a <seealso cref="ConfigurationPlan"/> instance, incorporating the values 
        /// supplied by the given string, that would be applied to the given 
        /// <paramref name="host"/>.
        /// </summary>
        /// <returns>A plan that can be <see cref="ConfigurationPlan.Apply"/>'ed.</returns>
        /// <exception cref="ConfigurationException">Thrown when configuration could not be 
        /// applied successfully to the given <paramref name="host"/>.</exception>
        public static ConfigurationPlan PlanFromString(object host, string config, ImportFlags flags = ImportFlags.AllErrors)
        {
            XmlReaderSettings settings = new XmlReaderSettings
            {
                IgnoreComments = true,
                IgnoreWhitespace = true
            };
            using (StringReader strReader = new StringReader(config))
            {
                XmlReader xmlReader = XmlReader.Create(strReader, settings);
                return Plan(host, xmlReader, flags);
            }
        }
        /// <summary>
        /// Create a <seealso cref="ConfigurationPlan"/> object, incorporating the values supplied 
        /// by the source <paramref name="filename"/>, that would be applied to the given 
        /// <paramref name="host"/>.  
        /// </summary>
        /// <returns>A plan that can be <see cref="ConfigurationPlan.Apply"/>'ed.</returns>
        /// <exception cref="ConfigurationException">Thrown when configuration could not be 
        /// applied successfully to the given <paramref name="host"/>.</exception>
        public static ConfigurationPlan PlanFromFile(object host, string filename = Configuration.DEFAULT_FILENAME, ImportFlags flags = ImportFlags.AllErrors)
        {
            XmlReaderSettings settings = new XmlReaderSettings
            {
                IgnoreComments = true,
                IgnoreWhitespace = true
            };
            XmlReader reader = XmlReader.Create(filename, settings);
            return Plan(host, reader);
        }

        /// <summary>
        /// Creates a <seealso cref="ConfigurationPlan"/> object intended to set all configurable 
        /// values within the given host back to their 
        /// <seealso cref="ConfigurationAttribute.DefaultValue"/>s.
        /// </summary>
        public static ConfigurationPlan Default(object host, bool throwWithoutDefault = true)
        {
            ConfigNode cn = new ConfigNode(host.GetType().Name, null, null);
            cn.Default(host);
            ImportFlags flags = ImportFlags.DefaultOnMissing | (throwWithoutDefault ? ImportFlags.ThrowOnMissing : ImportFlags.None);
            return new ConfigurationPlan(host, Configuration.GetCurrentVersion(), cn, flags);
        }



    }
}
