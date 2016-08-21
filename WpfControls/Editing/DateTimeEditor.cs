using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfHelpers.Editing
{
    
    public class DateTimeEditor : AbstractPropertyEditor
    {
        static DateTimeEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DateTimeEditor), new FrameworkPropertyMetadata(typeof(DateTimeEditor)));
        }
        public override bool IsValid
        {
            get
            {
                object data = Input;
                if (data is DateTime) return true;
                if (data is string)
                {
                    DateTime b;
                    return DateTime.TryParse((string)data, out b);
                }
                return false;
            }
        }

        public override object GetValue()
        {
            object data = Input;
            if (data is DateTime) return (DateTime)data;
            if (data is string)
            {
                DateTime b;
                if (DateTime.TryParse((string)data, out b)) return b;
            }
            return false;
        }
    }
}
