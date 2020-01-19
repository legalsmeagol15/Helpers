using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency.Values
{
    internal class Converter<T> : IConverter<T>
    {
        private static IConverter<T> _Default;
        static Converter()
        {
            if (typeof(int).Equals(typeof(T)))
                _Default = (IConverter<T>)(IConverter<int>)(new IntConverter());
            else if (typeof(double).Equals(typeof(T)))
                _Default = (IConverter<T>)(IConverter<double>)(new DoubleConverter());
            else if (typeof(decimal).Equals(typeof(T)))
                _Default = (IConverter<T>)(IConverter<decimal>)(new DecimalConverter());
            else if (typeof(string).Equals(typeof(T)))
                _Default = (IConverter<T>)(IConverter<string>)(new StringConverter());
            else if (typeof(IEvaluateable).IsAssignableFrom(typeof(T)))
                _Default = (IConverter<T>)(IConverter<IEvaluateable>)(new TransparentConverter());
            else
                _Default = new Converter<T>();
        }

        public static IConverter<T> Default => _Default;

        bool IConverter<T>.CanConvert(IEvaluateable ie)
        {
            throw new InvalidCastException("Cannot convert " + ie.GetType().Name + " to " + typeof(T).Name);
        }

        IEvaluateable IConverter<T>.ConvertFrom(T item)
        {
            throw new InvalidCastException("Cannot convert " + item.GetType().Name + " to " + nameof(IEvaluateable));
        }

        T IConverter<T>.ConvertTo(IEvaluateable ie)
        {
            throw new InvalidCastException("Cannot convert " + ie.GetType().Name + " to " + typeof(T).Name);
        }

        bool IConverter<T>.TryConvert(IEvaluateable ie, out T target)
        {
            target = default(T);
            return false;
        }
    }
    

    internal sealed class DecimalConverter : IConverter<decimal>, ITypeGuarantee
    {
        TypeFlags ITypeGuarantee.TypeGuarantee => TypeFlags.RealAny;

        bool IConverter<decimal>.CanConvert(IEvaluateable ie) => ie is Number n;

        IEvaluateable IConverter<decimal>.ConvertFrom(decimal item) => new Number(item);

        decimal IConverter<decimal>.ConvertTo(IEvaluateable item)
        {
            return ((Number)item).ToDecimal();
        }

        bool IConverter<decimal>.TryConvert(IEvaluateable ie, out decimal target)
        {
            if (ie is Number n) { target = n.ToDecimal(); return true; }
            else { target = 0m; return false; }
        }
        
    }

    internal sealed class DoubleConverter : IConverter<double>, ITypeGuarantee
    {
        TypeFlags ITypeGuarantee.TypeGuarantee => TypeFlags.RealAny;

        bool IConverter<double>.CanConvert(IEvaluateable ie) => ie is Number n;

        IEvaluateable IConverter<double>.ConvertFrom(double item) => new Number(item);

        bool IConverter<double>.TryConvert(IEvaluateable ie, out double target)
        {
            if (ie is Number n) { target = n.ToDouble(); return true; }
            else { target = double.NaN; return false; }
        }

        double IConverter<double>.ConvertTo(IEvaluateable item)
        {
            return ((Number)item).ToDouble();
        }
    }

    internal sealed class IntConverter : IConverter<int>, ITypeGuarantee
    {
        TypeFlags ITypeGuarantee.TypeGuarantee => TypeFlags.Integer;

        bool IConverter<int>.CanConvert(IEvaluateable ie) => ie is Number n && n.IsInteger;

        IEvaluateable IConverter<int>.ConvertFrom(int item) => new Number(item);

        bool IConverter<int>.TryConvert(IEvaluateable ie, out int target)
        {
            if (ie is Number n && n.IsInteger) { target = n.ToInt(); return true; }
            else { target = 0; return false; }
        }

        int IConverter<int>.ConvertTo(IEvaluateable item)
        {
            return ((Number)item).ToInt();
        }
    }

    internal sealed class StringConverter : IConverter<string>, ITypeGuarantee
    {
        TypeFlags ITypeGuarantee.TypeGuarantee => TypeFlags.String;

        bool IConverter<string>.CanConvert(IEvaluateable ie) => true;

        IEvaluateable IConverter<string>.ConvertFrom(string item) => new Dependency.String(item);

        bool IConverter<string>.TryConvert(IEvaluateable ie, out string target) { target = ie.ToString(); return true; }
        string IConverter<string>.ConvertTo(IEvaluateable item)
        {
            return ((Number)item).ToString();
        }
    }

    internal sealed class TransparentConverter : IConverter<IEvaluateable>, ITypeGuarantee
    {
        TypeFlags ITypeGuarantee.TypeGuarantee => TypeFlags.Any;

        bool IConverter<IEvaluateable>.CanConvert(IEvaluateable ie) => true;

        IEvaluateable IConverter<IEvaluateable>.ConvertFrom(IEvaluateable item) => item;

        bool IConverter<IEvaluateable>.TryConvert(IEvaluateable ie, out IEvaluateable target) { target = ie; return true; }

        IEvaluateable IConverter<IEvaluateable>.ConvertTo(IEvaluateable item) => item;
    }
    


}
