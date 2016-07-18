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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfControls.Editing
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:WpfControls.Editing"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:WpfControls.Editing;assembly=WpfControls.Editing"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:Editor/>
    ///
    /// </summary>
    public class Editor : Control
    {
        static Editor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Editor), new FrameworkPropertyMetadata(typeof(Editor)));
        }

        public Editor()
        {
            //This constructor required for use by the WPF Designer.
            this.DataContextChanged += On_DataContext_Changed;           
        }

        /// <summary>
        /// The list of editing controls for this editor.
        /// </summary>
        protected Dictionary<FrameworkElement, ControlInfo> ControlInfos { get; private set; } = new Dictionary<FrameworkElement, ControlInfo>();

        private Panel _ControlPanel = null;
        private bool _ImmediateUpdates = false;

        public static DependencyProperty EditContextProperty = DependencyProperty.Register("EditContext", typeof(object), typeof(Editor), new PropertyMetadata(null));
        /// <summary>
        /// The metadata about the editable properties of an object.  This data will usually contain information such as available selections for a property, minimum or maximum range for a property, 
        /// the palette for a property, etc.
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
        /// <summary>
        /// A change to the editing context signifies that the editor should be rebuilt.
        /// </summary>
        private static void On_EditContext_Changed(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            Editor editor = sender as Editor;
            if (editor != null) editor.Rebuild();
        }
        
        protected void Rebuild()
        {
            //Whenever the DataContext changes, the Editor must re-form itself 
            object newObject = DataContext;

            //Since changing the DataContext counts as a cancel, the previous DataContext should be restored.
            Reset();


            //Step #0 - remove all event handlers from old IPropertyEditor controls
            foreach (FrameworkElement fwe in ControlInfos.Keys)
            {
                IPropertyEditor pe = fwe as IPropertyEditor;
                if (pe == null) continue;
                pe.ValueChanged -= On_PropertyEditor_ValueChanged;
            }


            //Step #1 - find all the properties that are Editable, and create ControlInfo objects for them to be stored.
            Dictionary<string, List<ControlInfo>> categories = new Dictionary<string, List<ControlInfo>>();
            ControlInfos = new Dictionary<FrameworkElement, ControlInfo>();
            Type newObjectType = newObject.GetType();
            foreach (PropertyInfo pInfo in newObjectType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                //Get the Editable tag, if there is one (if not, just skip).
                Editable tag = (Editable)Attribute.GetCustomAttribute(pInfo, typeof(Editable));
                if (tag == null) continue;

                //Group according to tag.
                List<ControlInfo> group;
                if (!categories.TryGetValue(tag.GroupName, out group))
                {
                    group = new List<ControlInfo>();
                    categories.Add(tag.GroupName, group);
                }


                //Get the current value of the property.
                object originalValue = pInfo.GetValue(newObject);

                //Create the tag's control.
                FrameworkElement control = tag.Control;
                if (control == null)
                {

                    if (pInfo.PropertyType == typeof(double))
                        control = new DoubleEditor();
                    //TODO:  common editor controls.

                    else
                    {
                        Label lbl = new Label();
                        string tagLabel = tag.Label == "" ? pInfo.Name : tag.Label;
                        lbl.Content = tagLabel + ": no default editor for type " + pInfo.PropertyType.Name + ".";
                        control = lbl;
                    }

                }


                //If the control is a defined IPropertyEditor, take advantage of that.
                if (control is IPropertyEditor)
                {
                    IPropertyEditor pe = (IPropertyEditor)control;
                    pe.Label = tag.Label == "" ? pInfo.Name : tag.Label;
                    pe.EditContext = EditContext;
                    pe.Value = originalValue;
                    
                    pe.ValueChanged += On_PropertyEditor_ValueChanged;
                }
                else
                    control.DataContext = originalValue;

                //Store the control info.
                ControlInfo cInfo = new ControlInfo(pInfo, tag, originalValue, control);
                ControlInfos[control] = cInfo;
                group.Add(cInfo);
            }

            //Now, add according to group.
            if (_ControlPanel == null)
            {
                Console.WriteLine("ControlPanel is null.");
                return;
            }


            //Step #2 - Time to create the GUI for editing the new DataContext and add it to the Editor.
            //Add what needs to be in a tree, to the tree
            _ControlPanel.Children.Clear();
            TreeView tree = null;
            foreach (string groupName in categories.Keys)
            {
                if (groupName == "") continue;                
                categories[groupName].Sort((a, b) => a.Tag.Priority.CompareTo(b.Tag.Priority));

                if (tree == null) tree = new TreeView();
                TreeViewItem groupTvi = null;
                foreach (TreeViewItem t in tree.Items)
                {
                    if ((string)t.Header == groupName)
                    {
                        groupTvi = t;
                        break;
                    }
                }
                if (groupTvi == null)
                {
                    groupTvi = new TreeViewItem();
                    groupTvi.Header = groupName;
                    groupTvi.IsExpanded = true;
                    tree.Items.Add(groupTvi);
                }
                foreach (ControlInfo cInfo in categories[groupName])
                    groupTvi.Items.Add(cInfo.Control);

            }
            if (tree != null) _ControlPanel.Children.Add(tree);

            //Add non-categorized properties.
            if (categories.ContainsKey(""))
                foreach (ControlInfo cInfo in categories[""]) _ControlPanel.Children.Add(cInfo.Control);

        }

        /// <summary>
        /// A change to the DataContext signifies that a new object is being edited.  The new object might be of the same class as the old object, and so have the same properties, but it 
        /// might not.  Either way, the ControlInfo references will need to be created anew, and the Editor GUI re-done.
        /// </summary>
        private void On_DataContext_Changed(object sender, DependencyPropertyChangedEventArgs e)
        {
            Rebuild();
        }

        

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
          
            _ControlPanel = GetTemplateChild("PART_PropertyControls") as Panel;
            if (_ControlPanel == null) throw new InvalidOperationException("Editor must contain a \"PART_PropertyControls\".");

        }

        protected void On_PropertyEditor_ValueChanged(object sender, EventArgs e)
        {
            if (DataContext == null) return;
            if (!_ImmediateUpdates) return;

            IPropertyEditor propEditor = sender as IPropertyEditor;
            if (propEditor != null && propEditor is FrameworkElement && propEditor.IsValid)
            {
                FrameworkElement control = (FrameworkElement)propEditor;
                ControlInfo cInfo = ControlInfos[control];
                cInfo.Property.SetValue(DataContext, propEditor.Value);
            }
        }

        
        public void Reset()
        {
            foreach (FrameworkElement fwe in ControlInfos.Keys)
                fwe.DataContext = ControlInfos[fwe].OriginalValue;
        }
        public void Cancel()
        {
            Reset();
            this.Result = MessageBoxResult.Cancel;
        }
        public void Update()
        {
            object obj = DataContext;
            if (obj == null) throw new InvalidOperationException("No DataContext for Editor to update.");

            foreach (FrameworkElement control in ControlInfos.Keys)
            {
                ControlInfo cInfo = ControlInfos[control];
                if (cInfo.Tag.IsReadOnly) continue;
                if (!cInfo.Property.CanWrite) continue;

                //Is the property edited by a PRopertyEditor?  If so, update it according to the IPropertyEditor.Value                    
                if (control is IPropertyEditor)
                {
                    IPropertyEditor ipe = (IPropertyEditor)control;
                    if (!ipe.IsValid ||ipe.IsReadOnly) continue;
                    try
                    {
                        cInfo.Property.SetValue(obj, ipe.Value);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error writing to property " + (cInfo.Tag.Label == "" ? cInfo.Property.Name : cInfo.Tag.Label) + ".\n" + ex.ToString());
                    }

                }
                //If  the property is not edited by a PRopertyEditor, just update according to whatever the control's DataContext is.
                else
                {
                    try
                    {
                        cInfo.Property.SetValue(obj, control.DataContext);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error writing to property " + cInfo.Property.Name + ".\n" + ex.ToString());
                    }

                }

            }
            this.Result = MessageBoxResult.OK;            
        }

        

        /// <summary>
        /// A lightweight data object used to associate a PropertyInfo, editable Tag, and Control for modifying the property.
        /// </summary>
        protected struct ControlInfo
        {
            public readonly PropertyInfo Property;
            public readonly Editable Tag;
            public readonly object OriginalValue;
            public readonly FrameworkElement Control;

            public ControlInfo(PropertyInfo pInfo, Editable tag, object originalValue, FrameworkElement control)
            {
                this.Property = pInfo;
                this.Tag = tag;
                this.OriginalValue = originalValue;
                this.Control = control;
            }

        }

        /// <summary>
        /// The result of the Editor, once a button has been pushed.  Prior to a button being pushed, the default is 'Cancel'.
        /// </summary>
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.Cancel;



        
        public static DependencyProperty AllowsResetProperty
            = DependencyProperty.Register("AllowsReset", typeof(bool), typeof(Editor), new PropertyMetadata(true));
        public bool AllowsReset
        {
            get
            {
                return (bool)GetValue(AllowsResetProperty);
            }
            set
            {
                SetValue(AllowsResetProperty, value);
            }
        }
        private void Reset_Button_Click(object sender, RoutedEventArgs e)
        {
            foreach (FrameworkElement fwe in ControlInfos.Keys)
                fwe.DataContext = ControlInfos[fwe].OriginalValue;
        }
    }
}
