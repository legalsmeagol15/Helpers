using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures.Undo
{
    public interface IUndoStack
    {
        bool Undo();
        bool Redo();
        void Push(IUndoable undoable);

        bool CanUndo { get; }
        bool CanRedo { get; }
    }
}
