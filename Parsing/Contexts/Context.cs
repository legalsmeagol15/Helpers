using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Parsing.Contexts
{
    public static class Context
    {
        
        /// <summary>Returns whether the given child is a descendant of (or identical to) the given parent.</summary>        
        public static bool IsDescendant(IContext parent, IContext child)
        {
            while (child != null)
            {
                if (child.Equals(parent)) return true;
                child = child.Parent;
            }
            return false;
        }

    }


}
