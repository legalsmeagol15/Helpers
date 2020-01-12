using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dependency.Functions;
using Dependency.Variables;

namespace Dependency
{
    public sealed class DynamicVector : IEvaluateable, ITypeGuarantee, IContext, ISyncUpdater, IIndexable, IList<IEvaluateable>
    {

        private readonly List<IEvaluateable> _Items;

        public DynamicVector(IEnumerable<IEvaluateable> items) { this._Items = items.ToList(); }
        public DynamicVector(params IEvaluateable[] items):  this((IEnumerable<IEvaluateable>)items){}
        public DynamicVector(params decimal[] items) : this(items.Select( m => (IEvaluateable)(new  Number(m)))) { }

        public IEvaluateable this[int idx]
        {
            get
            {
                if (idx < 0 || idx >= _Items.Count)
                    throw new IndexOutOfRangeException("Index " + idx + " out of range.");
                return _Items[idx];
            }
            set
            {
                if (idx < 0 || idx > _Items.Count)
                    throw new IndexOutOfRangeException("Index " + idx + " out of range.");
                if (idx == _Items.Count)
                    Add(value);

            }
        }

        public void Add(IEvaluateable item)
        {
            Update.StructureLock.EnterWriteLock();
            try
            {
                _Items.Add(item);
                Update.ForVector(this).Execute();
            } finally            { Update.StructureLock.ExitWriteLock(); }
         }
        public void Clear()
        {
            Update.StructureLock.EnterWriteLock();
            try
            {
                _Items.Clear();
                Update.ForVector(this).Execute();
            }
            finally { Update.StructureLock.ExitWriteLock(); }
        }
        public void Remove(int idx)
        {
            Update.StructureLock.EnterWriteLock();
            try
            {
                _Items.RemoveAt(idx);
                Update.ForVector(this).Execute();
            }
            finally { Update.StructureLock.ExitWriteLock(); }
        }
        
        // This vector is a context, because it has a property called "count".
        private WeakReference<Variable> _CountRef;
        internal bool TryGetProperty(string path, out IEvaluateable source)
        {
            switch (path.ToLower())
            {
                case "size":
                case "count":
                case "length":
                    Variable countVar = Update.GetWeakVariableSafe(ref _CountRef, new Variable(new Number(_Items.Count)));
                    source = countVar;
                    return true;
                default: source = null; return false;
            }
        }
        bool IContext.TryGetProperty(string path, out IEvaluateable source) => this.TryGetProperty(path, out source);
        bool IContext.TryGetSubcontext(string path, out IContext ctxt) { ctxt = null; return false; }

        private bool TryIndex(IEvaluateable ordinal, out IEvaluateable val)
        {
            if (!(ordinal is Number idx)) { val = null; return false; }
            if (idx < 0 || idx >= _Items.Count) { val = null; return false; }
            val = _Items[idx];
            return true;
        }
        bool IIndexable.TryIndex(IEvaluateable ordinal, out IEvaluateable val) => TryIndex(ordinal, out val);

        TypeFlags ITypeGuarantee.TypeGuarantee => TypeFlags.Vector;

        ISyncUpdater ISyncUpdater.Parent { get; set; }

        // If a contents indexed member changed, then of course the value of the vector changed.
        bool ISyncUpdater.Update(Variables.Update u, ISyncUpdater uc) => true;

        IEvaluateable IEvaluateable.Value => this;

        public override string ToString() => "{" + string.Join(",", _Items.Select(i => i.ToString())) + "}";

    }
    
    public sealed class Vector : IFunction, IEvaluateable, ITypeGuarantee, IContext, ISyncUpdater, IIndexable
    // Though a Vector has inputs, it CANNOT be a Function.
    {
        public IList<IEvaluateable> Inputs { get; internal set; }  // TODO:  indexes shouldn't be publicly settable.
        private Vector _Value = null;
        public Vector Value => _Value ?? (_Value = new Vector(Inputs.Select(i => i.Value)));
        public IEvaluateable this[int idx]
        {
            get
            {
                if (idx >= 0 && idx <= Inputs.Count) return Inputs[idx].Value;
                return new IndexingError(this, Inputs, "Index " + idx + " out of range.");
            }
        }

        private Vector(Vector contentVector)
        {

        }
        internal Vector(IEnumerable<IEvaluateable> contents) => Inputs = contents.ToArray();
        internal Vector(IList<IEvaluateable> contents) => Inputs = contents.ToArray();
        public Vector(params IEvaluateable[] contents) => Inputs = contents;
        public Vector(params decimal[] contents) 
            : this (contents.Select(m => new Number(m)).OfType<IEvaluateable>().ToArray())
        {
        }
        public Vector() { }

        IEvaluateable IEvaluateable.Value => Value;

        public int Size => Inputs.Count;

        public IEvaluateable MaxIndex => new Number(Inputs.Count - 1);

        public IEvaluateable MinIndex => Number.Zero;

        public override bool Equals(object obj)
        {
            if (!(obj is Vector other)) return false;
            if (Size != other.Size) return false;
            for (int i = 0; i < Size; i++) if (!Equals(Inputs[i].Value, other.Inputs[i].Value)) return false;
            return true;
        }
        //public override int GetHashCode() { unchecked { return (int)Inputs.Sum(i => i.GetHashCode()); } }
        public override int GetHashCode() => base.GetHashCode();

        bool IContext.TryGetSubcontext(string path, out IContext ctxt) { ctxt = null; return false; }

        internal bool TryGetProperty(string  path, out IEvaluateable source)
        {
            switch (path)
            {
                case "size":
                case "count":
                case "length": source = new Number(Value.Size); return true;
                case "min": source = Number.Zero; return true;
                case "max": source = new Number(Value.Size - 1); return true;
                default: source = null; return false;
            }
        }
        bool IContext.TryGetProperty(string  path, out IEvaluateable source) => this.TryGetProperty(path, out source);

        TypeFlags ITypeGuarantee.TypeGuarantee => TypeFlags.Vector;

        ISyncUpdater ISyncUpdater.Parent { get; set; }

        public override string ToString() => "{" + string.Join(",", Inputs.Select(i => i.ToString())) + "}";

        // If the value of an indexed member changed, then of course the value of the vector changed.
        bool ISyncUpdater.Update(Variables.Update u, ISyncUpdater uc) => true;

        bool IIndexable.TryIndex(IEvaluateable ordinal, out IEvaluateable val)
        {
            if (!(ordinal is Number n) || !n.IsInteger) { val = null; return false; }
            val= this[(int)n];
            return !(val is Error);
        }
    }
}
