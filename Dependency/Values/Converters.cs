using Mathematics.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency.Values
{
   

    /// <summary>
    /// The standard converter simply converts an object to a <seealso cref="Dependency.String"/>
    /// by invoking the object's <seealso cref="Object.ToString"/> method.  Casts from the 
    /// dependency type back to <typeparamref name="T"/> will fail.
    /// </summary>
    /// <remarks>
    /// This type is sealed.  If you need to specify a type converter, implement 
    /// <seealso cref="IConverter{T}"/>.
    /// </remarks>
    /// <typeparam name="T">The CLR type to and from which type will be converted.</typeparam>
    public sealed class Converter<T> : IConverter<T>
    {
        private static readonly IConverter<T> _Default;
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
            else if (typeof(bool).IsAssignableFrom(typeof(T)))
                _Default = (IConverter<T>)(IConverter<bool>)(new BoolConverter());
            else if (typeof(VectorN).IsAssignableFrom(typeof(T)))
                _Default = (IConverter<T>)(IConverter<VectorN>)(new VectorNConverter());
            else if (typeof(IPoint<T>).IsAssignableFrom(typeof(T)))
                _Default = (IConverter<T>)(IConverter<IPoint<T>>)(new VectorConverter<T>());
            else if (typeof(Number).IsAssignableFrom(typeof(T)))
                _Default = (IConverter<T>)(IConverter<Number>)(new TransparentValue<Number>());
            else if (typeof(IEvaluateable).IsAssignableFrom(typeof(T)))
                _Default = new Transparent();
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

        bool IConverter<T>.TryConvertTo(IEvaluateable ie, out T target)
        {
            target = default(T);
            return false;
        }

        internal sealed class TransparentValue<E> : IConverter<E>, ITypeGuarantee where E : IEvaluateable, new()
        {
            private static readonly TypeFlags _TypeGuarantee;
            static TransparentValue()
            {
                if (typeof(ITypeGuarantee).IsAssignableFrom(typeof(E)))
                {
                    E obj = (E)Activator.CreateInstance(typeof(E));
                    _TypeGuarantee = ((ITypeGuarantee)obj).TypeGuarantee;
                }
                else
                    _TypeGuarantee = TypeFlags.Any;
            }
            public TypeFlags TypeGuarantee => _TypeGuarantee;
            
            public bool CanConvert(IEvaluateable ie) => ie is E;

            public IEvaluateable ConvertFrom(E item) => item;

            public E ConvertTo(IEvaluateable item) => (E)item;

            public bool TryConvertTo(IEvaluateable ie, out E target)
            {
                if (ie is E e) { target = e; return true; }
                target = default;
                return false;
            }
        }

        private sealed class Transparent : IConverter<T>, ITypeGuarantee
        {
            public TypeFlags TypeGuarantee => TypeFlags.Any;

            public bool CanConvert(IEvaluateable ie) => true;

            public IEvaluateable ConvertFrom(T item) => (IEvaluateable)item;

            public T ConvertTo(IEvaluateable item) => (T)item;

            public bool TryConvertTo(IEvaluateable ie, out T target) { target = (T)ie; return true; }
        }
    }

    internal sealed class BoolConverter : IConverter<bool>, ITypeGuarantee
    {
        TypeFlags ITypeGuarantee.TypeGuarantee => TypeFlags.Boolean;

        bool IConverter<bool>.CanConvert(IEvaluateable ie) => ie is Dependency.Boolean;

        IEvaluateable IConverter<bool>.ConvertFrom(bool item) => item ? Dependency.Boolean.True : Dependency.Boolean.False;

        bool IConverter<bool>.ConvertTo(IEvaluateable item)
        {
            if (item is Dependency.Boolean b) return b;
            return false;
        }

        bool IConverter<bool>.TryConvertTo(IEvaluateable ie, out bool target)
        {
            if (ie is Dependency.Boolean b) { target = b; return true; }
            target = false;
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

        bool IConverter<decimal>.TryConvertTo(IEvaluateable ie, out decimal target)
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

        bool IConverter<double>.TryConvertTo(IEvaluateable ie, out double target)
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

        bool IConverter<int>.TryConvertTo(IEvaluateable ie, out int target)
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

        bool IConverter<string>.TryConvertTo(IEvaluateable ie, out string target) { target = ie.ToString(); return true; }
        string IConverter<string>.ConvertTo(IEvaluateable item)
        {
            return ((Number)item).ToString();
        }
    }

    public sealed class VectorNConverter : IConverter<VectorN>
    {
        bool IConverter<VectorN>.CanConvert(IEvaluateable ie)
        {
            return ie is Vector v && v.Inputs.Count == 2 && v[0] is Number && v[1] is Number;
        }

        IEvaluateable IConverter<VectorN>.ConvertFrom(VectorN item)
        {
            return new Vector((IEvaluateable)item.X, (IEvaluateable)item.Y);
        }

        VectorN IConverter<VectorN>.ConvertTo(IEvaluateable item)
        {
            Vector v = (Vector)item;
            Number x = (Number)v[0];
            Number y = (Number)v[1];
            return new VectorN(x, y);
        }

        bool IConverter<VectorN>.TryConvertTo(IEvaluateable ie, out VectorN target)
        {
            if (ie is Vector v && v.Inputs.Count == 2 && v[0] is Number x && v[1] is Number y)
            {
                target = new VectorN(x, y);
                return true;
            }
            target = default;
            return false;
        }
    }

    public sealed class VectorConverter<T> : IConverter<IPoint<T>>
    {
        bool IConverter<IPoint<T>>.CanConvert(IEvaluateable ie)
        {
            throw new NotImplementedException();
        }

        IEvaluateable IConverter<IPoint<T>>.ConvertFrom(IPoint<T> item)
        {
            throw new NotImplementedException();
        }

        IPoint<T> IConverter<IPoint<T>>.ConvertTo(IEvaluateable item)
        {
            throw new NotImplementedException();
        }

        bool IConverter<IPoint<T>>.TryConvertTo(IEvaluateable ie, out IPoint<T> target)
        {
            throw new NotImplementedException();
        }
    }

    
    internal sealed class TransparentConverter : IConverter<IEvaluateable>, ITypeGuarantee
    {
        TypeFlags ITypeGuarantee.TypeGuarantee => TypeFlags.Any;

        bool IConverter<IEvaluateable>.CanConvert(IEvaluateable ie) => true;

        IEvaluateable IConverter<IEvaluateable>.ConvertFrom(IEvaluateable item) => item;

        bool IConverter<IEvaluateable>.TryConvertTo(IEvaluateable ie, out IEvaluateable target) { target = ie; return true; }

        IEvaluateable IConverter<IEvaluateable>.ConvertTo(IEvaluateable item) => item;
    }



    public static class Converter
    {
        /// <summary>
        /// Returns the defined converter for the given object.
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="obj">The object whose type will specify the converter type.</param>
        /// <param name="requireDefinition">Optional.  If no converter exists for the object's 
        /// specific type, setting this value to true will cause nulls to return instead of the 
        /// pass-through converter (<seealso cref="Converter{U}"/>).</param>
        internal static IConverter<U> GetDefaultFor<U>(U obj, bool requireDefinition = true)
        {
            // This line is to make the compiler believe the argument is used.
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            var result = Converter<U>.Default;
            if (result is Converter<U> && requireDefinition) return null;
            return result;
        }
    }


}
