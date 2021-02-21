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
        internal const string DEFAULT_FILENAME = "test.config";

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

        


    }




    public class ConfigurationException : Exception
    {
        public ConfigurationException(string message, Exception inner) : base(message, inner) { }
        public ConfigurationException(params string[] message) : base(string.Format(message[0], message.Skip(1).ToArray())) { }
    }

}

