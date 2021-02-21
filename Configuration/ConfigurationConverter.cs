using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers
{
    public abstract class ConfigurationConverter : TypeConverter
    {

        public sealed override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => sourceType == typeof(string) || sourceType == typeof(String);
        public sealed override bool CanConvertTo(ITypeDescriptorContext context, Type destType)
            => destType == typeof(string) || destType == typeof(String);
        public sealed override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            => ConvertTo(value, null);
        public sealed override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            => ConvertFrom(value.ToString());
        public sealed override bool IsValid(ITypeDescriptorContext context, object value)
            => CanConvertFrom(context, value.GetType());
       

        /// <summary>
        /// Converts from a string to the type of this converter.
        /// </summary>
        /// <param name="str">The string to be converted from.</param>
        /// <param name="xpaths">The paths that will provide additional information from the xml 
        /// original to inform the conversion.</param>
        public abstract object ConvertFrom(string str, params KeyValuePair<string, string>[] xpaths);

        /// <summary>
        /// Converts the given object to a string.  The object's host is given to inform the conversion.
        /// </summary>
        /// <param name="original"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        public virtual string ConvertTo(object original, object host) => original.ToString();


        #region ConfigurationConverter's sealed overrides from TypeConverter

        // TypeConverter provides a lot of overrideable methods that I don't want to pass down to 
        // inheritors of ConfigurationConverter.

        public sealed override bool GetCreateInstanceSupported(ITypeDescriptorContext context)
           => false;
        public sealed override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
            => base.GetProperties(context, value, attributes);
        public sealed override bool GetPropertiesSupported(ITypeDescriptorContext context)
            => base.GetPropertiesSupported(context);
        public sealed override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            => base.GetStandardValues(context);
        public sealed override object CreateInstance(ITypeDescriptorContext context, System.Collections.IDictionary propertyValues)
            => base.CreateInstance(context, propertyValues);
        public sealed override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
            => base.GetStandardValuesExclusive(context);
        public sealed override bool GetStandardValuesSupported(ITypeDescriptorContext context)
            => base.GetStandardValuesSupported(context);

        #endregion

    }
}
