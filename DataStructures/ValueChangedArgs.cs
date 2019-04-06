using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{
    /// <summary>A simple, generic class representing a change from one value to another.</summary>
    public class ValueChangedArgs<T> : EventArgs
    {
        /// <summary>The value before the change.</summary>
        public readonly T Before;
        /// <summary>The value after the change.</summary>
        public readonly T After;
        /// <summary>Creates a new <see cref="ValueChangedArgs{T}"/>.</summary>
        public ValueChangedArgs(T before, T after) { this.Before = before;this.After = after; }
    }
}
