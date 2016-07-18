using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfControls.Editing
{

    /// <summary>
    /// An interface designed to work with the Editor class to automatically generate controls that will handle property changes.  A control that implements IPropertyEditor will have the 
    /// advantage of of maintaining a context for validating the value, a separate label string for identifying the property, and bool signals for whether changes to the property should 
    /// be reflected immediate in the linked object or not.  Custom controls that derive from Editor will automatically take advantage of these properties when an object is edited and any 
    /// of its Editable properties reflect an IPropertyEditor control type.
    /// </summary>
    public interface IPropertyEditor
    {
        bool IsValid { get; }

        //bool UpdateImmediately { get; set; }

        string Label { get; set; }

        object Value { get; set; }

        object EditContext { get; set; }

        bool IsReadOnly { get; set; }


        event EventHandler ValueChanged;

    }
    
}
