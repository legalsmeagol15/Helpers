using DataStructures;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Helpers
{
    /// <summary>
    /// A user-friendly system used to store configuration to an XML file, and then load it back 
    /// again without replacing the instances to which it is applied.  The handling of the 
    /// configuration is guided by <seealso cref="ConfigurationAttribute"/>s written with the 
    /// applicable code.
    /// </summary>
    public sealed class Configuration
    {
        private const string DEFAULT_FILENAME = "test.config";

        internal static Version GetCurrentVersion() => Assembly.GetExecutingAssembly().GetName().Version;

        public static void Save(object obj, Version saveAs, XmlWriter writer)
        {
            ConfigNode cn = new ConfigNode(obj.GetType().Name, null, null);
            try
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("configuration");
                writer.WriteStartElement("versions");
                writer.WriteAttributeString("current", Assembly.GetExecutingAssembly().GetName().Version.ToString());
                writer.WriteAttributeString("contents", saveAs.ToString());
                writer.WriteEndElement();

                cn.Save(writer, saveAs, obj);

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
            catch (ConfigurationException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new ConfigurationException("Failed to write configuration xml", e);
            }
            finally
            {
                writer.Flush();
                writer.Close();
                writer.Dispose();
            }
        }

        /// <summary>
        /// Writes and returns a string containing the XML for the configuration of the given object.
        /// </summary>
        /// <param name="obj">The object whose configuration will be written.</param>
        /// <param name="saveAs">The version as which configuration will be written.</param>
        public static string Save(object obj, Version saveAs = default)
        {
            if (saveAs == default) saveAs = GetCurrentVersion();
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                NewLineOnAttributes = false
            };
            StringBuilder sb = new StringBuilder();
            XmlWriter writer = XmlWriter.Create(sb, settings);
            Save(obj, saveAs, writer);
            return sb.ToString();
        }

        /// <summary>
        /// Saves configuration to the given filename.
        /// </summary>
        /// <param name="obj">The object whose configuration will be saved.</param>
        /// <param name="filename">The filename to save at.</param>
        public static void Save(object obj, string filename) => Save(obj, default, filename);

        /// <summary>
        /// Saves configuration to the given filename.
        /// </summary>
        /// <param name="obj">The object whose configuration will be saved.</param>
        /// <param name="saveAs">The version as which configuration will be saved.</param>
        /// <param name="filename">The filename to save at.</param>
        public static void Save(object obj, Version saveAs, string filename)
        {
            if (saveAs == default) saveAs = GetCurrentVersion();

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                NewLineOnAttributes = false
            };
            string tempFilename = filename + ".tmp";
            string oldConfig = filename + ".old";

            // Stash the old config, in case something breaks here.
            try
            {
                if (File.Exists(oldConfig)) File.Delete(oldConfig);
                if (File.Exists(filename)) File.Move(filename, oldConfig);

                // Now write the config doc.
                XmlWriter writer = XmlWriter.Create(tempFilename, settings);
                Save(obj, saveAs, writer);
                
                // Finally, move temp into the actual slot, and delete oldConfig.w
                try
                {
                    File.Move(tempFilename, filename);
                    if (File.Exists(oldConfig)) File.Delete(oldConfig);
                }
                catch (Exception e)
                {
                    throw new ConfigurationException("Error moving temporary file to \"" + filename + "\"", e);
                }

            }
            catch (Exception e)
            {
                // The XmlWriter will have closed because it has gone out of scope.
                try
                {
                    if (File.Exists(filename)) File.Delete(filename);
                    File.Move(oldConfig, filename);
                }
                catch
                {
                    // This is very bad.
                    throw new ConfigurationException("Error restoring old config due to underlying exception.", e);
                }
                
                throw new ConfigurationException("Error stashing old config \"" + filename + "\"", e);
            }

            
        }

        /// <summary>
        /// Create a <seealso cref="ConfigurationPlan"/> object, incorporating the values supplied 
        /// by the source <paramref name="reader"/>, that would be applied to the given 
        /// <paramref name="host"/>.  
        /// </summary>
        /// <returns>A plan that can be <see cref="ConfigurationPlan.Apply"/>'ed.</returns>
        /// <exception cref="ConfigurationException">Thrown when configuration could not be 
        /// applied successfully to the given <paramref name="host"/>.</exception>
        public static ConfigurationPlan Plan(object host, XmlReader reader)
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
            cn.Import(xmlNode, host, ver, doc);

            return new ConfigurationPlan(host, ver, cn);
        }
        /// <summary>
        /// Create a <seealso cref="ConfigurationPlan"/> instance, incorporating the values 
        /// supplied by the given string, that would be applied to the given 
        /// <paramref name="host"/>.
        /// </summary>
        /// <returns>A plan that can be <see cref="ConfigurationPlan.Apply"/>'ed.</returns>
        /// <exception cref="ConfigurationException">Thrown when configuration could not be 
        /// applied successfully to the given <paramref name="host"/>.</exception>
        public static ConfigurationPlan PlanFromString(object host, string config)
        {
            XmlReaderSettings settings = new XmlReaderSettings
            {
                IgnoreComments = true,
                IgnoreWhitespace = true
            };
            using (StringReader strReader = new StringReader(config))
            {
                XmlReader xmlReader = XmlReader.Create(strReader, settings);
                return Plan(host, xmlReader);
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
        public static ConfigurationPlan PlanFromFile(object host, string filename = DEFAULT_FILENAME)
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
        public static ConfigurationPlan Default(object host)
        {
            ConfigNode cn = new ConfigNode(host.GetType().Name, null, null);
            cn.Default(host);
            return new ConfigurationPlan(host, GetCurrentVersion(), cn);
        }


        


    }




    public class ConfigurationException : Exception
    {
        public ConfigurationException(string message, Exception inner) : base(message, inner) { }
        public ConfigurationException(params string[] message) : base(string.Format(message[0], message.Skip(1).ToArray())) { }
    }

}

