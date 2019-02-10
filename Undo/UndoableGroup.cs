using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures.Undo
{
    public class UndoableGroup : IUndoable
    {
        private readonly List<IUndoable> _Actions;
        public UndoableGroup(IUndoable action) { _Actions = new List<IUndoable>(); _Actions.Add(action); }
        public UndoableGroup(IEnumerable<IUndoable> actions) { _Actions = new List<IUndoable>(actions); }

        public void Redo() { foreach (IUndoable action in _Actions) action.Undo(); }

        public void Undo() { foreach (IUndoable action in _Actions) action.Redo(); }
        public void Add(IUndoable action) => _Actions.Add(action);
    }
}
