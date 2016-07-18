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
using System.Windows.Shapes;

namespace WpfControls.Editing
{
    /// <summary>
    /// Interaction logic for EditWindow.xaml
    /// </summary>
    public partial class EditWindow : Window
    {
        public EditWindow(object editedObject, object editingContext)
        {
            InitializeComponent();

            this.EditContext = editingContext;
            this.DataContext = editedObject;
            
            
        }

        public static DependencyProperty EditContextProperty = Editor.EditContextProperty.AddOwner(typeof(EditWindow));
        public object EditContext
        {
            get
            {
                return GetValue(EditContextProperty);
            }
            set
            {
                SetValue(EditContextProperty, value);
            }
        }

        private void Reset_Button_Click(object sender, RoutedEventArgs e)
        {
            this.editor.Reset();
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            this.editor.Reset();
            this.Result = this.editor.Result;
            this.Close();
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            this.editor.Update();
            this.Result = this.editor.Result;
            this.Close();
        }

        public MessageBoxResult Result { get; private set; } = MessageBoxResult.Cancel;
    }
}
