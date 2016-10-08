using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WpfHelpers.Editing
{
    public abstract class AbstractPropertyEditor : Control, IPropertyEditor
    {
      
        /// <summary>
        /// Creates a new editor and sets its DataContext to itself.
        /// </summary>
        public AbstractPropertyEditor()
        {
            this.DataContext = this;
        }


        /// <summary>
        /// Raised when the value of the property edited by this control changes.
        /// </summary>
        public event EventHandler ValueChanged;


        private static DependencyPropertyKey GroupNamePropertyKey = 
            DependencyProperty.RegisterReadOnly("GroupName", typeof(string), typeof(AbstractPropertyEditor), new PropertyMetadata(""));
        /// <summary>
        /// The declaration of the read-only GroupName dependency property.
        /// </summary>
        public static DependencyProperty GroupNameProperty = GroupNamePropertyKey.DependencyProperty;
        /// <summary>
        /// The name of an editor group, used for auto-generating a sorted and grouped AutoEditor.
        /// </summary>
        public string GroupName
        {
            get { return (string)GetValue(GroupNameProperty); }
            set { SetValue(GroupNamePropertyKey, value); }
        }


        private static DependencyPropertyKey PriorityPropertyKey = 
            DependencyProperty.RegisterReadOnly("Priority", typeof(int), typeof(AbstractPropertyEditor), new PropertyMetadata(0));
        /// <summary>
        /// The declaration of the read-only Priority dependency property.
        /// </summary>
        public static DependencyProperty PriorityProperty = PriorityPropertyKey.DependencyProperty;
        /// <summary>
        /// The priority within an editor group, used for auto-generating a sorted and grouped AutoEditor.  Low numbers are listed first.
        /// </summary>
        public int Priority
        {
            get { return (int)GetValue(PriorityProperty); }
            set { SetValue(PriorityPropertyKey, value); }
        }




        public static DependencyProperty LabelProperty = DependencyProperty.Register("Label", typeof(string), typeof(AbstractPropertyEditor));
        /// <summary>
        /// The label associated with this editor.  By default, an AutoEditor will generate a GUI showing a label, so it is not necessarily to present in 
        /// the visual representating of an editor, but it can be.
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

        public static DependencyProperty ParameterProperty =
            DependencyProperty.Register("Parameter", typeof(object), typeof(AbstractPropertyEditor), new PropertyMetadata(null, On_Parameter_Changed));
        /// <summary>
        /// An editing parameter distinct from the editor's input.  This property may be used to pass information like minima / maxima, conversions, etc.
        /// </summary>
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
        private static void On_Parameter_Changed(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            AbstractPropertyEditor ape = obj as AbstractPropertyEditor;
            if (ape == null) return;
            ape.OnParameterChanged(e);
        }
        /// <summary>
        /// Called when the editing paramater changes.  Base declaration  does nothing.
        /// </summary>        
        protected virtual void OnParameterChanged(DependencyPropertyChangedEventArgs e)
        {

        }

        /// <summary>
        /// The declaration of the Input property.
        /// </summary>
        public static DependencyProperty InputProperty = DependencyProperty.Register("Input", typeof(object), typeof(AbstractPropertyEditor), new PropertyMetadata(null, On_Input_Changed, Coerce_Input));
        private static object Coerce_Input(DependencyObject obj, object originalValue)
        {
            AbstractPropertyEditor ape = obj as AbstractPropertyEditor;
            if (ape == null) return originalValue;
            return ape.CoerceInput(originalValue);
        }
        /// <summary>
        /// Override to provide logic for coercing the value of input whenever it changes.  Base declaration returns the original value.
        /// </summary>
        protected virtual object CoerceInput(object originalValue)
        {
            return originalValue;
        }
        /// <summary>
        /// The input value for the startup of this editor.  The input can also be treated as the current operating value of the editor by some editors.
        /// </summary>
        public object Input
        {
            get
            {
                return GetValue(InputProperty);
            }
            set
            {
                SetValue(InputProperty, value);
            }
        }
        private static void On_Input_Changed(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            AbstractPropertyEditor ape = obj as AbstractPropertyEditor;
            if (ape == null) return;
            ape.OnInputChanged(e);
        }
        /// <summary>
        /// Override to provide logic for when the editor's Input changes.  Base declaration does nothing.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnInputChanged(DependencyPropertyChangedEventArgs e)
        {
            
        }

        bool IPropertyEditor.Changed { get { return this.Changed; } }

        /// <summary>
        /// A signal indicating whether meaningful changes have occured to the Input for this editor.
        /// </summary>
        public bool Changed { get; protected set; }

        /// <summary>
        /// Override to provide logic for how the finalized value of this editor will be presented.
        /// </summary>        
        public abstract object GetValue();
        object IPropertyEditor.GetValue() { return this.GetValue(); }

        /// <summary>
        /// Override to provide logic determining whether the input and value of the editor are in a valid state.
        /// </summary>
        public abstract bool IsValid { get; }


        /// <summary>
        /// The IsReadOnly dependency property declaration.
        /// </summary>
        public static DependencyProperty IsReadOnlyProperty = DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(AbstractPropertyEditor));
        /// <summary>
        /// A value indicating whether this editor is intended to be presented with a read-only GUI.
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
        /// Raises the ValueChanged event.
        /// </summary>
        protected void RaiseValueChanged()
        {
            EventHandler handler = ValueChanged;
            if (handler != null) handler(this, new EventArgs());
        }

    }

}
