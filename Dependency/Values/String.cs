using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dependency
{
    [Serializable]
    public struct String : ILiteral<string>, ITypeGuarantee
    {
        public static readonly String Empty = new String("");
        internal const string PARSE_PATTERN = "\"[^\"]*\"";  // Any string bracketed by two "s and containing no " in between.

        internal readonly string Value;

        public TypeFlags TypeGuarantee => TypeFlags.String;

        public String(string str) { this.Value = str; }

        public static implicit operator String(string str) => new String(str);
        public static implicit operator string(String s) => s.Value;

        public static implicit operator String(Number n) => new String(n.ToString());
        //public static implicit operator IExpression(String s) => Decimal.TryParse(s.Value, out decimal m) ? new Number(m) : new Error("Cannot convert string "+s.Value + " to number.");

        public static bool operator ==(String a, String b) => a.Value == b.Value;
        public static bool operator !=(String a, String b) => a.Value != b.Value;
        public static String operator +(String a, String b) => new String(a.Value + b.Value);

        public override bool Equals(object obj)
        {
            if (obj is String n) return this.Value == n.Value;
            if (obj is string s) return this.Value == s;
            return false;
        }

        public override int GetHashCode() => Value.GetHashCode();

        public static bool TryUnquote(string str, out string result)
        {
            if (str.Length < 2) { result = null; return false; }
            if (str[0] == '"' && str[str.Length - 1] == '"')
            {
                result = str.Substring(1, str.Length - 2);
                return true;
            }
            result = null;
            return false;
        }

        public override string ToString() => this.Value;

        internal static string QueensJoin<T>(IEnumerable<T> items, string lastJoiner = "or") => QueensJoin(items.ToArray(), lastJoiner);
        internal static string QueensJoin<T>(T[] items, string lastJoiner = "or")
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(items[0].ToString());
            if (items.Length == 1) return sb.ToString();
            for (int i = 1; i < items.Length - 1; i++)
            {
                sb.Append(", ");
                sb.Append(items[i].ToString());
            }
            if (items.Length > 2) sb.Append(","); // The Oxford comma
            sb.Append(" " + lastJoiner + " " + items[items.Length - 1].ToString());
            return sb.ToString();
        }

        string ILiteral<string>.CLRValue => Value;
        
        IEvaluateable IEvaluateable.Value => this;
        
    }
}
