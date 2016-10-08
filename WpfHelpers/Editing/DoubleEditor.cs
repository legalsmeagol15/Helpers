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
    
    public class DoubleEditor : AbstractPropertyEditor
    {
        static DoubleEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DoubleEditor), new FrameworkPropertyMetadata(typeof(DoubleEditor)));
        }

        public override bool IsValid
        {
            get
            {
                object data = Input;
                if (data is double) return true;
                if (data is string)
                {
                    double b;
                    return double.TryParse((string)data, out b);
                }
                return false;
            }
        }

        public override object GetValue()
        {
            object data = Input;
            if (data is double) return (double)data;
            if (data is string)
            {
                double b;
                if (double.TryParse((string)data, out b)) return b;
            }
            return false;
        }


    }
}
