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
    public enum ImportFlags
    {
        None = 0,
        DefaultOnMissing = 1 << 0,
        ThrowOnMissing = 1 << 1,

        /// <summary>
        /// Someday, there may be more flags than just ThrowOnMissing
        /// </summary>
        AllErrors = ThrowOnMissing
    }

    [DebuggerDisplay("ConfigNode {Name} = {_Value}")]
    internal sealed class ConfigNode
    {
        private const BindingFlags BINDING_FILTER = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

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
        public Type GetReflectionType() {
            if (ReflectionInfo is PropertyInfo pinfo) return pinfo.PropertyType;
            if (ReflectionInfo is FieldInfo finfo) return finfo.FieldType;
            throw new InvalidOperationException("Where is the " + nameof(ReflectionInfo) + "?");
        }



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

        

        public void Import(XmlNode xmlNode, object host, Version sourceVersion, XmlDocument doc, ImportFlags flags, object opaque = null)
            => Private_Import(xmlNode, host, sourceVersion, doc, flags, opaque);
        private bool Private_Import(XmlNode xmlNode, object host, Version sourceVersion, XmlDocument doc, ImportFlags flags, object opaque)
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
                member._Value = member.GetValue(host);

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
                    if ((flags & ImportFlags.DefaultOnMissing) != ImportFlags.None)
                    {
                        if (!member.TryGetDefault(out object def) && (flags & ImportFlags.ThrowOnMissing) != ImportFlags.None)
                            throw new ConfigurationException("Cannot configure host of type " + host.GetType().Name + ": missing default for " + member.Name);
                        else
                            def = member.GetValue(host);
                        member._Value = def;
                    }
                    else if ((flags & ImportFlags.ThrowOnMissing) != ImportFlags.None)
                    {
                        throw new ConfigurationException("Cannot configure host of type " + host.GetType().Name + ": missing configuration for " + member.Name);
                    }   
                    continue;
                }

                // If the ConfigNode has a node to examine, check if it can be imported 
                // recursively.  If it turns out to be a leaf ConfigNode, try to convert & 
                // import the value.  Failing that, look for a default.  Note that GetValue() 
                // looks for a default.
                if (member.Private_Import(xmlChildNode, member._Value, sourceVersion, doc, flags, opaque))
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
                    object preconfigured = _node._Value;
                    ConfigurationContext context = new ConfigurationContext() { Host = host, Preconfigured = preconfigured, Opaque = opaque, XPaths = kvps };
                    result = configurationConverter.ConvertFrom(context, _value);
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

            this.Members = CreateMemberNodes(host, Configuration.GetCurrentVersion()).ToArray();
            foreach (var member in this.Members)
            {
                object memberValue = member.GetValue(host);
                member.Default(memberValue);
                if (member.Members == null || member.Members.Any()) return;
                if (!member.TryGetDefault(out object d))
                {
                    // If we can't get default on a leaf, that's a problem.  Otherwise, it can be ignored.
                    if (!member.Members.Any() && member.GetReflectionType().IsValueType)
                        throw new ConfigurationException("Failed to obtain default for " + member.Name);
                    d = member.GetValue(host);
                }   
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
                if (member._Value == null && member.GetReflectionType().IsValueType)
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
                throw new ConfigurationException("Leaf configuration objects must have converters.  No converter defined for object of type " + t.Name);
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
