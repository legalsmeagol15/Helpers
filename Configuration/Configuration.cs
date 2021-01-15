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
        private readonly ConfigNode _Node;
        private readonly Type _Type;
        private Configuration(Type type, ConfigNode node) { this._Type = type; this._Node = node; }

        private static ConfigNode Profile(object obj, Version savedVersion)
        {
            HashSet<object> visited = new HashSet<object>();
            return _Profile(obj, null);

            ConfigNode _Profile(object this_obj, ConfigurationAttribute attrib)
            {
                if (this_obj.GetType().IsClass && !visited.Add(this_obj))
                    throw new ConfigurationException("Circularity in configuration detected.");
                ConfigNode cn = new ConfigNode(this_obj, attrib);
                Type t = this_obj.GetType();

                // Step #1 - what are the object's configuration declared properties associated with the class.
                foreach (var dca in t.GetCustomAttributes<ConfigurationDeclaredAttribute>()
                                     .Where(_dca => _dca.Versions.Contains(savedVersion)))
                {
                    if (cn.Members.ContainsKey(dca.Key))
                        throw new ConfigurationException("Multiple configurations handle key '{0}' for version {1}", dca.Key, savedVersion.ToString());
                    if (t.GetProperty(dca.MemberName, BINDING_FILTER) is PropertyInfo pinfo)
                        cn.Members[dca.Key] = _Profile(pinfo.GetValue(this_obj), dca);
                    else if (t.GetField(dca.MemberName, BINDING_FILTER) is FieldInfo finfo)
                        cn.Members[dca.Key] = _Profile(finfo.GetValue(this_obj), dca);
                    else
                        throw new ConfigurationException("Undeclared configuration member '{0}' in {1}.", dca.MemberName, nameof(ConfigurationDeclaredAttribute));
                }

                // Step #2 - check for ConfigurationAttributes associated with the properties.
                foreach (PropertyInfo pinfo in t.GetProperties(BINDING_FILTER))
                {
                    foreach (var ca in pinfo.GetCustomAttributes<ConfigurationAttribute>()
                                            .Where(_ca => _ca.Versions.Contains(savedVersion)))
                    {
                        string name = string.IsNullOrWhiteSpace(ca.Key) ? pinfo.Name : ca.Key;
                        if (cn.Members.ContainsKey(name))
                            throw new ConfigurationException("Multiple configurations handle key '{0}' for version {1}", name, savedVersion.ToString());
                        cn.Members[name] = _Profile(pinfo.GetValue(this_obj), ca);
                    }
                }

                // Step #3 - check for ConfigurationAttributes associated with the fields.
                foreach (FieldInfo finfo in t.GetFields(BINDING_FILTER))
                {
                    foreach (var ca in finfo.GetCustomAttributes<ConfigurationAttribute>()
                                            .Where(_ca => _ca.Versions.Contains(savedVersion)))
                    {
                        string name = string.IsNullOrWhiteSpace(ca.Key) ? finfo.Name : ca.Key;
                        if (cn.Members.ContainsKey(name))
                            throw new ConfigurationException("Multiple configurations handle key '{0}' for version {1}", name, savedVersion.ToString());
                        cn.Members[name] = _Profile(finfo.GetValue(this_obj), ca);
                    }
                }

                // Done
                return cn;
            }

        }

        public static void Save(object obj, Version saveAs = default, string filename = DEFAULT_FILENAME)
        {
            if (saveAs == default) saveAs = Assembly.GetExecutingAssembly().GetName().Version;
            
            ConfigNode n = Profile(obj, saveAs);

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                NewLineOnAttributes = true
            };

            XmlWriter writer = XmlWriter.Create(filename, settings);
            writer.WriteStartDocument();
            writer.WriteStartElement("configuration");
            writer.WriteStartElement("versions");
            writer.WriteAttributeString("current", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            writer.WriteAttributeString("used", saveAs.ToString());
            writer.WriteEndElement();

            _Write("config", n);

            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
            writer.Close();


            void _Write(string name, ConfigNode node)
            {
                if (node.Members.Any())
                {
                    writer.WriteStartElement(name);
                    foreach (var kvp in node.Members)
                    {
                        
                        if (!kvp.Value.Members.Any())
                            writer.WriteAttributeString(kvp.Key, kvp.Value.Data.ToString());
                        else
                            _Write(kvp.Key, kvp.Value);
                        
                    }
                    writer.WriteEndElement();
                }
            }

           
        }

        public static void Load(object applyTo, string filename = DEFAULT_FILENAME)
        {
            //XmlReader reader = XmlReader.Create(filename);
            ////reader.WhitespaceHandling = WhitespaceHandling.None;
            //while (reader.Read())
            //{
            //    Console.WriteLine(reader.NodeType + " name:" + reader.Name + " value:" + reader.Value);
            //}

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(filename);
            foreach (var node in doc.DocumentElement.ChildNodes)
            {
                Console.WriteLine(node);
            }
        }

        private sealed class ConfigNode
        {
            internal readonly ConfigurationAttribute Attribute;
            internal readonly object Data;

            internal readonly Dictionary<string, ConfigNode> Members = new Dictionary<string, ConfigNode>();

            internal ConfigNode(object data, ConfigurationAttribute attrib) { this.Data = data; this.Attribute = attrib; }
        }




    }

    public class ConfigurationException : Exception
    {
        public ConfigurationException(params string[] message) : base(string.Format(message[0], message.Skip(1).ToArray())) { }
    }
}
