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
    internal class Profile
    {
        /// <summary>The default of whether a group is required for an argument profile.</summary>
        public static readonly bool DEFAULT_GROUP_REQUIRED = false;

        private readonly IList<Group> _Groups = new List<Group>();
        private readonly IList<Option> _Options = new List<Option>();
        private static readonly object Chosen = true;

        public Profile(Type type)
        {

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

            void _Add_Throw(MemberInfo mInfo)
            {
                IEnumerable<AliasAttribute> aliases = mInfo.GetCustomAttributes<AliasAttribute>();
                IEnumerable<GroupAttribute> groups = mInfo.GetCustomAttributes<GroupAttribute>();
                HelpAttribute helpAttr = mInfo.GetCustomAttribute<HelpAttribute>();
                ParseAttribute patternAttr = mInfo.GetCustomAttribute<ParseAttribute>();

                // Ensure there's at list one alias.  If there are none, it matches the member's name.
                if (!aliases.Any())
                    if (groups != null || helpAttr != null || patternAttr != null)
                        aliases = new List<AliasAttribute>() { new AliasAttribute(mInfo.Name) };

                // Create the new Option
                Option o;
                if (mInfo is PropertyInfo pInfo) o = new Option(pInfo, aliases, groups, helpAttr, patternAttr);
                else if (mInfo is FieldInfo fInfo) o = new Option(fInfo, aliases, groups, helpAttr, patternAttr);
                else throw new NotImplementedException();
                
                // Check for alias duplication.
                if (_Options.Any(existing => existing.IsMatch(o.Name)))
                    throw new NamingException("An Option with the name \"" + o.Name + "\" already exists");
                if (o.Flag != '\0' && _Options.Any(existing => o.Flag == existing.Flag))
                    throw new NamingException("An Option with the flag '" + o.Flag + "' already exists");
                foreach (AliasAttribute a in aliases)
                    if (_Options.Any(existing => existing.IsMatch(a.Alias)))
                        throw new NamingException("Alias conflict for \"" + a.Alias + "\".)");

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


        public class Option
        {
            
            public readonly IEnumerable<AliasAttribute> Aliases;
            public readonly IEnumerable<GroupAttribute> GroupsAttr;
            public readonly HelpAttribute Help;
            public readonly ParseAttribute Pattern;
            public readonly MemberInfo Info;
            public readonly char Flag = '\0';
            public string Name => (Aliases != null && Aliases.Any()) ? Aliases.First().Alias
                                : Info.Name;
            public Type OptionType => (Info is PropertyInfo pInfo) ? pInfo.PropertyType 
                                    : (Info is FieldInfo fInfo) ? fInfo.FieldType 
                                    : typeof(object);

            private Option(MemberInfo mInfo, IEnumerable<AliasAttribute> aliases, IEnumerable<GroupAttribute> groups, HelpAttribute help, ParseAttribute pattern)
            {
                List<AliasAttribute> aliasList = new List<AliasAttribute>();
                this.Aliases = aliasList;
                if (aliases == null) aliasList.Add(new AliasAttribute(true, mInfo.Name));
                this.GroupsAttr = groups;
                this.Help = help;
                this.Pattern = pattern;
                Info = mInfo;
                
                foreach (AliasAttribute aliasAttr in aliases)
                {
                    if (aliasAttr == aliases.First()) continue;
                    if (IsMatch(aliasAttr.Alias))
                        throw new NamingException("Option \"" + this.Name + "\" has duplicate alias \"" + aliasAttr.Alias + "\".");                    
                    if (aliasAttr.Alias.Length == 1)
                    {
                        if (OptionType != typeof(bool) && OptionType != typeof(Boolean))                        
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

            public Option(PropertyInfo pInfo, IEnumerable<AliasAttribute> aliases, IEnumerable<GroupAttribute> groups, HelpAttribute help, ParseAttribute pattern)
                : this((MemberInfo)pInfo, aliases, groups, help, pattern) { }
            public Option(FieldInfo fInfo, IEnumerable<AliasAttribute> aliases, IEnumerable<GroupAttribute> groups, HelpAttribute help, ParseAttribute pattern)
                : this((MemberInfo)fInfo, aliases, groups, help, pattern) { }

            private bool _IsSet = false;
            private object _Value = null;
            public object Value { get => _IsSet ? _Value : null; set { _Value = value; _IsSet = true; } }
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
                    else if (arg.ToLower() == aliasAttr.ToString())
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
                Value = null;
                if (String.IsNullOrWhiteSpace(argValue))
                    return false;                
                if (Pattern != null && Pattern.Parser != null)
                {
                    try
                    {
                        Value = Pattern.Parser(argValue);
                        if (Value != null) return true;
                    }
                    catch { }
                    return false;
                }

                Type t = OptionType;
                if (t == typeof(string) || t == typeof(String))
                {
                    Value = argValue;
                    return true;
                }
                else if (t == typeof(bool) || t == typeof(Boolean))
                {
                    bool success = bool.TryParse(argValue, out bool b);
                    Value = b;
                    return success;
                }
                else if (t == typeof(int) || t == typeof(Int32))
                {
                    bool success = int.TryParse(argValue, out int i);
                    Value = i;
                    return success;
                }
                else if (t == typeof(float) || t == typeof(Single))
                {
                    bool success = float.TryParse(argValue, out float f);
                    Value = f;
                    return success;
                }
                else if (t == typeof(double) || t == typeof(Double))
                {
                    bool success = double.TryParse(argValue, out double d);
                    Value = d;
                    return success;
                }
                return false;
            }

            public override string ToString() => "<" + Name + ">" + (Value == null ? "null" : Value.ToString()) + "</" + Name + ">";
        }


        public class Group
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


        public void Parse(IEnumerable<string> args, object resultObj)
        {
            ResetAll();

            // Step #1 - Interpret the argument strings.
            string[] strs = args.ToArray();
            for (int i = 0; i < strs.Length; i++)
            {
                string arg = strs[i];
                if (String.IsNullOrWhiteSpace(arg)) continue;
                if (arg.Contains(" "))
                    throw new ParsingException("Argument " + i + " cannot contain any whitespace.");

                // An option split by '=' ?
                string[] split = arg.Split('=');
                if (split.Length > 2)
                    throw new ParsingException("Only one value can be assigned to argument \"" + split[0] + "\".");
                if (split.Length == 2)
                {
                    Option o = _Options.FirstOrDefault(existing => existing.IsMatch(split[0]));
                    if (o == null)
                        throw new ParsingException("Argument \"" + split[0] + "\" is not defined.");
                    if (!o.TryParse(split[1]))
                        throw new ParsingException("Cannot parse value \"" + arg[1] + "\" on argument \"" + split[0] + "\".");
                    continue;
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
                            existing.Value = Chosen;
                        continue;
                    }
                }

                // A standard parse?                
                {
                    Option o = _Options.FirstOrDefault(existing => existing.IsMatch(arg));
                    if (o == null)
                        throw new ParsingException("Argument \"" + arg + "\" is not defined.");
                    if (o.OptionType == typeof(bool) || o.OptionType == typeof(Boolean))
                    {
                        o.Value = Chosen;
                        continue;
                    }
                    if (i == strs.Length - 1)
                        o.Throw();

                    string nextArg = strs[i + 1];
                    if (!o.TryParse(nextArg))
                        o.Throw();
                    i++;
                    continue;
                }
            }

            // Step #2 - Check for inactive-but-required groups.
            {
                Group g = _Groups.FirstOrDefault(g1 => g1.Required == true && !g1.IsActive);
                if (g != null) throw new ParsingException("Required group " + g.Name + " is not active.");
            }

            // Step #3 - Check for group exclusion conflicts.
            {
                var conflicted = _Groups.Where(g => g.Excluded.Any(g1 => g1.IsActive));
                if (conflicted.Any())
                    throw new ParsingException("There are " + conflicted.Count() + " groups which are mutually exclusive.");
            }

            // Step #4 - Check for extraneous options.
            {
                IEnumerable<Option> extraneous =
                    _Options.Where(o => o.Value != null
                                        && o.GroupsAttr.Any()
                                        && _Groups.Where(g => g.Options.Contains(o)).All(g => !g.IsActive));
                if (extraneous.Any())
                    throw new ParsingException("Argument " + extraneous.First().Name + " requires additional arguments.");
            }


            // Step #5 - Finally, assign the values to the properties/fields on the result object.
            foreach (Option o in _Options.Where(o1 => o1.Value != null))
            {
                try
                {
                    if (o.Info is PropertyInfo pInfo)
                    {
                        if (! pInfo.PropertyType.IsAssignableFrom(o.Value.GetType()))
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


        /// <summary>Resets the stored value of all options.</summary>
        private void ResetAll()
        {
            foreach (Option o in _Options) o.Reset();
        }


    }
}
