using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WpfControls.Editing
{
    /// <summary>
    /// Interaction logic for EditWindow.xaml
    /// </summary>
    public partial class EditWindow : Window
    {

        public static EditWindow ForEditableObject(object editedObject, object editingParamater, Window owner = null)
        {
            return new EditWindow(editedObject, editingParamater, owner);
        }
        protected EditWindow(object editedObject, object editingParamater, Window owner = null)
        {
            InitializeComponent();
            this.Owner = owner;

            this.Parameter = editingParamater;
            this.DataContext = editedObject;
        }

        public static EditWindow ForProperty(IPropertyEditor editor, object input, object editingParameter, Window owner= null)
        {
            return new EditWindow(editor, input, editingParameter, owner);
        }
        protected EditWindow(IPropertyEditor editor, object input, object editingParameter, Window owner = null)
        {
            
            InitializeComponent();
            this.Owner = owner;

            this.Parameter = editingParameter;
            EditingInput ei = new EditingInput();
            ei.Input = input;
            ei.Editor = editor;
            this.DataContext = ei;
        }

        internal struct EditingInput
        {
            public object Input;
            public IPropertyEditor Editor;
            //public string Label;
            
        }


        public static DependencyProperty ParameterProperty = AutoEditor.ParameterProperty.AddOwner(typeof(EditWindow));
        public object Parameter
        {
            get
            {
                return GetValue(ParameterProperty);
            }
            set
            {
                SetValue(ParameterProperty, value);
            }
        }


        public static DependencyProperty AllowResetProperty = DependencyProperty.Register("AllowReset", typeof(bool), typeof(EditWindow), new PropertyMetadata(true));
        public bool AllowReset
        {
            get
            {
                return (bool)GetValue(AllowResetProperty);
            }
            set
            {
                SetValue(AllowResetProperty, value);
            }
        }

        public static DependencyProperty AllowCancelProperty = DependencyProperty.Register("AllowCancel", typeof(bool), typeof(EditWindow), new PropertyMetadata(true));
        public bool AllowCancel
        {
            get
            {
                return (bool)GetValue(AllowCancelProperty);
            }
            set
            {
                SetValue(AllowCancelProperty, value);
            }
        }

        private void Reset_Button_Click(object sender, RoutedEventArgs e)
        {
            this.editor.Reset();
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            this.editor.Cancel();
            this.Result = this.editor.Result;
            this.Close();
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            this.editor.Update();
            this.Result = this.editor.Result;
            this.Close();
        }

        public object Result { get; private set; } = null;


        
    }
}
