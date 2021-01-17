using DataStructures;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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


        /// <summary>
        /// Profile the given object.  This will result in a tree whose leaf nodes exist for the 
        /// leaf values (for example, if <paramref name="obj"/> has a 
        /// <seealso cref="ConfigurationAttribute"/> property that is an <seealso cref="int"/>, 
        /// the tree will contain a <seealso cref="ConfigNode"/> for the property but also for the 
        /// <seealso cref="int"/>.
        /// </summary>
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
                    {
                        cn.Members[dca.Key] = _Profile(pinfo.GetValue(this_obj), dca);
                        cn.ReflectionInfo = pinfo;
                    }   
                    else if (t.GetField(dca.MemberName, BINDING_FILTER) is FieldInfo finfo)
                    {
                        cn.Members[dca.Key] = _Profile(finfo.GetValue(this_obj), dca);
                        cn.ReflectionInfo = finfo;
                    }   
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
                        cn.ReflectionInfo = pinfo;
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
                        cn.ReflectionInfo = finfo;
                    }
                }

                // Done
                if (cn.IsLeaf && cn.Attribute.TypeConverter != default)
                    throw new ConfigurationException("Non-leaf configuration members cannot specify ")
                return cn;
            }

        }

        private static Version GetCurrentVersion() => Assembly.GetExecutingAssembly().GetName().Version;

        public static void Save(object obj, Version saveAs = default, string filename = DEFAULT_FILENAME)
        {
            if (saveAs == default) saveAs = GetCurrentVersion();

            ConfigNode root = Profile(obj, saveAs);

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

            _Write("config", root);

            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
            writer.Close();


            void _Write(string name, ConfigNode cn)
            {
                if (cn.Members.Any())
                {
                    writer.WriteStartElement(name);
                    foreach (var kvp in cn.Members)
                    {
                        if (kvp.Value.IsLeaf)
                        {
                            TypeConverter converter = GetConverter(cn);
                            if (!converter.CanConvertTo(typeof(string)))
                                throw new ConfigurationException("Type " + cn.Type.Name + " cannot be converted to string with " + converter.GetType().Name);
                            if (converter is ConfigurationConverter configConverter)
                                writer.WriteAttributeString(kvp.Key, configConverter.ConvertTo(kvp.Value.Data, cn.Data));
                            else
                                writer.WriteAttributeString(kvp.Key, (string)converter.ConvertTo(kvp.Value.Data, typeof(string)));
                        }   
                        else
                            _Write(kvp.Key, kvp.Value);
                    }
                    writer.WriteEndElement();
                }
            }


        }

        public static void Load(object applyTo, string filename = DEFAULT_FILENAME)
        {
            
            XmlDocument doc = new XmlDocument();
            doc.Load(filename);
            Console.WriteLine(doc.InnerXml);
            Version loaded_version = new Version(doc.SelectSingleNode("//configuration/versions/@used").Value);
            Console.WriteLine(loaded_version);

            // TODO:  shouldn't use recursive Profile.  The ConfigNode should be created just-in-
            // -time, to allow for the value of a config'ed property to be changed and then applied.
            ConfigNode n = Profile(applyTo, loaded_version);
            _Read(n, doc.SelectSingleNode("//configuration/config"), applyTo);
            
            void _Read(ConfigNode configNode, XmlNode xmlNode, object target)
            {
                foreach (KeyValuePair<string, ConfigNode> kvp in configNode.Members)
                {
                    if (xmlNode.SelectSingleNode("@" + kvp.Key) is XmlNode xmlAttr)
                    {
                        object newValue = __Convert(configNode, xmlNode);
                        
                    }
                    else if (xmlNode.SelectSingleNode(kvp.Key) is XmlNode xmlChild)
                    {
                        _Read(kvp.Value, xmlChild, target);
                    }
                }

                object __Convert(ConfigNode cn, XmlNode xn)
                {
                    TypeConverter converter = GetConverter(cn);
                    
                    if (!converter.CanConvertFrom(typeof(string)))
                        throw new ConfigurationException("Cannot convert \"" + xn.Value + "\" from string to " + cn.Type.Name + " using " + converter.GetType().Name);
                    else if (converter is ConfigurationConverter configConverter)
                    {
                        List<KeyValuePair<string, string>> kvps = new List<KeyValuePair<string, string>>();
                        foreach (string xpath in cn.Attribute.ConversionXPaths)
                        {
                            var value = xn.SelectSingleNode(xpath);
                            kvps.Add(new KeyValuePair<string, string>(xpath, value != null ? value.Value : ""));
                        }
                        return configConverter.ConvertFrom(xn.Value, kvps.ToArray());
                    }
                    else
                        return converter.ConvertFrom(xn.Value);
                }
            }


        }

        private sealed class ConfigNode
        {
            internal readonly ConfigurationAttribute Attribute;
            internal MemberInfo ReflectionInfo;
            internal readonly object Data;
            public bool IsLeaf => Members.Count == 0;

            public Type Type => ReflectionInfo is PropertyInfo pinfo ? pinfo.PropertyType
                                                                     : ((FieldInfo)ReflectionInfo).FieldType;

            internal readonly Dictionary<string, ConfigNode> Members = new Dictionary<string, ConfigNode>();

            internal ConfigNode(object data, ConfigurationAttribute attrib)
            {
                this.Data = data;
                this.Attribute = attrib;
            }
        }


        private static TypeConverter GetConverter(ConfigNode n)
        {
            if (n.Attribute.TypeConverter != default)
                return (TypeConverter)Activator.CreateInstance(n.Attribute.TypeConverter);
            else if (n.ReflectionInfo is PropertyInfo pinfo)
                return DefaultConverter(pinfo.PropertyType);
            else if (n.ReflectionInfo is FieldInfo finfo)
                return DefaultConverter(finfo.FieldType);
            else 
                throw new ConfigurationException("Cannot identify converter type.");

        }
        private static TypeConverter DefaultConverter(Type t)
        {
            if (t == typeof(string)) return new NopConverter();
            if (t == typeof(int) || t == typeof(Int32)) return new Int32Converter();
            if (t == typeof(double) || t == typeof(Double)) return new DoubleConverter();
            else throw new NotImplementedException("Haven't implemented type converter for " + t.Name);
        }

        private class NopConverter : System.ComponentModel.TypeConverter
        {
            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string);
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);
            public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) => value.ToString();
            public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType) => value.ToString();
        }
    }




    public class ConfigurationException : Exception
    {
        public ConfigurationException(params string[] message) : base(string.Format(message[0], message.Skip(1).ToArray())) { }
    }

}

