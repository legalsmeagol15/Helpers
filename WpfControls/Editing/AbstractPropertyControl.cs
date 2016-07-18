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
    
    public abstract class AbstractPropertyControl : Control, IPropertyEditor
    {

        public static DependencyProperty EditContextProperty = DependencyProperty.Register("EditContext", typeof(object), typeof(AbstractPropertyControl), new PropertyMetadata(null, On_EditContext_Changed));
        private static void On_EditContext_Changed(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            AbstractPropertyControl apc = obj as AbstractPropertyControl;
            if (apc == null) return;
            apc.OnEditContextChanged(e);
        }
        /// <summary>
        /// Override to add behavior when the control's editing context changes.  Base declaration does nothing.
        /// </summary>        
        protected virtual void OnEditContextChanged(DependencyPropertyChangedEventArgs e)
        {

        }
        /// <summary>
        /// The context information for this control.
        /// </summary>
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



        public static DependencyProperty IsReadOnlyProperty = DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(AbstractPropertyControl), new PropertyMetadata(true, On_IsReadOnly_Changed));
        private static void On_IsReadOnly_Changed(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            AbstractPropertyControl apc = obj as AbstractPropertyControl;
            if (apc == null) return;
            apc.OnIsReadOnlyChanged(e);
        }
        protected virtual void OnIsReadOnlyChanged(DependencyPropertyChangedEventArgs e)
        {

        }
        /// <summary>
        /// Whether or not this control is intended to be read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return (bool)GetValue(IsReadOnlyProperty);
            }
            set
            {
                SetValue(IsReadOnlyProperty, value);
            }
        }


        /// <summary>
        /// Indicates whether the current value of the property control is valid or not.
        /// </summary>
        public abstract bool IsValid { get; }



        public static DependencyProperty LabelProperty = DependencyProperty.Register("Label", typeof(string), typeof(AbstractPropertyControl), new PropertyMetadata("", On_Label_Changed));
        private static void On_Label_Changed(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            AbstractPropertyControl apc = obj as AbstractPropertyControl;
            if (apc == null) return;
            apc.OnLabelChanged(e);
        }
        /// <summary>
        /// Override to add behavior when a label changes.  Base declaration does nothing.
        /// </summary>        
        protected virtual void OnLabelChanged(DependencyPropertyChangedEventArgs e)
        {

        }
        /// <summary>
        /// The property label to display in the control.
        /// </summary>
        public string Label
        {
            get
            {
                return (string)GetValue(LabelProperty);
            }
            set
            {
                SetValue(LabelProperty, value);
            }
        }



        public static DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(object), typeof(AbstractPropertyControl), new PropertyMetadata(null, On_Value_Changed));
        private static void On_Value_Changed(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            AbstractPropertyControl apc = obj as AbstractPropertyControl;
            if (apc == null) return;
            apc.OnValueChanged(e);
        }
        /// <summary>
        /// Override to modify behavior when the control's value changes.  Base declaration raises the ValueChanged event.
        /// </summary>
        
        protected virtual void OnValueChanged(DependencyPropertyChangedEventArgs e)
        {
            RaiseValueChangedEvent();            
        }        
        /// <summary>
        /// The current value reflected in this control.
        /// </summary>
        public object Value
        {
            get
            {
                return GetValue(ValueProperty);
            }

            set
            {
                SetValue(ValueProperty, value);
            }
        }
        protected void RaiseValueChangedEvent()
        {
            EventHandler handler = ValueChanged;
            if (handler != null) handler(this, new EventArgs());

        }
        /// <summary>
        /// Raised when the value of the property edited by this control changes.
        /// </summary>
        public event EventHandler ValueChanged;



    }
}
