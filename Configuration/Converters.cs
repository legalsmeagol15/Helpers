using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Helpers.Converters
{
    internal sealed class ListStringsConverter :Helpers.ConfigurationConverter
    {
        private static char[] DELIMITERS = ";!@#$%^*_+=-/?".ToCharArray();
        public override object ConvertFrom(string str, params KeyValuePair<string, string>[] xpaths)
        {
            // The very first character is the delimiter char.
            if (str.Length == 0) return new List<string>();
            return str.Split(new char[] { str[0] }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }
        public override string ConvertTo(object original, object host)
        {
            HashSet<char> delimiters = new HashSet<char>(DELIMITERS);
            
            List<string> list = (List<string>)original;

            foreach (string item in list)
            {
                delimiters.RemoveWhere(c => item.Contains(c));
                if (delimiters.Count == 0) 
                    throw new InvalidOperationException("No valid delimiters can be chosen from among " + new string(DELIMITERS));
            }

            char delimiter = delimiters.First();

            StringBuilder sb = new StringBuilder();
            foreach (string item in list)
            {
                sb.Append(delimiter);
                sb.Append(item);
            }
            return sb.ToString();
        }
    }

    internal sealed class DependencyVariableConverter : Helpers.ConfigurationConverter
    {
        public override object ConvertFrom(string str, params KeyValuePair<string, string>[] xpaths)
        {
            // TODO:  how to provide context to the Parse.FromString method?
            Dependency.IEvaluateable iev = Dependency.Parse.FromString(str);
            return new Dependency.Variables.Variable();
            throw new NotImplementedException();
        }
    }
}
