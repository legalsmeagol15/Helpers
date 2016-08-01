using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Xml.Serialization;

namespace WpfControls.Editing
{

    /// <summary>
    /// Indicates that a properity is an essential or editable attribute describing the structure of a given object, for use in serialization and automatic 
    /// editor generation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class Editable : Attribute
    {
        ///// <summary>
        ///// Creates a new property.
        ///// </summary>
        ///// <param name="groupName">The name of the group to which this tagged property is related, ie, "Appearance", "Shape", "Use", etc.</param>
        //public Editable()
        //{
            
        //}

        /// <summary>
        /// The name of the group to which this tagged property is related within this object type.  For example, all properties whose group name is 
        /// "Appearance" will be sorted into an "Appearance" group when being edited.
        /// </summary>
        public string GroupName { get; set; }


        /// <summary>
        /// Determines whether the tagged property is intended to be read-only.
        /// </summary>
        public bool IsReadOnly { get; set; } = false;


        /// <summary>
        /// Determines whether the property will be visible in an automatically-generated editor.
        /// </summary>
        public bool IsVisible { get; set; } = true;


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

        ///// <summary>
        ///// Any information that needs to be passed to an editor for use in building the editing control for the tagged property or object.
        ///// </summary>
        //public object Context { get; set; } = null;

        /// <summary>
        /// The control object whose DataContext will be set to manipulate the value of a given property.  If null, auto-generation of an editor will attempt 
        /// to find a default editor for the type of the property.  If no such default editor can be found, a label will be applied to the editor.
        /// </summary>
        public Type Control { get; set; } = null;


        public override string ToString()
        {
            return "Editable group name:" + GroupName + "  priority:" + _Priority;
        }



        /// <summary>
        /// Returns a dictionary of all Editable properties in this stroke, keyed according to the property name.
        /// </summary>
        /// <param name="obj">The object whose Editable properties are sought.</param>
        /// <param name="writeableOnly">True if only PropertyInfo.CanWrite properties are to be included; otherwise, properties will be included regardless of 
        /// whether they have a 'set' accessor.</param>
        /// <param name="excludeNonSerializable">True if the presence of the NonSerializable tag will exclude a property; otherwise, properties will be included regardless of the 
        /// NonSerializable property.</param>        
        /// <param name="useEditableLabel">True if the property is to be keyed according to its Editable.Label property (assuming this is non-whitespace); otherwise, the property will 
        /// be keyed according  to its PropertyInfo.Name.</param>
        public static IDictionary<string, PropertyInfo> GetEditableProperties(object obj, bool includeNonPublic, bool writeableOnly = true, bool excludeNonSerializable = true, bool useEditableLabel = false)
        {
            Dictionary<string, PropertyInfo> props = new Dictionary<string, PropertyInfo>();
            BindingFlags flags = includeNonPublic ? (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static) : BindingFlags.Public;

            foreach (PropertyInfo pInfo in obj.GetType().GetProperties( flags))
            {
                //Non-writeable properties make no sense to serialize.
                if (writeableOnly && !pInfo.CanWrite) continue;

                //Is there an editable tag?  If not, skip this property.
                Editable editableTag = (Editable)pInfo.GetCustomAttribute(typeof(Editable));
                if (editableTag == null) continue;

                //Is there a NonSerialized tag?  If so, skip this property.
                if (excludeNonSerializable)
                {
                    NonSerializedAttribute nonSerializedTag = (NonSerializedAttribute)pInfo.GetCustomAttribute(typeof(NonSerializedAttribute));
                    if (nonSerializedTag != null) continue;
                }

                //Add this property to the list.
                string label = (!useEditableLabel || editableTag.Label == "") ? pInfo.Name : editableTag.Label;
                props.Add(label, pInfo);
            }

            return props;
        }

    }
}
