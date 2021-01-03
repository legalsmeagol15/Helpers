using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Helpers
{
    /// <summary>
    /// Allows for simple and centralized conversions from strings, either with dynamic types or 
    /// times known at compile time.
    /// </summary>
    public static class FromString
    {
        private static readonly Dictionary<Type, Func<string, ITuple>> _TryParsers;
        private static readonly Dictionary<Type, Func<string, object>> _Parsers;
        
        static FromString()
        {
            ITuple _Tupleize<T>(bool b, T val) => new Tuple<bool, T>(b, val);
            Type t = MethodBase.GetCurrentMethod().DeclaringType;
            if (t == typeof(string) || t == typeof(String))
            {
                _TryParsers[t] = s => _Tupleize(true, s);
                _Parsers[t] = s => s;
            }
            else if (t == typeof(bool) || t == typeof(Boolean))
            {
                _TryParsers[t] = s => bool.TryParse(s, out bool b) ? _Tupleize(true, b) : _Tupleize<bool>(false, default);
                _Parsers[t] = s => bool.Parse(s);
            }
            else if (t == typeof(int) || t == typeof(Int32))
            {
                _TryParsers[t] = s => int.TryParse(s, out int i) ? _Tupleize(true, i) : _Tupleize<int>(false, default);
                _Parsers[t] = s => int.Parse(s);
            }
            else if (t == typeof(float) || t == typeof(Single))
            {
                _TryParsers[t] = s => float.TryParse(s, out float f) ? _Tupleize(true, f) : _Tupleize<float>(false, default);
                _Parsers[t] = s => float.Parse(s);
            }
            else if (t == typeof(double) || t == typeof(Double))
            {
                _TryParsers[t] = s => double.TryParse(s, out double d) ? _Tupleize(true, d) : _Tupleize<double>(false, default);
                _Parsers[t] = s => double.Parse(s);
            }
            else if (t == typeof(byte) || t == typeof(Byte))
            {
                _TryParsers[t] = s => byte.TryParse(s, out byte b) ? _Tupleize(true, b) : _Tupleize<byte>(false, default);
                _Parsers[t] = s => byte.Parse(s);
            }
            else if (t == typeof(char) || t == typeof(Char))
            {
                _TryParsers[t] = s => char.TryParse(s, out char c) ? _Tupleize(true, c) : _Tupleize<char>(false, default);
                _Parsers[t] = s => char.Parse(s);
            }
            else if (t == typeof(byte[]) || t == typeof(Byte[]))
            {
                _TryParsers[t] = s => throw new NotImplementedException();
                _Parsers[t] = s => Convert.FromBase64String(s);
            }
            else
                throw new NotImplementedException();
        }

        public static bool TryParse(Type t, string s, out object result)
        {
            if (_TryParsers.TryGetValue(t, out var f))
            {
                ITuple tuple = f(s);
                if ((bool)tuple[0]) { result = tuple[1]; return true; }
            }
            result = default;
            return false;
        }
        public static bool TryParse<T>(string s, out T result)
        {
            if (_TryParsers.TryGetValue(typeof(T), out var f))
            {
                ITuple tuple = f(s);
                if ((bool)tuple[0]) { result = (T)tuple[1]; return true; }
            }
            result = default;
            return false;
        }

        public static object Parse(Type t, string s) => _Parsers.TryGetValue(t, out var f) ? f(s) : default;
        public static T Parse<T>(string s) => _Parsers.TryGetValue(typeof(T), out var f) ? (T)f(s) : default;
    }
}
