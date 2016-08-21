using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace WpfHelpers.Converters
{
    /// <summary>
    /// Converts a color to a new SolidColorBrush, or returns the SolidColorBrush's Color, depending on what is input.
    /// </summary>
    public class ColorBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            if (value is Color) return new SolidColorBrush((Color)value);
            if (value is SolidColorBrush) return ((SolidColorBrush)value).Color;
            throw new InvalidOperationException("ColorBrushConverter unsupported type " + value.GetType().Name + ".");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            if (value is Color) return new SolidColorBrush((Color)value);
            if (value is SolidColorBrush) return ((SolidColorBrush)value).Color;
            throw new InvalidOperationException("ColorBrushConverter unsupported type " + value.GetType().Name + ".");
        }
    }
}
