using System;
using System.Collections;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfControls.Editing
{

    public sealed class AutoEditor : ItemsControl
    {
        static AutoEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AutoEditor), new FrameworkPropertyMetadata(typeof(AutoEditor)));
            
        }

        public AutoEditor()
        {
            //This constructor required for use by the WPF Designer.
            this.DataContextChanged += On_DataContext_Changed;
        }



        private IDictionary<IPropertyEditor, ControlInfo> _Contexts = new Dictionary<IPropertyEditor, ControlInfo>();


       

        public static DependencyProperty ParameterProperty =
            DependencyProperty.Register("Parameter", typeof(object), typeof(AutoEditor), new PropertyMetadata(null, On_Parameter_Changed));
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
            AutoEditor ae = obj as AutoEditor;
            if (ae == null) return;
            ae.Rebuild(ae.DataContext);
        }




        /// <summary>
        /// Cancels all pending changes and re-constructs the editor according to the given data object.
        /// </summary>
        private void Rebuild(object data)
        {
            if (data == null) return;

            //Step #1 - if there was already a context table, clear it out.
            _Contexts.Clear();

            //Step #2 - get the new contexts.
            if (data is EditWindow.EditingInput)
            {
                EditWindow.EditingInput input = (EditWindow.EditingInput)data;
                _Contexts = GetEditors(input, Parameter);
            }
            else
                _Contexts = GetEditors(data, Parameter);

            //Step #3 - update the UI.        
            this.ItemsSource = _Contexts.Keys;

        }

        /// <summary>
        /// A factor-style constructor that creates a set containined a single IPropertyEditor control suited for editing the input of the 
        /// given data.  
        /// </summary> 
        private static IDictionary<IPropertyEditor, ControlInfo> GetEditors(EditWindow.EditingInput data, object parameter)
        {
            //Step #1 - figure out what is the appropriate IPropertyEditor.
            Dictionary<IPropertyEditor, ControlInfo> result = new Dictionary<IPropertyEditor, ControlInfo>();
            IPropertyEditor ipe = data.Editor;
            object originalInput = data.Input;

            //If the editor is null, meaning there was no editor with the data, try for a default editor based on the input's type.
            if (ipe == null) ipe = GetDefaultEditor(data.GetType());

            //If the editor is still null, then it must be just a dud with no predefined editor.
            if (ipe == null)
            {
                StringEditor lbl = new StringEditor();
                lbl.Input = "No valid editor type established for data type " + data.GetType() + ".";
                lbl.IsReadOnly = true;
                //lbl.Label = data.Label;   //No need to label - just a single editor.
                result.Add(lbl, new ControlInfo(null, originalInput, null));
                return result;          
            }

            //Step #2 - Update the new control's links, and add the created control to the dictionary.              
            ipe.Parameter = parameter;
            ipe.Input = originalInput;
            ipe.IsReadOnly = false;     //Of course it's not readonly - just a single editor.
            //ipe.Label = data.Label;   //No need to label - just a single editor.
            result.Add(ipe, new ControlInfo(null, originalInput, null));

            //No need to specially handle if the editor is an AbstractPropertyEditor, because special handling is only for grouping and priority sorting.
            return result;
        }

        /// <summary>
        /// A factory-style multi-constructor that creates a set of IPropertyEditor controls (including AbstractPropertyEditor objects), linked in a 
        /// dictionary with the controls' original values.  The editors returned from this method will all be fully linked 
        /// and ready to display.
        /// </summary>
        /// <param name="targetedObject">The object whose Editable properties are to be extracted into a set of controls.</param>
        /// <param name="parameter">Optional.  The editing parameter for controls generated.  If omitted, the parameter given to each control will be null.</param>        
        private static IDictionary<IPropertyEditor, ControlInfo> GetEditors(object targetedObject, object parameter = null)
        {
            Dictionary<IPropertyEditor, ControlInfo> result = new Dictionary<IPropertyEditor, ControlInfo>();

            foreach (PropertyInfo pInfo in targetedObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                //Step #1 - get the Editable tag for the property.  If there is no such tag, the property shouldn't be included in the editor.
                Editable tag = (Editable)pInfo.GetCustomAttribute(typeof(Editable));
                if (tag == null) continue;

                //Step #2a - Try to create the control from the Tag's Control type.
                IPropertyEditor ipe = null;
                if (tag.Control != null)
                {
                    if (!typeof(FrameworkElement).IsAssignableFrom(tag.Control))
                        Console.WriteLine("Editable.Control for property " + targetedObject.GetType().Name + "." + pInfo.Name + " of type " + pInfo.PropertyType.Name
                                          + " does not inherit from FrameworkElement.  An editor cannot be auto-generated.");
                    if (!typeof(IPropertyEditor).IsAssignableFrom(tag.Control))
                        Console.WriteLine("Editable.Control for property " + targetedObject.GetType().Name + "." + pInfo.Name + " of type " + pInfo.PropertyType.Name
                                          + " does not inherit from IPropertyEditor.  An editor cannot be auto-generated.");
                    else
                        ipe = (AbstractPropertyEditor)Activator.CreateInstance(tag.Control);
                }
                //Step #2b - since there is no control type specified on the tag, try to find a default control.
                else
                    ipe = GetDefaultEditor(pInfo.PropertyType);
                //Step #2c - since there is no tag control, and no predefined default control, the control is going to be a string message.
                if (ipe == null)
                {
                    StringEditor lbl = new StringEditor();
                    lbl.Input = "No valid editor type established for property " + targetedObject.GetType().Name + "." + pInfo.Name + " of type " + pInfo.PropertyType.Name + ".";
                    lbl.IsReadOnly = true;
                    lbl.Label = (tag.Label == "") ? pInfo.Name : tag.Label;
                    result.Add(lbl, new ControlInfo(targetedObject, pInfo.GetValue(targetedObject), pInfo));
                    continue;
                }


                //Step #3 - Update the new control's links, and add the created control to the dictionary. 
                object originalInput = pInfo.GetValue(targetedObject);                
                ipe.Parameter = parameter;
                ipe.Input = originalInput;
                ipe.IsReadOnly = tag.IsReadOnly;
                ipe.Label = (tag.Label == "") ? pInfo.Name : tag.Label;
                result.Add(ipe, new ControlInfo(targetedObject, originalInput, pInfo));

                //Step #4, If it's an AbstractPropertyEditor, set up the links as possible.                
                AbstractPropertyEditor editor = ipe as AbstractPropertyEditor;
                if (editor == null) continue;
                editor.GroupName = tag.GroupName;
                editor.Visibility = (tag.IsVisible) ? Visibility.Visible : Visibility.Hidden;
                editor.Priority = tag.Priority;

            }

            return result;
        }

        private static IPropertyEditor GetDefaultEditor(Type propertyType)
        {
            if (propertyType == typeof(bool)) return new BoolEditor();
            if (propertyType == typeof(double)) return new DoubleEditor();
            if (propertyType == typeof(DateTime)) return new DateTimeEditor();
            if (propertyType == typeof(Point)) return new PointEditor();
            if (propertyType == typeof(string)) return new StringEditor();
            return null;
        }

        private struct ControlInfo
        {
            public readonly object OriginalValue;
            public readonly PropertyInfo Property;
            public readonly object Target;
            public ControlInfo(object target, object originalValue, PropertyInfo pInfo)
            {
                Target = target;
                OriginalValue = originalValue;
                Property = pInfo;
            }
        }



        /// <summary>
        /// A change to the DataContext signifies that a new object is being edited.  The new object might be of the same class as the old object, and so have the same properties, 
        /// but it might not.  Either way, the ControlInfo references will need to be created anew, and the Editor GUI re-done.
        /// </summary>
        private void On_DataContext_Changed(object sender, DependencyPropertyChangedEventArgs e)
        {
            Rebuild(e.NewValue);
        }




        /// <summary>
        /// Restores all values to what they were originally when the DataContext was set.
        /// </summary>
        public void Reset()
        {
            foreach (KeyValuePair<IPropertyEditor, ControlInfo> kvp in _Contexts)
            {
                IPropertyEditor ipe = kvp.Key;
                ControlInfo cInfo = kvp.Value;
                ipe.Input = cInfo.OriginalValue;
            }

            this.Result = null;
        }


        /// <summary>
        /// Cancels any changes to the DataContext object by restoring all values to what they were originally when the DataContext was set.
        /// </summary>
        public void Cancel()
        {
            Reset();
            this.Result = null;
        }


        /// <summary>
        /// Updates the DataContext object by writing new properties values for every non-readonly property being managed by a control.
        /// </summary>
        public void Update()
        {
            ///Possibility #1 - there's only a single EditContext being stored.  If there's only one Context being stored, it might be because it is the sole 
            ///Editable property in the context's target object, or it might be because this is a non-targeted edit setup.
            if (_Contexts.Count == 1)
            {
                KeyValuePair<IPropertyEditor, ControlInfo> kvp = _Contexts.First();
                IPropertyEditor ipe = kvp.Key;
                ControlInfo cInfo = kvp.Value;

                //Possibility #1a - Non-targeted autoeditor setup.  The result will the GetValue() of the singular editor.
                if (cInfo.Target == null || cInfo.Property == null)
                    this.Result = ((IPropertyEditor)ipe).GetValue();

                //Possibility #1b - Single Editable property for the target object.  The result will be the original targeted object, but Reflection will set the value of that target's property.
                else
                {
                    //TODO:  AutoEditor - check  that  each IPropertyEditor.Changed==true.
                    this.Result = cInfo.Target;
                    cInfo.Property.SetValue(cInfo.Target, ipe.GetValue());
                }
                return;
            }

            ///Possibility #2 - there are multiple EditContexts stored, which means there must be a stored TargetObject as well, and the AutoEditor.DataContext 
            ///must also be the TargetObject.
            else
            {
                //First, check that all values are valid.
                foreach (IPropertyEditor ipe in _Contexts.Keys)
                {
                    if (!ipe.IsValid)
                        throw new InvalidOperationException("Editor value for editor labeled " + ipe.Label + " is not valid.  Cannot update object.");
                }
                foreach (KeyValuePair<IPropertyEditor, ControlInfo> kvp in _Contexts)
                {
                    IPropertyEditor ipe = kvp.Key;
                    ControlInfo context = kvp.Value;
                    if (ipe.IsReadOnly || !context.Property.CanWrite) continue;

                    object newValue = ipe.GetValue();
                    try
                    {
                        context.Property.SetValue(context.Target, newValue);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }

                }
                this.Result = DataContext;
            }
        }






        /// <summary>
        /// The result of the Editor, once a button has been pushed.  Prior to a button being pushed, the default is null.
        /// </summary>
        public object Result { get; private set; } = null;

    }
}
