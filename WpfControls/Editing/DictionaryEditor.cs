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
    
    public class DictionaryEditor : AbstractPropertyEditor
    {
        static DictionaryEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DictionaryEditor), new FrameworkPropertyMetadata(typeof(DictionaryEditor)));
        }

        public override bool IsValid
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override object GetValue()
        {
            throw new NotImplementedException();
        }
    }
}
