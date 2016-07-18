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
    /// <summary>
    /// Interaction logic for DoubleEditor.xaml
    /// </summary>
    public partial class DoubleEditor : UserControl, IPropertyEditor
    {
        public DoubleEditor()
        {
            DataContext = this;
            InitializeComponent();
            txtbxValue.TextChanged += TxtbxValue_TextChanged;
        }


        /// <summary>
        /// This is not even used.
        /// </summary>
        object IPropertyEditor.EditContext { get; set; }

        /// <summary>
        /// Returns true if the contents of the text box are valid as a double.
        /// </summary>
        public bool IsValid
        {
            get
            {
                double result;
                return double.TryParse(txtbxValue.Text, out result);
            }
        }

        public string Label { get; set; }

    
        public object Value
        {
            get
            {
                double result;
                if (!double.TryParse(txtbxValue.Text, out result)) return double.NaN;
                return result;
            }
            set
            {
                txtbxValue.Text = value.ToString();
            }
        }

        public bool IsReadOnly { get; set; }

        private void TxtbxValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            EventHandler handler = ValueChanged;
            if (handler != null) handler(this, new EventArgs());
        }

        public event EventHandler ValueChanged;


    }
}
