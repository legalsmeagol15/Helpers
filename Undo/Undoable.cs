using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures.Undo
{
    public sealed class Undoable : IUndoable
    {
        public readonly Action Undo, Redo;
        public Undoable(Action undo, Action redo) { this.Undo = undo; this.Redo = redo; }

        void IUndoable.Redo() => Redo();

        void IUndoable.Undo() => Undo();
    }
    public enum UndoTypes { Add, Delete, Modify }
    public sealed class Undoable<T> : IUndoable
    {        
        public readonly UndoTypes ActionType;
        public readonly Action Undo;
        public readonly Action Redo;
        public readonly T OldValue;
        public readonly T NewValue;

        private Undoable(UndoTypes type, T oldValue, T newValue, Action undoAction, Action redoAction)
        {
            this.ActionType = type;
            this.Undo = undoAction;
            this.Redo = redoAction;
            this.OldValue = oldValue;
            this.NewValue = newValue;
        }

        public static Undoable<TChange> AsAdd<TChange>(TChange newValue, Action undoAction, Action redoAction)
            => new Undoable<TChange>(UndoTypes.Add, default(TChange), newValue, undoAction, redoAction);

        public static Undoable<TChange> AsDelete<TChange>(TChange oldValue, Action undoAction, Action redoAction)
            => new Undoable<TChange>(UndoTypes.Delete, oldValue, default(TChange), undoAction, redoAction);

        public static Undoable<TChange> AsModify<TChange>(TChange oldValue, TChange newValue, Action undoAction, Action redoAction)
            => new Undoable<TChange>(UndoTypes.Modify, oldValue, newValue, undoAction, redoAction);

        void IUndoable.Redo() => this.Redo();

        void IUndoable.Undo() => this.Undo();
    }
}
