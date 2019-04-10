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

        private readonly IList<Group> _Groups = new List<Group>();
        private readonly IList<Option> _Options = new List<Option>();
        private static readonly object Chosen = true;

        /// <summary>
        /// Creates a new <see cref="Profile{T}"/>, which can automagically interpret a set of string arguments into the 
        /// property of the given profile type.
        /// </summary>
        /// <param name="type"></param>
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

            foreach (MethodInfo mInfo in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                _Add_Parser(mInfo);
            foreach (MethodInfo mInfo in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
                _Add_Parser(mInfo);

            void _Add_Parser(MethodInfo mInfo)
            {
                foreach (ParseAttribute parseAttr in mInfo.GetCustomAttributes<ParseAttribute>())
                {
                    foreach (string name in parseAttr.Properties)
                    {
                        Option o = _Options.FirstOrDefault(opt => opt.IsMatch(name));
                        if (o == null)
                            throw new NamingException("A parser cannot be assigned for an Option with the name " + name + ", because it does not exist.");
                        if (o.Parser != null)
                            throw new StructureException("Option " + name + " cannot be assigned a parser twice.");
                        o.Parser = mInfo;
                    }
                    
                }
            }

            void _Add_Throw(MemberInfo mInfo)
            {
                IEnumerable<AliasAttribute> aliases = mInfo.GetCustomAttributes<AliasAttribute>();
                IEnumerable<GroupAttribute> groups = mInfo.GetCustomAttributes<GroupAttribute>();
                HelpAttribute helpAttr = mInfo.GetCustomAttribute<HelpAttribute>();

                // Ensure there's at list one alias.  If there are none, it matches the member's name.
                if (!aliases.Any())
                    if (groups != null || helpAttr != null)
                        aliases = new List<AliasAttribute>() { new AliasAttribute(mInfo.Name) };

                // Create the new Option
                Option o;                
                if (mInfo is PropertyInfo pInfo) o = new Option(pInfo, aliases, groups, helpAttr);
                else if (mInfo is FieldInfo fInfo) o = new Option(fInfo, aliases, groups, helpAttr);
                else throw new NotImplementedException();
                
                // Check for alias duplication.
                if (_Options.Any(existing => existing.IsMatch(o.Name)))
                    throw new NamingException("An Option with the name \"" + o.Name + "\" already exists");
                if (o.Flag != '\0' && _Options.Any(existing => o.Flag == existing.Flag))
                    throw new NamingException("An Option with the flag '" + o.Flag + "' already exists");
                foreach (AliasAttribute a in aliases)
                    if (_Options.Any(existing => existing.IsMatch(a.Alias)))
                        throw new NamingException("Alias conflict for \"" + a.Alias + "\".)");

                // With the new option, it shouldn't have a parser assigned yet.
                o.Parser = null;

                // Add the option to the appropriate groups.                
                _Options.Add(o);
                foreach (GroupAttribute ga in o.GroupsAttr)
                {
                    Group g = _Groups.FirstOrDefault(existing => existing.Name == ga.Name);
                    if (g == null)
                    {
                        _Groups.Add(g = new Group(ga.Name));
                        if (ga.Required != null) g.Required = ga.Required;
                    }
                    g.Options.Add(o);
                    if (ga.Required != g.Required)
                    {
                        if (g.Required == null)
                            g.Required = ga.Required;
                        else if (ga.Required != null)
                            throw new GroupException("Option \"" + o.Name + "\" has requirement inconsistency for group " + g.Name);
                    }
                        
                    foreach (string ea in ga.Exclusions)
                    {
                        Group exc = _Groups.FirstOrDefault(existing => existing.Name == ea);
                        if (exc == null) _Groups.Add(exc = new Group(ea));
                        else if (exc == g)
                            throw new GroupException("Option \"" + o.Name + "\" in group \"" + g.Name + "\" cannot exclude its own group.");
                        g.Excluded.Add(exc);
                        exc.Excluded.Add(g);
                    }
                }
            }
        }


        internal class Option
        {
            
            public readonly IEnumerable<AliasAttribute> Aliases;
            public readonly IEnumerable<GroupAttribute> GroupsAttr;
            public readonly HelpAttribute Help;
            public readonly MemberInfo Info;
            public MethodInfo Parser = null;
            public readonly char Flag = '\0';
            public string Name => (Aliases != null && Aliases.Any()) ? Aliases.First().Alias
                                : Info.Name;
            
            private Option(MemberInfo mInfo, IEnumerable<AliasAttribute> aliases, IEnumerable<GroupAttribute> groups, HelpAttribute help)
            {
                List<AliasAttribute> aliasList = new List<AliasAttribute>();
                this.Aliases = aliasList;
                if (aliases == null) aliasList.Add(new AliasAttribute(true, mInfo.Name));
                this.GroupsAttr = groups;
                this.Help = help;
                Info = mInfo;
                
                foreach (AliasAttribute aliasAttr in aliases)
                {
                    if (aliasList.Count > 0 && aliasAttr == aliasList[0] && IsMatch(aliasAttr.Alias))
                        throw new NamingException("Option \"" + this.Name + "\" has duplicate alias \"" + aliasAttr.Alias + "\".");                    
                    if (aliasAttr.Alias.Length == 1)
                    {
                        if (typeof(T) != typeof(bool) && typeof(T) != typeof(Boolean))                        
                            throw new StructureException("Flag alias \"" + aliasAttr.Alias[0] + "\" my be applied only to bool arguments.");
                        if (Flag != '\0')
                            throw new StructureException("Option \"" + this.Name + "\" has flag conflict between '" + Flag + "' and \"" + aliasAttr.Alias[0] + "\".");
                        Flag = aliasAttr.Alias[0];
                    }
                    aliasList.Add(aliasAttr);
                }
                
                if (groups.Select(g => g.Name).Distinct().Count() != groups.Count())
                    throw new GroupException("On Option \"" + this.Name + "\", duplicate group names.");
                foreach (GroupAttribute ga in groups)
                    if (ga.Exclusions.Distinct().Count() != ga.Exclusions.Count())
                        throw new ParsingException("On Option \"" + this.Name + "\", in group \"" + ga.Name + "\", there exists a duplicate exclusion.");
            }

            public Option(PropertyInfo pInfo, IEnumerable<AliasAttribute> aliases, IEnumerable<GroupAttribute> groups, HelpAttribute help)
                : this((MemberInfo)pInfo, aliases, groups, help) { }
            public Option(FieldInfo fInfo, IEnumerable<AliasAttribute> aliases, IEnumerable<GroupAttribute> groups, HelpAttribute help)
                : this((MemberInfo)fInfo, aliases, groups, help) { }

            private bool _IsSet = false;
            private object _Value = default(T);
            internal void SetValue(object anyTypeValue) { _Value = anyTypeValue; _IsSet = true; }
            public T Value { get => _IsSet ? (T)_Value : default(T); set { _Value = value; _IsSet = true; } }
            public void Reset() => _IsSet = false;

            public bool IsMatch(char chr) => chr == Flag;
            public bool IsMatch(string arg)
            {
                if (arg == Name) return true;
                if (Aliases == null || !Aliases.Any()) return arg.ToLower() == Name.ToLower();
                if (Aliases == null) return false;
                foreach (AliasAttribute aliasAttr in Aliases)
                {
                    if (aliasAttr.IsCaseSensitive && arg == aliasAttr.Alias)
                        return true;
                    else if (arg.ToLower() == aliasAttr.Alias.ToLower())
                        return true;
                }
                return false;
            }

            public void Throw()
            {
                if (Help != null) throw new ParsingException(Help._Message);
                throw new ParsingException("<" + Name + ">");
            }

            public bool TryParse(string argValue)
            {
                if (Value != null)
                    throw new ParsingException("Argument \"" + Name + "\" is supplied more than once.");
                Value = default(T);
                if (String.IsNullOrWhiteSpace(argValue))
                    return false;                
                if (Parser != null)
                {
                    try
                    {
                        Value = (T)Parser.Invoke(Value, new string[] { argValue });
                    }
                    catch (Exception ex)
                    {
                        throw new Arguments.ParsingException("Assigned parser for \"" + Name + "\" failed to parse " + argValue, ex );
                    }
                    return Value != null;
                }

                Type t = typeof(T);                
                if (t== typeof(string) || t == typeof(String))
                {
                    _Value = argValue;
                    return true;
                }
                else if (t == typeof(bool) || t == typeof(Boolean))
                {
                    bool success = bool.TryParse(argValue, out bool b);
                    _Value = b;
                    return success;
                }
                else if (t == typeof(int) || t == typeof(Int32))
                {
                    bool success = int.TryParse(argValue, out int i);
                    _Value = i;
                    return success;
                }
                else if (t == typeof(float) || t == typeof(Single))
                {
                    bool success = float.TryParse(argValue, out float f);
                    _Value = f;
                    return success;
                }
                else if (t == typeof(double) || t == typeof(Double))
                {
                    bool success = double.TryParse(argValue, out double d);
                    _Value = d;
                    return success;
                }
                return false;
            }

            public override string ToString() => "<" + Name + ">" + (Value == null ? "null" : Value.ToString()) + "</" + Name + ">";
        }


        internal class Group
        {
            public readonly string Name;
            public readonly HashSet<Group> Excluded = new HashSet<Group>();
            public readonly IList<Option> Options = new List<Option>();
            public bool? Required;

            public Group (string name, params Option[] options)
            {
                this.Name = name;
                foreach (Option o in options)
                    Add(o);
            }            
            public void Add(Option option)
            {
                if (Options.Any(o => option.Name == o.Name))
                    throw new GroupException("A member with name " + option.Name + " already exists on group "
                                                + Name + ".");
                Options.Add(option);

            }
            public bool IsActive => Options.All(o => o.Value != null);
        }


        /// <summary>
        /// Parses a single argument.  Returns the number of arguments parsed.
        /// </summary>
        /// <param name="arg">The whitespace-delineated argument to parse.</param>
        /// <param name="resultObj"></param>
        /// <param name="nextArg"></param>
        /// <returns>Returns 0, 1, or 2 (if nextArg is supplied), which is the number of arguments that have been parsed.</returns>
        public int Parse(string arg, string nextArg = null)
        {
            if (String.IsNullOrWhiteSpace(arg)) return 0;

            // An option split by '=' or ':' ?
            string[] split = arg.Split('=');
            if (split.Length > 2)
                throw new ParsingException("Only one value can be assigned to argument \"" + split[0] + "\".");
            else if (split.Length == 1)
            {
                int cIdx = arg.IndexOf(':');
                if (cIdx > 0) split = new string[] { arg.Substring(0, cIdx), arg.Substring(cIdx + 1) };
            }
            else if (split.Length == 2)
            {
                Option o = _Options.FirstOrDefault(existing => existing.IsMatch(split[0]));
                if (o == null)
                    throw new ParsingException("Argument \"" + split[0] + "\" is not defined.");
                if (!o.TryParse(split[1]))
                    throw new ParsingException("Cannot parse value \"" + arg[1] + "\" on argument \"" + split[0] + "\".");
                return 1;
            }


            // Aggregated flag options?  All options in an aggregated flag must match options in a group, or none 
            // of them can.
            else if (arg[0] == '-' && arg.Length > 1 && arg[1] != '-')
            {
                char[] aggFlags = arg.Substring(1).ToCharArray();
                Option[] joined = aggFlags.Join(_Options, flag => flag, o => o.Flag, (flag, o) => o).ToArray();
                if (joined.Length == aggFlags.Length)
                {
                    foreach (Option existing in joined)
                        existing.SetValue(Chosen);
                    return 1;
                }
            }

            // A standard parse?                
            {
                Option o = _Options.FirstOrDefault(existing => existing.IsMatch(arg));
                if (o == null)
                    throw new ParsingException("Argument \"" + arg + "\" is not defined.");
                if (typeof(T) == typeof(bool) || typeof(T) == typeof(Boolean))
                {
                    o.SetValue(Chosen);
                    return 1;
                }
                if (nextArg == null)
                    o.Throw();
                
                if (!o.TryParse(nextArg))
                    o.Throw();

                return 2;
            }
        }

        /// <summary>Ensures that all of the argument rules are respected among the currently-built options.</summary>
        public void Validate()
        {
            // Step #1 - Check for inactive-but-required groups.
            {
                Group g = _Groups.FirstOrDefault(g1 => g1.Required == true && !g1.IsActive);
                if (g != null) throw new ParsingException("Required group " + g.Name + " is not active.");
            }

            // Step #2 - Check for group exclusion conflicts.
            {
                var conflicted = _Groups.Where(g => g.Excluded.Any(g1 => g1.IsActive));
                if (conflicted.Any())
                    throw new ParsingException("There are " + conflicted.Count() + " groups which are mutually exclusive.");
            }

            // Step #3 - Check for unfulfilled options.
            {
                IEnumerable<Option> unfulfilled =
                    _Options.Where(o => o.Value != null
                                        && o.GroupsAttr.Any()
                                        && _Groups.Where(g => g.Options.Contains(o)).All(g => !g.IsActive));
                if (unfulfilled.Any())
                    throw new ParsingException("Argument " + unfulfilled.First().Name + " requires additional arguments.");
            }
        }

        /// <summary>
        /// Applies the parsed options currently stored in the parser to the given object.
        /// </summary>
        /// <param name="resultObj"></param>
        public void Apply(T resultObj)
        {

            // Step #3 - Finally, assign the values to the properties/fields on the result object.
            foreach (Option o in _Options.Where(o1 => o1.Value != null))
            {
                try
                {
                    if (o.Info is PropertyInfo pInfo)
                    {
                        if (!pInfo.PropertyType.IsAssignableFrom(o.Value.GetType()))
                            throw new ParsingException("Parser for argument " + o.Name + " returned invalid type " + o.Value.GetType().Name + ", must be a " + pInfo.PropertyType.Name + ".");
                        pInfo.SetValue(resultObj, o.Value);
                    }
                    else if (o.Info is FieldInfo fInfo)
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
        /// <param name="resultObj"></param>
        /// <param name="ignores">Optional.  An explicit array of string arguments you want to have ignored.</param>
        public void Parse(IEnumerable<string> args, T resultObj, IEnumerable<string> ignores = null)
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

                int parsed = Parse(arg, i == strs.Length ? null : strs[i + 1]);
                if (parsed == 0) throw new Exception("Empty arguments cannot be parsed.");
                i += (parsed - 1);
            }

            // Step #2 Validate.
            Validate();
            
            // Step #3 Apply
            Apply(resultObj);

            // Step #4 Reset
            ResetAll();
        }


        /// <summary>Resets the stored value of all options.</summary>
        public void ResetAll()
        {
            foreach (Option o in _Options) o.Reset();
        }


    }
}
