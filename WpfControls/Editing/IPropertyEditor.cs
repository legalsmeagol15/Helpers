using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfControls.Editing
{

    /// <summary>
    /// An interface designed to work with the Editor class to automatically generate controls that will handle property changes.  A control that implements 
    /// IPropertyEditor will have the advantage of of maintaining a context parameter for validating the value, a separate label string for identifying the 
    /// property, and bool signals for whether changes to the property should be reflected immediate in the linked object or not.  Custom controls that derive 
    /// from Editor will automatically take advantage of these properties when an object is edited and any of its Editable properties reflect an IPropertyEditor 
    /// control type.
    /// </summary>
    public interface IPropertyEditor
    {
        /// <summary>
        /// Indicates whether an editor is currently in a valid input value state.
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// The label used for identifying an editor in a GUI.
        /// </summary>
        string Label { get; set; }

        /// <summary>
        /// The input value for an editor.
        /// </summary>
        object Input { get; set; }

        /// <summary>
        /// Whether or not the represented value of an editor has changed.
        /// </summary>
        bool Changed { get; }

        /// <summary>
        /// Returns the finalized value of an editor.
        /// </summary>        
        object GetValue();

        /// <summary>
        /// An editing parameter to be passed to an editor.
        /// </summary>
        object Parameter { get; set; }

        /// <summary>
        /// Whether or not an editor is intended to be read-only.
        /// </summary>
        bool IsReadOnly { get; set; }

        /// <summary>
        /// An event signifying a change in the value of the editor.
        /// </summary>
        event EventHandler ValueChanged;

    }
    
    
}
