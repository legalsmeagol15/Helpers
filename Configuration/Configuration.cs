using DataStructures;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
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
        private const BindingFlags BINDING_FILTER = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const string DEFAULT_FILENAME = "test.config";

        private static Version GetCurrentVersion() => Assembly.GetExecutingAssembly().GetName().Version;

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
                writer.Flush();
                writer.Close();
            }
            catch (Exception e)
            {
                throw new ConfigurationException("Failed to write configuration xml", e);
            }
        }

        public static void Save(object obj, Version saveAs = default, string filename = DEFAULT_FILENAME)
        {
            if (saveAs == default) saveAs = GetCurrentVersion();

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                NewLineOnAttributes = false
            };
            XmlWriter writer = XmlWriter.Create(filename, settings);
            Save(obj, saveAs, writer);
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
        /// Create a <seealso cref="ConfigurationPlan"/> object, incorporating the values supplied 
        /// by the source <paramref name="filename"/>, that would be applied to the given 
        /// <paramref name="host"/>.  
        /// </summary>
        /// <returns>A plan that can be <see cref="ConfigurationPlan.Apply"/>'ed.</returns>
        /// <exception cref="ConfigurationException">Thrown when configuration could not be 
        /// applied successfully to the given <paramref name="host"/>.</exception>
        public static ConfigurationPlan Plan(object host, string filename = DEFAULT_FILENAME)
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
        /// Holds all the necessary data from a loaded configuration file.  Creating a plan should 
        /// succeed if and only if it can be applied successfully to a designated 
        /// <see cref="Host"/>.  If it would fail, it should always throw a 
        /// <seealso cref="ConfigurationException"/>.
        /// <para/>Once the plan is created, don't forget to <seealso cref="Apply"/> it.
        /// </summary>
        public sealed class ConfigurationPlan
        {
            public readonly object Host;
            public readonly Version Source;
            private readonly ConfigNode _Node;
            internal ConfigurationPlan(object host, Version source, ConfigNode node) { this.Host = host; this.Source = source; this._Node = node; }
            public void Apply() { _Node.ApplyTo(Host); }
        }

        [DebuggerDisplay("ConfigNode {Name} = {_Value}")]
        internal sealed class ConfigNode
        {
            internal readonly string Name;
            internal readonly ConfigurationAttribute CodeAttribute;
            internal readonly MemberInfo ReflectionInfo;
            private IEnumerable<ConfigNode> Members;

            private object _Value = null;
            public object GetValue(object host) => _Value
                                                 ?? ((ReflectionInfo is PropertyInfo pinfo) ? (_Value = pinfo.GetValue(host))
                                                 : (ReflectionInfo is FieldInfo finfo) ? (_Value = finfo.GetValue(host))
                                                 : null);

            public Type Type => ReflectionInfo is PropertyInfo pinfo ? pinfo.PropertyType
                                                                     : ((FieldInfo)ReflectionInfo).FieldType;


            internal ConfigNode(string name, ConfigurationAttribute attrib, MemberInfo reflectionInfo)
            {
                this.Name = name;
                this.ReflectionInfo = reflectionInfo;
                this.CodeAttribute = attrib;
            }


            public void Save(XmlWriter writer, Version version, object host)
            {
                HashSet<object> visited = new HashSet<object>();
                Private_Save(this, host);

                void Private_Save(ConfigNode node, object _host)
                {

                    Type t = _host.GetType();
                    if (t.IsClass && !visited.Add(_host))
                        throw new ConfigurationException("Detected circularity in object graph.");

                    // Step #1 - identify all the properties that are marked for configuration.
                    this.Members = CreateMemberNodes(_host, version);

                    // Step #2 - if we have a leaf, add as an attribute and return.
                    if (!Members.Any())
                    {
                        TypeConverter converter = GetConverter(_host);
                        string strValue = (converter.CanConvertTo(typeof(string))) ? converter.ConvertToString(_host)
                                                                                   : _host.ToString();
                        writer.WriteAttributeString(node.Name, strValue);
                        return;
                    }

                    // Step #3 - otherwise, recursively call each member within a XML element
                    writer.WriteStartElement(node.Name);
                    foreach (var cn in Members)
                        Private_Save(cn, cn.GetValue(_host));
                    writer.WriteEndElement();
                }
            }


            public bool Import(XmlNode xmlNode, object host, Version sourceVersion, XmlDocument doc)
                => Private_Import(xmlNode, host, sourceVersion, doc);
            private bool Private_Import(XmlNode xmlNode, object host, Version sourceVersion, XmlDocument doc)
            {
                // Returns:  true if this should be converted, false if work is done.
                if (host == null) return true;
                if (this.CodeAttribute != null)
                {
                    if (this.CodeAttribute.TypeConverter != default)
                        return true;
                    if (!this.CodeAttribute.ApplyToSubsections)
                        return false;
                }

                this.Members = CreateMemberNodes(host, sourceVersion).ToArray();    // Must be a hardened array.
                if (!Members.Any())
                    return true;

                foreach (ConfigNode member in this.Members)
                {
                    string name = member.Name;
                    if (member.CodeAttribute is ConfigurationDeclaredAttribute decAttr)
                        name = (decAttr.Key ?? name);

                    XmlAttribute xmlAttrib = xmlNode.Attributes[name];
                    if (xmlAttrib != null)
                    {
                        member._Value = _Convert(xmlAttrib.Value, member);
                        //member.ApplyTo(host);
                        continue;
                    }

                    XmlNode xmlChildNode = xmlNode.SelectSingleNode(name);
                    if (xmlChildNode == null)
                        throw new ConfigurationException("Cannot configure host of type " + host.GetType().Name + ": missing configuration for " + member.Name);

                    if (member.Private_Import(xmlChildNode, member.GetValue(host), sourceVersion, doc))
                    {
                        member._Value = _Convert(xmlChildNode?.Value, member);
                        //member.ApplyTo(host);
                    }
                }
                return false;

                object _Convert(string _value, ConfigNode _node)
                {
                    TypeConverter converter = _node.GetConverter();
                    if (converter is ConfigurationConverter configurationConverter)
                    {
                        Dictionary<string, string> kvps = new Dictionary<string, string>();
                        foreach (string xpath in _node.CodeAttribute.ConversionXPaths)
                        {
                            if (kvps.ContainsKey(xpath))
                                throw new ConfigurationException("Configuration converter of type " + converter.GetType() + " seeks two entries for same xpath '" + xpath + "'");
                            kvps[xpath] = doc.SelectSingleNode(xpath).Value;
                        }
                        return configurationConverter.ConvertFrom(_value, kvps.ToArray());
                    }
                    return converter.ConvertFromString(_value);
                }
            }


            public void ApplyTo(object host)
            {
                if (this.Members == null) return;
                foreach (var member in this.Members)
                {
                    object newValue = member.GetValue(host);
                    if (member.ReflectionInfo is PropertyInfo pinfo)
                        pinfo.SetValue(host, newValue);
                    else if (member.ReflectionInfo is FieldInfo finfo)
                        finfo.SetValue(host, newValue);
                    else
                        throw new ConfigurationException("Invalid reflection info:" + this.ReflectionInfo.ToString());
                    member.ApplyTo(newValue);
                }
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
            private TypeConverter GetConverter(object obj = null)
            {
                if (obj == null && this.CodeAttribute != null && this.CodeAttribute.TypeConverter != null)
                    return (TypeConverter)Activator.CreateInstance(this.CodeAttribute.TypeConverter);
                else if (obj != null)
                    return _ConverterByType(obj.GetType());
                else if (this.ReflectionInfo is PropertyInfo pinfo)
                    return _ConverterByType(pinfo.PropertyType);
                else if (this.ReflectionInfo is FieldInfo finfo)
                    return _ConverterByType(finfo.FieldType);
                else
                    throw new ConfigurationException("Exactly what kind of converter do you want?");

                TypeConverter _ConverterByType(Type t)
                {
                    if (t == typeof(string) || t == typeof(String)) return new PassThruConverter();
                    if (t == typeof(int) || t == typeof(Int32)) return new Int32Converter();
                    if (t == typeof(double) || t == typeof(Double)) return new DoubleConverter();
                    throw new ConfigurationException("No converter defined for object of type " + t.Name);
                }
            }

            internal static IEnumerable<ConfigNode> CreateMemberNodes(object host, Version applicableVersion)
            {
                HashSet<string> members = new HashSet<string>();
                Type t = host.GetType();
                foreach (var dca in t.GetCustomAttributes<ConfigurationDeclaredAttribute>()
                                             .Where(_dca => _dca.Versions.Contains(applicableVersion)))
                {
                    if (!members.Add(dca.Key))
                        throw new ConfigurationException("Multiple configurations handle key '{0}' for version {1}", dca.Key, applicableVersion.ToString());
                    else if (t.GetProperty(dca.MemberName, BINDING_FILTER) is PropertyInfo pinfo)
                        yield return new ConfigNode(dca.Key, dca, pinfo);
                    else if (t.GetField(dca.MemberName, BINDING_FILTER) is FieldInfo finfo)
                        yield return new ConfigNode(dca.Key, dca, finfo);
                }
                foreach (PropertyInfo pinfo in t.GetProperties(BINDING_FILTER))
                {
                    foreach (var ca in pinfo.GetCustomAttributes<ConfigurationAttribute>()
                                            .Where(_ca => _ca.Versions.Contains(applicableVersion)))
                    {
                        string name = string.IsNullOrWhiteSpace(ca.Key) ? pinfo.Name : ca.Key;
                        if (!members.Add(name))
                            throw new ConfigurationException("Multiple configurations handle key '{0}' for version {1}", name, applicableVersion.ToString());
                        yield return new ConfigNode(name, ca, pinfo);
                    }
                }
                foreach (FieldInfo finfo in t.GetFields(BINDING_FILTER))
                {
                    foreach (var ca in finfo.GetCustomAttributes<ConfigurationAttribute>()
                                            .Where(_ca => _ca.Versions.Contains(applicableVersion)))
                    {
                        string name = string.IsNullOrWhiteSpace(ca.Key) ? finfo.Name : ca.Key;
                        if (!members.Add(name))
                            throw new ConfigurationException("Multiple configurations handle key '{0}' for version {1}", name, applicableVersion.ToString());
                        yield return new ConfigNode(name, ca, finfo);
                    }
                }
            }

            public override string ToString() => "ConfigNode \"" + this.ReflectionInfo.Name + "\"";
        }

    }




    public class ConfigurationException : Exception
    {
        public ConfigurationException(string message, Exception inner) : base(message, inner) { }
        public ConfigurationException(params string[] message) : base(string.Format(message[0], message.Skip(1).ToArray())) { }
    }

}

