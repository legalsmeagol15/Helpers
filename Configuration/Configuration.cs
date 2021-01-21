using DataStructures;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
        
        private static Version GetCurrentVersion() => Assembly.GetExecutingAssembly().GetName().Version;

        public static void Save(object obj, Version saveAs = default, string filename = DEFAULT_FILENAME)
        {
            if (saveAs == default) saveAs = GetCurrentVersion();

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                NewLineOnAttributes = false
            };
            ConfigNode cn = new ConfigNode("config", null, null);

            XmlWriter writer = XmlWriter.Create(filename, settings);

            writer.WriteStartDocument();
            writer.WriteStartElement("configuration");
            writer.WriteStartElement("versions");
            writer.WriteAttributeString("current", Assembly.GetExecutingAssembly().GetName().Version.ToString());
            writer.WriteAttributeString("used", saveAs.ToString());
            writer.WriteEndElement();

            cn.Save(writer, saveAs, obj);

            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
            writer.Close();

        }

        public static void Load(object applyTo, string filename = DEFAULT_FILENAME)
        {
            
            
        }


        private sealed class ConfigNode
        {
            internal readonly string Name;
            internal readonly ConfigurationAttribute Attribute;
            internal readonly MemberInfo ReflectionInfo;

            public object GetValue(object host) => (ReflectionInfo is PropertyInfo pinfo) ? pinfo.GetValue(host)
                                                 : (ReflectionInfo is FieldInfo finfo) ? finfo.GetValue(host)
                                                 : throw new NotImplementedException();

            public Type Type => ReflectionInfo is PropertyInfo pinfo ? pinfo.PropertyType
                                                                     : ((FieldInfo)ReflectionInfo).FieldType;

            internal ConfigNode(string name, ConfigurationAttribute attrib, MemberInfo reflectionInfo)
            {
                this.Name = name;
                this.ReflectionInfo = reflectionInfo;
                this.Attribute = attrib;
            }
            public void Save(XmlWriter writer, Version version, object host) => _Save(writer, host, version, new HashSet<object>());
            private void _Save(XmlWriter writer, object host, Version savedVersion, HashSet<object> visited)
            {
                
                Type t = host.GetType();
                if (t.IsClass && !visited.Add(host))
                    throw new ConfigurationException("Detected circularity in object graph.");
                Dictionary<string, ConfigNode> nodes = new Dictionary<string, ConfigNode>();

                // Step #1 - identify all the properties that are marked for configuration.
                {
                    foreach (var dca in t.GetCustomAttributes<ConfigurationDeclaredAttribute>()
                                     .Where(_dca => _dca.Versions.Contains(savedVersion)))
                    {
                        if (nodes.ContainsKey(dca.Key))
                            throw new ConfigurationException("Multiple configurations handle key '{0}' for version {1}", dca.Key, savedVersion.ToString());
                        else if (t.GetProperty(dca.MemberName, BINDING_FILTER) is PropertyInfo pinfo)
                            nodes[dca.Key] = new ConfigNode(dca.Key, dca, pinfo);
                        else if (t.GetField(dca.MemberName, BINDING_FILTER) is FieldInfo finfo)
                            nodes[dca.Key] = new ConfigNode(dca.Key, dca, finfo);
                    }
                    foreach (PropertyInfo pinfo in t.GetProperties(BINDING_FILTER))
                    {
                        foreach (var ca in pinfo.GetCustomAttributes<ConfigurationAttribute>()
                                                .Where(_ca => _ca.Versions.Contains(savedVersion)))
                        {
                            string name = string.IsNullOrWhiteSpace(ca.Key) ? pinfo.Name : ca.Key;
                            if (nodes.ContainsKey(name))
                                throw new ConfigurationException("Multiple configurations handle key '{0}' for version {1}", name, savedVersion.ToString());
                            else
                                nodes[name] = new ConfigNode(name, ca, pinfo);
                        }
                    }
                    foreach (FieldInfo finfo in t.GetFields(BINDING_FILTER))
                    {
                        foreach (var ca in finfo.GetCustomAttributes<ConfigurationAttribute>()
                                                .Where(_ca => _ca.Versions.Contains(savedVersion)))
                        {
                            string name = string.IsNullOrWhiteSpace(ca.Key) ? finfo.Name : ca.Key;
                            if (nodes.ContainsKey(name))
                                throw new ConfigurationException("Multiple configurations handle key '{0}' for version {1}", name, savedVersion.ToString());
                            else
                                nodes[name] = new ConfigNode(name, ca, finfo);
                        }
                    }
                }
                
                // Step #2 - if we have a leaf, add as an attribute and return.
                if (!nodes.Any())
                {
                    
                    TypeConverter converter = GetConverter(host);
                    string strValue = (converter.CanConvertTo(typeof(string))) ? converter.ConvertToString(host) 
                                                                               : host.ToString();
                    writer.WriteAttributeString(this.Name, strValue);
                    return;
                }

                // Step #3 - otherwise, recursively call each member within a XML element
                writer.WriteStartElement(this.Name);
                foreach (var kvp in nodes)
                {
                    string name = kvp.Key;
                    ConfigNode node = kvp.Value;
                    node._Save(writer, node.GetValue(host), savedVersion, visited);
                }
                writer.WriteEndElement();
            }


            private class PassThruConverter : TypeConverter
            {
                public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
                    => sourceType == typeof(string) || sourceType == typeof(String);
                public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
                    => CanConvertFrom(context, destinationType);
                public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
                    => value;
                public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
                    => value;
            }
            private TypeConverter GetConverter(object obj)
            {
                if (this.Attribute != null && this.Attribute.TypeConverter != null)
                    return (TypeConverter)Activator.CreateInstance(this.Attribute.TypeConverter);
                else if (obj != null)
                {
                    Type t = obj.GetType();
                    if (t == typeof(string) || t == typeof(String)) return new PassThruConverter();
                    if (t == typeof(int) || t == typeof(Int32)) return new Int32Converter();
                    throw new ConfigurationException("No converter defined for object of type " + t.Name);
                }
                else 
                    throw new ConfigurationException("No converter for null object.");
            }
            
        }

    }




    public class ConfigurationException : Exception
    {
        public ConfigurationException(params string[] message) : base(string.Format(message[0], message.Skip(1).ToArray())) { }
    }

}

