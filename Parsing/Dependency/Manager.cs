using DataStructures.Undo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Parsing.Dependency
{
    
    public sealed class Manager
    {
        // NOT THREAD-SAFE!!!

        private IUndoStack _UndoStack;
        public Manager(IUndoStack undoStack = null) { this._UndoStack = undoStack; }
        
        internal readonly Dictionary<Type, ClassProfile> ClassProfiles = new Dictionary<Type, ClassProfile>();
        internal readonly Dictionary<object, Context> Roots = new Dictionary<object, Context>();

    }
    
}
