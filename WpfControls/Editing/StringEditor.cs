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
    
    /// <summary>
    /// A control for use by automatically-generated editors to edit a 'string'-Type property.
    /// </summary>
    public class StringEditor : AbstractPropertyEditor
    {
        static StringEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(StringEditor), new FrameworkPropertyMetadata(typeof(StringEditor)));
        }

        public override bool IsValid
        {
            get
            {
                if (Input == null) return true;
                return Input is string;
            }
        }

        public override object GetValue()
        {
            if (Input == null) return "";
            return Input.ToString();
        }
    }
}
