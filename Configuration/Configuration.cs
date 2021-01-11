using DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Helpers
{
    public sealed class Configuration
    {
        private const BindingFlags BINDING_FILTER = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const string DEFAULT_FILENAME = "test.config";
        private readonly SaveNode _Node;
        private readonly Type _Type;
        private Configuration(Type type, SaveNode node) { this._Type = type; this._Node = node; }

        public static void Save(object obj, string filename = DEFAULT_FILENAME, Version saveAs = default)
        {
            if (saveAs == default) saveAs = Assembly.GetExecutingAssembly().GetName().Version;            
            HashSet<object> visited = new HashSet<object>();
            SaveNode n = _Profile(obj, null);

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.NewLineOnAttributes = true;
            
            XmlWriter writer = XmlWriter.Create(filename, settings);
            writer.WriteStartDocument();
            writer.WriteStartElement("versions");
            writer.WriteAttributeString("current", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            writer.WriteAttributeString("used", saveAs.ToString());
            writer.WriteEndElement();

            _WriteProfile("config", n);

            writer.WriteEndDocument();
            writer.Close();


            void _WriteProfile(string name, SaveNode saveNode)
            {
                if (saveNode.Members.Any()){
                    writer.WriteStartElement(name);
                    foreach (var kvp in saveNode.Members) _WriteProfile(kvp.Key, kvp.Value);
                    writer.WriteEndElement();
                }
                else {
                    writer.WriteAttributeString(name, saveNode.Data.ToString());
                }

            }

            SaveNode _Profile(object this_obj, ConfigurationAttribute attrib)
            {
                if (this_obj.GetType().IsClass && !visited.Contains(this_obj))
                    throw new ConfigurationException("Circularity in configuration detected.");
                SaveNode sn = new SaveNode(this_obj, attrib);
                Type t = sn.GetType();

                // Step #1 - what are the object's configuration declared properties associated with the class.
                foreach (var dca in t.GetCustomAttributes<ConfigurationDeclaredAttribute>()
                                     .Where(_dca => _dca.Versions.Contains(saveAs)))
                {
                    if (sn.Members.ContainsKey(dca.Key))
                        throw new ConfigurationException("Multiple configurations handle key '{0}' for version {1}", dca.Key, saveAs.ToString());
                    if (t.GetProperty(dca.MemberName, BINDING_FILTER) is PropertyInfo pinfo)
                        sn.Members[dca.Key] = _Profile(pinfo.GetValue(this_obj), dca);
                    else if (t.GetField(dca.MemberName, BINDING_FILTER) is FieldInfo finfo)
                        sn.Members[dca.Key] = _Profile(finfo.GetValue(this_obj), dca);
                    else
                        throw new ConfigurationException("Undeclared configuration member '{0}' in {1}.", dca.MemberName, nameof(ConfigurationDeclaredAttribute));
                }

                // Step #2 - check for ConfigurationAttributes associated with the properties.
                foreach (PropertyInfo pinfo in t.GetProperties(BINDING_FILTER))
                {
                    foreach (var ca in pinfo.GetCustomAttributes<ConfigurationAttribute>()
                                            .Where(_ca => _ca.Versions.Contains(saveAs)))
                    {
                        if (sn.Members.ContainsKey(ca.Key))
                            throw new ConfigurationException("Multiple configurations handle key '{0}' for version {1}", ca.Key, saveAs.ToString());
                        sn.Members[ca.Key] = _Profile(pinfo.GetValue(this_obj), ca);
                    }
                }

                // Step #3 - check for ConfigurationAttributes associated with the fields.
                foreach (FieldInfo finfo in t.GetFields(BINDING_FILTER))
                {
                    foreach (var ca in finfo.GetCustomAttributes<ConfigurationAttribute>()
                                            .Where(_ca => _ca.Versions.Contains(saveAs)))
                    {
                        if (sn.Members.ContainsKey(ca.Key))
                            throw new ConfigurationException("Multiple configurations handle key '{0}' for version {1}", ca.Key, saveAs.ToString());
                        sn.Members[ca.Key] = _Profile(finfo.GetValue(this_obj), ca);
                    }
                }

                // Done
                return sn;
            }
            void _Write(SaveNode node)
            {
                if (node.Members.Any())
                {
                    foreach (var kvp in node.Members)
                    {
                        writer.WriteStartElement(kvp.Key);
                        _Write(kvp.Value);
                        writer.WriteEndElement(kvp.Key);
                    }
                }
            }
        }

        private sealed class SaveNode
        {
            internal readonly ConfigurationAttribute Attribute;
            internal readonly object Data;

            internal readonly Dictionary<string, SaveNode> Members = new Dictionary<string, SaveNode>();

            internal SaveNode (object data, ConfigurationAttribute attrib) { this.Data = data; this.Attribute = attrib; }
        }



        
    }

    public class ConfigurationException : Exception
    {
        public ConfigurationException(params string[] message) : base(string.Format(message[0], message.Skip(1))) { }
    }
}
