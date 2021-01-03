using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Arguments
{

    /// <summary>
    /// Derived through reflection from the code of the options object.
    /// </summary>
    public class Profile<T>
    {
        /// <summary>The default of whether a group is required for an argument profile.</summary>
        public static readonly bool DEFAULT_GROUP_REQUIRED = false;

        private readonly IDictionary<string, Alias> _Aliases = new Dictionary<string, Alias>();
        private readonly IDictionary<char, Flag> _Flags = new Dictionary<char, Flag>();
        private readonly IDictionary<string, Group> _Groups = new Dictionary<string, Group>();
        private readonly IDictionary<string, MethodInfo> _Invocations = new Dictionary<string, MethodInfo>();


        private static readonly object Chosen = true;

        /// <summary>
        /// Creates a new <see cref="Profile{T}"/>, which can automagically interpret a set of string arguments into the 
        /// property of the given profile type.
        /// </summary>
        public Profile()
        {
            Type type = typeof(T);

            // Assign all the property-attached options to their groups            
            foreach (PropertyInfo pInfo in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                _Add_Throw(pInfo);
            foreach (PropertyInfo pInfo in type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance))
                _Add_Throw(pInfo);

            // Assign all the field-attached options to their groups
            foreach (FieldInfo fInfo in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                _Add_Throw(fInfo);
            foreach (FieldInfo fInfo in type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                _Add_Throw(fInfo);

            // Methods can  be options too.
            foreach (MethodInfo mInfo in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                _Add_Throw(mInfo);
            foreach (MethodInfo mInfo in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
                _Add_Throw(mInfo);

            void _Add_Throw(MemberInfo mInfo)
            {
                if (mInfo.GetCustomAttributes<NoParseAttribute>().Any()) return;
                IEnumerable<AliasAttribute> aliasAttrs = mInfo.GetCustomAttributes<AliasAttribute>();
                IEnumerable<GroupAttribute> groupAttrs = mInfo.GetCustomAttributes<GroupAttribute>();
                IEnumerable<HelpAttribute> helpAttrs = mInfo.GetCustomAttributes<HelpAttribute>();
                if (helpAttrs.Count() > 1)
                    throw new ProfileException("Only one " + typeof(HelpAttribute).Name + " per member is allowed.");

                // Create the new Option for a property or field.
                Field o = null;
                string name = mInfo.Name;
                {
                    var nameAttr = aliasAttrs.FirstOrDefault(a => a.Alias != null && !string.IsNullOrWhiteSpace(a.Alias));
                    if (nameAttr != null) name = nameAttr.Alias;
                }
                if (mInfo is PropertyInfo pInfo) o = new Field(pInfo, name, helpAttrs.FirstOrDefault());
                else if (mInfo is FieldInfo fInfo) o = new Field(fInfo, name, helpAttrs.FirstOrDefault());

                // If we have an option, interpret the property or field.
                if (o != null)
                {
                    bool notAdded = true;

                    // Assign all the option's aliases and flags.
                    foreach (AliasAttribute attr in aliasAttrs)
                    {
                        // Flag before alias, to look for naming conflicts.
                        if (attr.Flag != '\0')
                        {
                            if (_Flags.ContainsKey(attr.Flag))
                                throw new NamingException("An Option \"" + _Flags[attr.Flag].Field.Name
                                                            + "\" with the flag '" + attr.Flag + "' already exists.");
                            Flag f = new Flag(attr.Flag, o);
                            _Flags[attr.Flag] = f;
                            notAdded = false;
                        }
                        else if (attr.Alias == null || String.IsNullOrWhiteSpace(attr.Alias))
                            throw new NamingException("No applied alias or flag in alias attribute.");
                        else if (attr.Alias.Length == 1 && _Flags.ContainsKey(attr.Alias[0]))
                            throw new NamingException("Naming conflict with between alias \"" + attr.Alias + "\" and flag '"
                                                        + attr.Alias + "' on Option " + _Flags[attr.Alias[0]].Field.Name + ".");
                        else if (_Aliases.ContainsKey(attr.Alias.ToLower()))
                            throw new NamingException("An Option with the name \"" + attr.Alias + "\" already exists.");
                        else
                        {
                            Alias a = new Alias(attr.Alias, o, attr.IsCaseSensitive);
                            _Aliases[a._Lower] = a;
                            notAdded = false;
                        }
                    }

                    // If there was no alias assignment, assign under the member name.
                    if (!aliasAttrs.Any() || notAdded)
                    {
                        if (_Aliases.ContainsKey(o.Name.ToLower()))
                            throw new NamingException("An Option with the name \"" + o.Name + "\" already exists.");
                        _Aliases[o.Name.ToLower()] = new Alias(o.Name, o, AliasAttribute.DEFAULT_CASE_SENSITIVE);
                    }

                    // With the new option, it shouldn't have a parser assigned yet.
                    o.Parser = null;

                    // Add the option to the appropriate groups.                
                    foreach (GroupAttribute ga in groupAttrs)
                    {
                        if (!_Groups.TryGetValue(ga.Name, out Group g))
                        {
                            string nameLower = ga.Name.ToLower();
                            _Groups[nameLower] = (g = new Group(nameLower));
                            if (ga.Required != null) g.Required = ga.Required;
                        }
                        if (ga.Required != g.Required)
                        {
                            if (g.Required == null)
                                g.Required = ga.Required;
                            else if (ga.Required != null)
                                throw new GroupException("Option \"" + o.Name + "\" has requirement inconsistency for group " + g.Name);
                        }
                        g.Fields.Add(o);

                        foreach (string ea in ga.Exclusions)
                        {
                            if (!_Groups.TryGetValue(ea.ToLower(), out Group exc))
                            {
                                _Groups[ea.ToLower()] = (exc = new Group(ea.ToLower()));
                                if (exc == g)
                                    throw new GroupException("Option \"" + o.Name + "\" in group \"" + g.Name + "\" cannot exclude its own group.");
                                g.Excluded.Add(exc);
                                exc.Excluded.Add(g);
                            }
                        }
                    }

                }

                // Apply the method to an existing option, or create a new method?
                else if (mInfo is MethodInfo methodInfo)
                {
                    IEnumerable<InvocationAttribute> methodAttrs = mInfo.GetCustomAttributes<InvocationAttribute>();
                    if (!methodAttrs.Any()) return;

                    foreach (InvocationAttribute methodAttr in methodAttrs)
                    {
                        // An invocation must accept exactly one method, a string.
                        string toLower = methodAttr.Invocation.ToLower();
                        ParameterInfo[] parms = methodInfo.GetParameters();
                        if (parms.Length != 1)
                            throw new ProfileException("Method must accept exactly 1 argument, a string.");
                        else if (!typeof(string).IsAssignableFrom(parms[0].ParameterType) && !typeof(String).IsAssignableFrom(parms[0].ParameterType))
                            throw new ProfileException("Invocation " + methodAttr.Invocation + " must take a string as its first and only argument.");
                        else if (_Aliases.TryGetValue(toLower, out Alias a))
                        {
                            // The method applies to an existing option.  It must return the correct type, and accept 
                            // a string.
                            if (a.Field.Parser != null)
                                throw new ProfileException("Option " + toLower + " is already associated with a parser.");
                            if (methodInfo.ReturnType != a.Field.Type)
                                throw new ProfileException("Parser assigned to " + a.Field.Name + " must return type " + a.Field.Type.Name + ".");
                            a.Field.Parser = methodInfo;
                        }
                        else if (toLower.Length == 1 && _Flags.TryGetValue(methodAttr.Invocation[0], out Flag f))
                        {
                            if (f.Field.Parser != null)
                                throw new ProfileException("Flag '" + f.Character + "' is already associated with a parser.");
                            f.Field.Parser = methodInfo;
                        }
                        else if (_Invocations.ContainsKey(toLower))
                            throw new ProfileException("A method invoked by \"" + methodAttr.Invocation + "\" already exists.");
                        else
                        {
                            // The method is a free invocation.
                            if (methodInfo.ReturnType != typeof(void))
                                throw new ProfileException("Invocation " + toLower + " must return type void.");
                            _Invocations[toLower] = methodInfo;
                        }
                    }
                }


            }
        }


        internal sealed class Field
        {
            public readonly HelpAttribute Help;
            public readonly MemberInfo Member;
            public readonly string Name;
            private MethodInfo _Parser = null;
            internal readonly Type Type;
            public MethodInfo Parser
            {
                get => _Parser; internal set
                {
                    if (value != null && value.ReturnType != Type)
                        throw new ProfileException("A parser must return the same type as the option (" + Type.Name + ").");
                    _Parser = value;
                }
            }

            private Field(MemberInfo mInfo, string name, HelpAttribute help = null)
            {
                List<AliasAttribute> aliasList = new List<AliasAttribute>();
                this.Help = help;
                this.Member = mInfo;
                this.Name = name;
            }
            public Field(PropertyInfo pInfo, string name, HelpAttribute help = null)
                : this((MemberInfo)pInfo, name, help) { Type = pInfo.PropertyType; }
            public Field(FieldInfo fInfo, string name, HelpAttribute help)
                : this((MemberInfo)fInfo, name, help) { Type = fInfo.FieldType; }

            internal bool IsSet = false;
            private object _Value = null;
            public object Value
            {
                get => IsSet ? _Value : null;
                set
                {
                    if (IsSet) throw new ParsingException("Field " + Name + " cannot be set more than once.");
                    _Value = value; IsSet = true;
                }
            }
            public void Reset() => IsSet = false;

            public bool TryParse(object hostObject, string argValue)
            {
                if (string.IsNullOrWhiteSpace(argValue))
                    return false;

                // If a parser is assigned, try that first.
                if (Parser != null)
                {
                    try
                    {
                        if (Parser.ReturnType == typeof(void))
                        {
                            Parser.Invoke(hostObject, new string[] { argValue });
                            Value = Chosen;
                        }
                        else
                        {
                            Value = Parser.Invoke(hostObject, new string[] { argValue });
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Arguments.ParsingException("Assigned parser for \"" + Name + "\" failed to parse " + argValue, ex);
                    }
                    return Value != null;
                }

                // Try some of the standard types.
                {   
                    if (Type == typeof(string) || Type == typeof(String))
                    {
                        Value = argValue;
                        return true;
                    }
                    else if (Type == typeof(bool) || Type == typeof(Boolean))
                    {
                        bool success = bool.TryParse(argValue, out bool b);
                        Value = b;
                        return success;
                    }
                    else if (Type == typeof(int) || Type == typeof(Int32))
                    {
                        bool success = int.TryParse(argValue, out int i);
                        Value = i;
                        return success;
                    }
                    else if (Type == typeof(float) || Type == typeof(Single))
                    {
                        bool success = float.TryParse(argValue, out float f);
                        Value = f;
                        return success;
                    }
                    else if (Type == typeof(double) || Type == typeof(Double))
                    {
                        bool success = double.TryParse(argValue, out double d);
                        Value = d;
                        return success;
                    }
                    else if (Type == typeof(byte) || Type == typeof(Byte))
                    {
                        byte[] bytes = System.Convert.FromBase64String(argValue);
                        if (bytes.Length != 1) return false;
                        Value = bytes[0];
                    }
                    else if (Type == typeof(char) || Type == typeof(Char))
                    {
                        if (argValue.Length != 1) return false;
                        Value = argValue[0];
                        return true;
                    }
                    else if (Type == typeof(byte[]) || Type == typeof(Byte[]))
                    {
                        Value = System.Convert.FromBase64String(argValue);
                        return true;
                    }
                }

                return false;
            }

            public override string ToString() => "<" + Name + ">" + (Value == null ? "null" : Value.ToString()) + "</" + Name + ">";

            internal void Throw()
            {
                if (Help != null) throw new ParsingException(Help._Message);
                throw new ParsingException("Invalid syntax with argument " + Name);
            }
        }


        private class Flag
        {
            public readonly char Character;
            public readonly Field Field;
            public Flag(char flag, Field option) { this.Character = flag; this.Field = option; }
        }


        internal class Alias
        {
            internal readonly string _Lower;
            public readonly string Name;
            public readonly bool IsCaseSensitive;
            public readonly Field Field;
            public Alias(string name, Field option, bool isCaseSensitive = false)
            {
                this.Name = name;
                this.Field = option;
                this.IsCaseSensitive = isCaseSensitive;
                _Lower = name.ToLower();
            }

            public override bool Equals(object obj) => _Lower == ((Alias)obj)._Lower;

            public override int GetHashCode() => _Lower.GetHashCode();
            public override string ToString() => "Alias \"" + Name + "\"";
        }


        internal class Group
        {
            public readonly string Name;
            public readonly HashSet<Group> Excluded = new HashSet<Group>();
            public readonly IList<Field> Fields = new List<Field>();
            public bool? Required { get; internal set; }
            public bool IsRequired => Required != null && Required == true;

            public Group(string name, params Field[] options)
            {
                this.Name = name;
                foreach (Field o in options)
                    Add(o);
            }
            public void Add(Field option)
            {
                if (Fields.Any(o => option.Name == o.Name))
                    throw new GroupException("A member with name " + option.Name + " already exists on group "
                                                + Name + ".");
                Fields.Add(option);

            }
            public bool IsActive => Fields.All(o => o.IsSet);
            public override string ToString() => "Group \"" + this.Name + "\"";
        }


        private static readonly char[] ArgSplitters = { '=', ':', ' ', '/', '?' };
        /// <summary>Parses a single argument.  Returns the number of arguments parsed.</summary>
        public int Parse(T hostObj, string arg, string nextArg = null) => Parse(hostObj, arg, ArgSplitters, nextArg);
        /// <summary>Parses a single argument.  Returns the number of arguments parsed.</summary>
        /// <param name="arg">The whitespace-delineated argument to parse.</param>
        /// <param name="hostObj">The object whose settings are being set.</param>
        /// <param name="nextArg">Optional.  The next argument after the current one.  If it becomes part of the 
        /// parse, this method will return 2 (representing the number of arguments parsed).</param>
        /// <param name="splitters">The characters on which a single argument will be split into a key/value pair.  If 
        /// there are multiple occurrences of these splitters, the argument will be split only on the first.</param>
        /// <returns>Returns 0, 1, or 2 (if nextArg is supplied), which is the number of arguments that have been parsed.</returns>
        public int Parse(T hostObj, string arg, char[] splitters, string nextArg = null)
        {
            bool split = false;
            if (string.IsNullOrWhiteSpace(arg)) return 0;

            // An option split by arg splitters?
            {
                int splitIdx = arg.IndexOfAny(splitters);
                if (splitIdx > 0 && splitIdx < arg.Length - 1)
                {
                    nextArg = arg.Substring(splitIdx + 1);
                    arg = arg.Substring(0, splitIdx);
                    split = true;
                }
            }

            // Boolean aggregated flag options?  All options in an aggregated flag must match options in a group, or 
            // none of them can.
            if (arg[0] == '-' && arg.Length > 1 && arg[1] != '-')
            {
                if (arg.Length == 1) throw new ParsingException("Symbol '-' should precede a flag.");
                arg = arg.TrimStart('-');
                char[] aggFlags = arg.ToCharArray();

                // If every flag has an associated option, interpret the chars as flags.
                if (aggFlags.All(flag => _Flags.Keys.Contains(flag)))
                {
                    foreach (char flag in aggFlags)
                        _Flags[flag].Field.Value = Chosen;
                    return 1;
                }
            }

            // If there's no associated field, it cannot be parsed.
            string toLower = arg.ToLower();
            if (!_Aliases.TryGetValue(toLower, out Alias alias))
            {
                // Is there some other free invocation?
                if (_Invocations.TryGetValue(toLower, out MethodInfo m))
                {
                    try
                    {
                        m.Invoke(hostObj, new object[] { nextArg });
                    }
                    catch
                    {
                        throw new ParsingException("Exception in invocation \"" + toLower + "\".");
                    }
                }
                else
                    throw new ParsingException("Argument \"" + arg + "\" is not defined.");
            }


            // A single-arg (bool) parse?  
            else if (!split && (typeof(bool).IsAssignableFrom(alias.Field.Type)
                            || typeof(Boolean).IsAssignableFrom(alias.Field.Type)))
            {
                alias.Field.Value = Chosen;
                return 1;
            }

            // In all other cases, we're looking at a 2-arg parse.  If there's no nextArg, that's a bad thing.
            else if (nextArg == null)
                alias.Field.Throw();

            // Failure to parse requires a throw.
            else if (!alias.Field.TryParse(hostObj, nextArg))
                alias.Field.Throw();



            // If it was split, only one of the original arguments has been parsed.
            return split ? 1 : 2;
        }

        /// <summary>Ensures that all of the argument rules are respected among the currently-built options.</summary>
        public void Validate()
        {
            // Step #1 - Check for inactive-but-required groups.
            {
                Group g = _Groups.Values.FirstOrDefault(g1 => g1.Required == true && !g1.IsActive);
                if (g != null)
                {
                    string notActive = string.Join(",", g.Fields.Select(o => o.Name));
                    throw new ParsingException("Required group \"" + g.Name + "\" is not active with the following fields:  " + notActive);
                }
            }

            // Step #2 - Check for group exclusion conflicts.
            {
                var conflicted = _Groups.Values.Where(g => g.Excluded.Any(g1 => g1.IsActive));
                if (conflicted.Any())
                    throw new ParsingException("There are " + conflicted.Count() + " groups which are mutually exclusive.");
            }

            // Step #3 - Check for unfulfilled options.
            {
                //IEnumerable<Field> unfulfilled =
                //    _Aliases.Values.Where(a => a.Field.IsSet
                //                        && a.Groups.Any()
                //                        && _Groups.Where(g => g.Options.Contains(o)).All(g => !g.IsActive));
                //if (unfulfilled.Any())
                //    throw new ParsingException("Argument " + unfulfilled.First().Name + " requires additional arguments.");
            }
        }

        /// <summary>
        /// Applies the parsed options currently stored in the parser to the given object.
        /// </summary>
        /// <param name="resultObj"></param>
        public void Apply(T resultObj)
        {

            // Step #3 - Finally, assign the values to the properties/fields on the result object.
            foreach (Field o in _Aliases.Values.Select(a => a.Field).Where(f => f.IsSet))
            {
                try
                {
                    if (o.Member is PropertyInfo pInfo)
                    {
                        if (!pInfo.PropertyType.IsAssignableFrom(o.Value.GetType()))
                            throw new ParsingException("Parser for argument " + o.Name + " returned invalid type " + o.Value.GetType().Name + ", must be a " + pInfo.PropertyType.Name + ".");
                        pInfo.SetValue(resultObj, o.Value);
                    }
                    else if (o.Member is FieldInfo fInfo)
                    {
                        if (!fInfo.FieldType.IsAssignableFrom(o.Value.GetType()))
                            throw new ParsingException("Parser for argument " + o.Name + " returned invalid type " + o.Value.GetType().Name + ", must be a " + fInfo.FieldType + ".");
                        fInfo.SetValue(resultObj, o.Value);
                    }
                    else
                        throw new ParsingException("Unable to set " + o.Name);
                }
                catch (ParsingException) { throw; }
                catch (Exception) { o.Throw(); }
            }
        }

        /// <summary>
        /// Parses all the arguments given into the result object, in one act, and then resets the 
        /// options for the resultObj.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="hostObj"></param>
        /// <param name="ignores">Optional.  An explicit array of string arguments you want to have ignored.</param>
        public void Parse(IEnumerable<string> args, T hostObj, IEnumerable<string> ignores = null)
        {
            ResetAll();

            // Step #1 - Interpret the argument strings.
            string[] strs = args.ToArray();
            for (int i = 0; i < strs.Length; i++)
            {
                string arg = strs[i];

                // An ignored option?
                if (ignores != null && ignores.Any(ig => ig == arg))
                    continue;

                int parsed = Parse(hostObj, arg, i == strs.Length - 1 ? null : strs[i + 1]);
                if (parsed == 0) throw new Exception("Empty arguments cannot be parsed.");
                i += (parsed - 1);
            }

            // Step #2 Validate.
            Validate();

            // Step #3 Apply
            Apply(hostObj);

            // Step #4 Reset
            ResetAll();
        }


        /// <summary>Resets the stored value of all options.</summary>
        public void ResetAll()
        {
            foreach (Field f in _Aliases.Values.Select(a => a.Field)) f.Reset();
        }


        /// <summary>Converts the currently-stored options into a string with the indicated separators.</summary>
        /// <param name="keyValueSeparator">Optional.  If omitted, each key-value pair will be separated by the 
        /// string ": \t".</param>
        /// <param name="argSeparator">Optional.  If omitted, each argument will be separated by the string "\r\n".</param>
        public string Serialize(string keyValueSeparator = ": \t", string argSeparator = "\r\n")
        {
            StringBuilder sb = new StringBuilder();
            HashSet<Field> fields = new HashSet<Field>();
            foreach (Alias a in _Aliases.Values)
                if (fields.Add(a.Field))
                    _TryAppend(a.Field);
            foreach (Flag f in _Flags.Values)
                if (fields.Add(f.Field))
                    _TryAppend(f.Field);
            foreach (Group g in _Groups.Values)
                foreach (Field f in g.Fields)
                    if (fields.Add(f))
                        _TryAppend(f);

            return sb.ToString();

            void _TryAppend(Field field)
            {
                if (field.IsSet)
                    sb.Append(field.Name + keyValueSeparator + field.Value.ToString() + argSeparator);
                else if (_Groups.Values.Where(g => g.Required==true).Where(g => g.Fields.Contains(field)).Any())
                    sb.Append(field.Name + keyValueSeparator + "" + argSeparator);
            }
        }


    }
}
