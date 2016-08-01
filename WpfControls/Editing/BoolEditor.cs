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

namespace WpfControls.Editing
{
    
    public class BoolEditor : AbstractPropertyEditor
    {
        
        static BoolEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BoolEditor), new FrameworkPropertyMetadata(typeof(BoolEditor)));
        }

        public override bool IsValid
        {
            get
            {
                object data = Input;
                if (data is bool) return true;
                if (data is string)
                {
                    bool b;
                    return bool.TryParse((string)data, out b);
                }
                return false;
            }
        }

        public override object GetValue()
        {
            object data = Input;
            if (data is bool) return (bool)data;
            if (data is string)
            {
                bool b;
                if (bool.TryParse((string)data, out b)) return b;
            }
            return false;
        }
    }
}
