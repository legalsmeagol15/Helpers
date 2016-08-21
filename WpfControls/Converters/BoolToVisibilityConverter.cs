using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace WpfHelpers.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
                return (bool)value ? Visibility.Visible : Visibility.Hidden;
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility)
            {
                Visibility viz = (Visibility)value;
                switch (viz)
                {
                    case Visibility.Hidden: return false;
                    case Visibility.Collapsed: return false;
                    case Visibility.Visible: return true;
                }
            }
            throw new NotImplementedException();
        }
    }
}
