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
            private ConfigNode[] Members;

            private object _Value = null;
            public object GetValue(object host)
            {
                // Each of these steps could still get a null value.  Gotta check each time.
                if (_Value == null && ReflectionInfo is PropertyInfo pinfo) _Value = pinfo.GetValue(host);
                else if (_Value == null && ReflectionInfo is FieldInfo finfo) _Value = finfo.GetValue(host);
                if (_Value == null && TryGetDefault(out object obj)) _Value = obj;
                return _Value;
            }
            public Type GetReflectionType() => (ReflectionInfo is PropertyInfo pinfo) ? pinfo.PropertyType
                                               : (ReflectionInfo is FieldInfo finfo) ? finfo.FieldType
                                               : throw new InvalidOperationException("Where is the " + nameof(ReflectionInfo) + "?");
                                   

            public bool TryGetDefault(out object @default)
            {
                if (CodeAttribute.DefaultGiven) { @default = CodeAttribute.DefaultValue; return true; }
                Type rt = GetReflectionType();
                if (rt.IsValueType)
                {
                    @default = Activator.CreateInstance(rt);
                    return true;
                }
                else if (rt == typeof(string) || rt == typeof(String))
                {
                    @default = "";
                    return true;
                }
                try
                {
                    // If there is no zero-argument constructor for a class-type 'rt', this will throw.
                    @default = Activator.CreateInstance(rt);
                    return true;
                }
                catch
                {
                    @default = default;
                    return false;
                }
            }

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
                _Save_Leaf_Or_Recursive(this, host);

                bool _Save_Leaf_Or_Recursive(ConfigNode node, object _host)
                {

                    Type t = _host.GetType();
                    if (t.IsClass && !visited.Add(_host))
                        throw new ConfigurationException("Detected circularity in object graph.");

                    // Step #1 - identify all the properties that are marked for configuration.
                    this.Members = CreateMemberNodes(_host, version).ToArray();

                    // Step #2 - if we have a leaf, return to indicate it's a leaf.
                    if (!Members.Any())
                        return true;

                    // Step #3 - otherwise, recursively call each member within a XML element
                    writer.WriteStartElement(node.Name);
                    foreach (var cn in Members)
                    {
                        object childValue = cn.GetValue(_host);
                        if (_Save_Leaf_Or_Recursive(cn, childValue))
                        {
                            TypeConverter converter = cn.GetConverter(childValue);
                            string strValue;
                            if (converter is ConfigurationConverter configConverter)
                            {
                                strValue = configConverter.ConvertTo(childValue, _host);
                            }
                            else
                                strValue = (converter.CanConvertTo(typeof(string))) ? converter.ConvertToString(childValue)
                                                                                    : childValue.ToString();
                            writer.WriteAttributeString(cn.Name, strValue);
                        }
                    }
                        
                    writer.WriteEndElement();
                    return false;
                }
            }


            public void Import(XmlNode xmlNode, object host, Version sourceVersion, XmlDocument doc)
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

                    // If the ConfigNode coincides with an XML attribute, read that attribute and continue on.
                    XmlAttribute xmlAttrib = xmlNode.Attributes[name];
                    if (xmlAttrib != null)
                    {
                        if (!_TryConvert(xmlAttrib.Value, member, out object result)
                                && !member.TryGetDefault(out result))
                            throw new ConfigurationException("Cannot configure host of type " + host.GetType().Name + ": cannot convert attribute \"" + xmlAttrib.Value + "\" to type of " + member.Name);
                        member._Value = result;
                        continue;
                    }

                    // If the ConfigNode has no coincidental node or attribute, look for a default.
                    XmlNode xmlChildNode = xmlNode.SelectSingleNode(name);
                    if (xmlChildNode == null)
                    {
                        if (!member.TryGetDefault(out object def))
                            throw new ConfigurationException("Cannot configure host of type " + host.GetType().Name + ": missing configuration for " + member.Name);
                        member._Value = def;
                        continue;
                    }   

                    // If the ConfigNode has a node to examine, check if it can be imported 
                    // recursively.  If it turns out to be a leaf ConfigNode, try to convert & 
                    // import the value.  Failing that, look for a default.  Note that GetValue() 
                    // looks for a default.
                    if (member.Private_Import(xmlChildNode, member.GetValue(host), sourceVersion, doc))
                    {
                        if (!_TryConvert(xmlChildNode?.Value, member, out object result))
                            throw new ConfigurationException("Cannot configure host of type " + host.GetType().Name + ": cannot convert \"" + xmlChildNode?.Value + "\" to type of " + member.Name);
                        member._Value = result;
                    }
                }
                return false;

                bool _TryConvert(string _value, ConfigNode _node, out object result)
                {
                    TypeConverter converter = _node.GetConverter();
                    if (!converter.CanConvertFrom(typeof(string)))
                    {
                        result = default;
                        return false;
                    }
                    else if (converter is ConfigurationConverter configurationConverter)
                    {
                        Dictionary<string, string> kvps = new Dictionary<string, string>();
                        if (_node.CodeAttribute.ConversionXPaths != null)
                        {
                            foreach (string xpath in _node.CodeAttribute.ConversionXPaths)
                            {
                                if (kvps.ContainsKey(xpath))
                                    throw new ConfigurationException("Configuration converter of type " + converter.GetType() + " seeks two entries for same xpath '" + xpath + "'");
                                kvps[xpath] = doc.SelectSingleNode(xpath).Value;
                            }
                        }
                        result = configurationConverter.ConvertFrom(_value, kvps.ToArray());
                    }
                    else 
                        result = converter.ConvertFromString(_value);
                    return true;
                }
            }

            public void Default(object host)
            {
                if (host == null)
                {
                    Type rt = GetReflectionType();
                    if (rt == typeof(string) || rt == typeof(String)) { _Value = ""; return; }
                    throw new ConfigurationException("Cannot determine default value for null object: " + this.Name);
                }
                    
                this.Members = CreateMemberNodes(host, GetCurrentVersion()).ToArray();
                foreach (var member in this.Members)
                {
                    object memberValue = member.GetValue(host);
                    member.Default(memberValue);
                    if (member.Members == null || member.Members.Any()) return;
                    if (!member.TryGetDefault(out object d))
                        throw new ConfigurationException("Failed to obtain default for " + member.Name);
                    member._Value = d;
                    if (member._Value == null)
                        throw new ConfigurationException("Cannot determine default for " + member.Name);
                }   
            }

            public void ApplyTo(object host)
            {
                if (this.Members == null) return;
                foreach (var member in this.Members)
                {
                    if (member._Value == null && GetReflectionType().IsValueType)
                        throw new ConfigurationException("Invalid value for member " + member.Name);
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
            private static bool IsStringType(Type t) => t == typeof(string) || t == typeof(String);

            private sealed class PassThruConverter : TypeConverter
            {
                public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => IsStringType(sourceType);
                public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => IsStringType(destinationType);
                public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => value;
                public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) => value;
            }
            

            private TypeConverter GetConverter(object obj = null)
            {
                if (this.CodeAttribute != null && this.CodeAttribute.TypeConverter != null)
                    return (TypeConverter)Activator.CreateInstance(this.CodeAttribute.TypeConverter);                
                else if (this.ReflectionInfo is PropertyInfo pinfo)
                    return _ConverterByType(pinfo.PropertyType);
                else if (this.ReflectionInfo is FieldInfo finfo)
                    return _ConverterByType(finfo.FieldType);
                else if (obj != null)
                    return _ConverterByType(obj.GetType());
                else
                    throw new ConfigurationException("Exactly what kind of converter do you want?");

                TypeConverter _ConverterByType(Type t)
                {
                    if (t == typeof(string) || t == typeof(String)) return new PassThruConverter();
                    if (t == typeof(int) || t == typeof(Int32)) return new Int32Converter();
                    if (t == typeof(double) || t == typeof(Double)) return new DoubleConverter();
                    if (typeof(IEnumerable<string>).IsAssignableFrom(t)) return new Converters.ListStringsConverter();
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

