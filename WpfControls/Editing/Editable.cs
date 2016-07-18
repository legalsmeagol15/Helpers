using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfControls.Editing
{

    /// <summary>
    /// Indicates that a properity is an essential or editable attribute describing the structure of a given object, for use in serialization and automatic 
    /// editor generation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class Editable : Attribute
    {
        /// <summary>
        /// Creates a new property.
        /// </summary>
        /// <param name="groupName">The name of the group to which this tagged property is related, ie, "Appearance", "Shape", "Use", etc.</param>
        public Editable(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                throw new ArgumentException("Group name must be non-whitespace.");

            this.GroupName = groupName;
        }

        /// <summary>
        /// The name of the group to which this tagged property is related within this object type.  For example, all properties whose group name is 
        /// "Appearance" will be sorted into an "Appearance" group when being edited.
        /// </summary>
        public string GroupName { get; }


        /// <summary>
        /// Determines whether the tagged property is intended to be read-only.
        /// </summary>
        public bool IsReadOnly { get; set; } = false;


        private int _Priority = int.MaxValue;
        /// <summary>
        /// The 0-based index at which a tagged property should appear in a list within the given group.  Properties with no priority specified will 
        /// appear after properties with a defined priority.  Properties with identical priorities will appear in indeterminate order.
        /// </summary>
        public int Priority
        {
            get
            {
                return _Priority;
            }
            set
            {
                if (value < 0) throw new ArgumentException("Priority must not be negative.");
                _Priority = value;
            }
        }

        /// <summary>
        /// Allows this tag to define a preferred property name if the name of property to which it is applied is unsatisfactory.
        /// </summary>
        public string Label { get; set; } = "";

        /// <summary>
        /// Any information that needs to be passed to an editor for use in building the editing control for the tagged property or object.
        /// </summary>
        public object Context { get; set; } = null;

        /// <summary>
        /// The control object whose DataContext will be set to manipulate the value of a given property.  If null, auto-generation of an editor will attempt 
        /// to find a default editor for the type of the property.  If no such default editor can be found, a label will be applied to the editor.
        /// </summary>
        public FrameworkElement Control { get; set; } = null;


        public override string ToString()
        {
            return "Editable group name:" + GroupName + "  priority:" + _Priority;
        }
    }
}
