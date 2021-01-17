using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helpers
{
    public abstract class ConfigurationConverter : TypeConverter
    {
        public sealed override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => sourceType == typeof(string) || sourceType == typeof(String);


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
        
    }
}
