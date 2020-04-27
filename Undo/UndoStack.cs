using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures.Undo
{
    /// <summary>
    /// A thread-safe undo/redo stack.
    /// </summary>
    public class UndoStack : IUndoStack
    {
        private readonly object _Lock = new object();
        private readonly Stack<IUndoable> toUndo = new Stack<IUndoable>();
        private readonly Stack<IUndoable> toRedo = new Stack<IUndoable>();
        
        public void Push(IUndoable action)
        {
            lock (_Lock)
            {
                toUndo.Push(action);
                toRedo.Clear();
            }            
        }

        public void Clear()
        {
            lock (_Lock)
            {
                toUndo.Clear();
                toRedo.Clear();
            }           
        }

        
        public bool CanUndo { get { lock (_Lock) return toUndo.Count > 0; } }
        public bool CanRedo { get { lock (_Lock) return toRedo.Count > 0; } }

        public bool Undo()
        {
            lock (_Lock)
            {
                if (toUndo.Count < 1) return false;
                IUndoable u = toUndo.Pop();
                u.Undo();
                toRedo.Push(u);
                return true;
            }
        }

        public bool Redo()
        {
            lock (_Lock)
            {
                if (toRedo.Count < 1) return false;
                IUndoable u = toRedo.Pop();
                u.Redo();
                toUndo.Push(u);
                return true;
            }            
        }
    }
}
