using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dependency.Functions;
using Dependency.Variables;

namespace Dependency
{
    
    public sealed class Vector : ITypeGuarantee, IContext, ISyncUpdater, IIndexable, IList<IEvaluateable>
    // Though a Vector has inputs, it CANNOT be a Function.
    // Should a Vector be mutable, or should it not?  I've gone back and forth.  At this point, I'm 
    // saying YES.
    {
        private struct IndexWrapper : ISyncUpdater, IEvaluateable
        {
            public int Index;
            public IEvaluateable Evaluateable;
            public IndexWrapper(Vector parent, int index, IEvaluateable evaluateable)
            {
                this.Parent = parent;
                this.Index = index;
                this.Evaluateable = evaluateable;
            }
            public Vector Parent;
            ISyncUpdater ISyncUpdater.Parent { get => Parent; set => throw new NotImplementedException(); }
            IEvaluateable IEvaluateable.Value => Evaluateable.Value;
            bool ISyncUpdater.Update(Update caller, ISyncUpdater updatedChild)
            {
                Debug.Assert(updatedChild == Evaluateable);
                return true;
            }
            public override bool Equals(object obj)
                => obj is IndexWrapper other && Evaluateable.Equals(other.Evaluateable);
            public override int GetHashCode()
                => throw new NotImplementedException();
            public override string ToString() => Evaluateable.ToString();
        }
        private readonly IList<IEvaluateable> _Members;
        private Vector _Value = null;
        IEvaluateable IEvaluateable.Value => Value;
        public Vector Value => _Value ?? (_Value = new Vector(_Members.Select(i => i.Value)) { Parent = this});
        public IEvaluateable this[int idx]
        {
            get
            {
                if (TryIndex(new Number(idx), out IndexWrapper result))
                    return result.Evaluateable;
                return new IndexingError(this, (IList<IEvaluateable>)_Members, "Index " + idx + " out of range.");
            }
            set
            {
                IndexWrapper wrapper = (IndexWrapper)_Members[idx];
                if (wrapper.Evaluateable.Equals(value)) return;
                wrapper.Evaluateable = value;
                if (_Value != null)
                    _Value[idx] = value.Value;
                SignalReindex(wrapper.Index);
            }
        }

        internal Vector(IEnumerable<IEvaluateable> contents)
        {
            _Members = new System.Collections.Generic.List<IEvaluateable>();
            int idx = 0;
            foreach (IEvaluateable c in contents)
                _Members.Add(new IndexWrapper(this, idx++, c));
        }
        internal Vector(IList<IEvaluateable> contents) : this((IEnumerable<IEvaluateable>)contents) { }
        public Vector(params IEvaluateable[] contents) : this((IEnumerable<IEvaluateable>)contents) { }
        public Vector(params decimal[] contents) 
            : this (contents.Select(m => new Number(m)).OfType<IEvaluateable>().ToArray())
        { }
        public Vector() { }

        public int Count => _Members.Count;

        
        bool IContext.TryGetSubcontext(string path, out IContext ctxt) { ctxt = null; return false; }

        internal bool TryGetProperty(string  path, out IEvaluateable source)
        {
            switch (path)
            {
                case "size":
                case "count":
                case "length": source = new Number(Value.Count); return true;
                case "min": source = Number.Zero; return true;
                case "max": source = new Number(Value.Count - 1); return true;
                default: source = null; return false;
            }
        }
        bool IContext.TryGetProperty(string  path, out IEvaluateable source) => this.TryGetProperty(path, out source);

        TypeFlags ITypeGuarantee.TypeGuarantee => TypeFlags.Vector;
        internal ISyncUpdater Parent { get; set; }
        ISyncUpdater ISyncUpdater.Parent { get => Parent; set { Parent = Value; } }
        bool ISyncUpdater.Update(Variables.Update u, ISyncUpdater updatedChild)
        {
            IndexWrapper child = (IndexWrapper)updatedChild;
            if (_Value != null)
                _Value._Members[child.Index] = child.Evaluateable.Value;
            SignalReindex(child.Index);
            return true;
        }

        private void SignalReindex(int startIndex, int endIndex = -1)
        {
            if (Parent is IIndexingUpdater iiu)
            {
                do
                {
                    iiu.Reindex(new Number(startIndex));
                } while (startIndex++ <= endIndex);
            }
            else if (Parent is Indexing ii)
                // I don't know if this is even possible.
                ii.Reindex();
        }
        bool IIndexable.ControlsReindex => true;

        bool ICollection<IEvaluateable>.IsReadOnly => false;

        private bool TryIndex(IEvaluateable ordinal, out IndexWrapper wrapper)
        {
            if (ordinal is Number n && n.IsInteger)
            {
                int idx = (int)n;
                if (idx>=0 && idx < Count)
                {
                    wrapper = (IndexWrapper)_Members[idx];
                    return true;
                }
            }
            wrapper = default;
            return false;
        }
        bool IIndexable.TryIndex(IEvaluateable ordinal, out IEvaluateable val)
        {
            if (this.TryIndex(ordinal, out IndexWrapper wrapper)) { val = wrapper.Evaluateable; return true; }
            val = default;
            return false;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Vector other)) return false;
            if (Count != other.Count) return false;
            for (int i = 0; i < Count; i++)
                if (!Equals(_Members[i], other._Members[i]))
                    return false;
            return true;
        }
        public override int GetHashCode() => throw new NotImplementedException();
        public override string ToString() => "{" + string.Join(",", _Members.Select(i => i.ToString())) + "}";

        #region Vector IList implementations

        
        public int IndexOf(IEvaluateable item)
        {
            for (int i = 0; i < _Members.Count; i++)
            {
                if (_Members[i] is IndexWrapper wrapper && wrapper.Evaluateable.Equals(item))
                    return i;
                else if (_Members[i].Equals(item))
                    // Is this even possible?
                    return i;
            }
            return -1;
        }

        public void Insert(int index, IEvaluateable item)
        {
            IndexWrapper newWrapper = new IndexWrapper(this, index, item);
            _Members.Insert(index, newWrapper);
            for (int i = index + 1; i < _Members.Count; i++)
            {
                IndexWrapper wrapper = (IndexWrapper)_Members[i];
                wrapper.Index = i;
            }
            if (_Value != null) _Value.Insert(index, item.Value);
            SignalReindex(index, _Members.Count - 1);
        }

        public void RemoveAt(int index)
        {
            _Members.RemoveAt(index);
            for (int i = index; i < _Members.Count; i++)
            {
                IndexWrapper wrapper = (IndexWrapper)_Members[i];
                wrapper.Index = i;
            }
            if (_Value != null) _Value.RemoveAt(index);
            SignalReindex(index, _Members.Count);
        }

        public void Add(IEvaluateable item)
        {
            IndexWrapper wrapper = new IndexWrapper(this, _Members.Count, item);
            _Members.Add(wrapper);
            if (_Value != null)
                _Value._Members.Add(item.Value);
            SignalReindex(_Members.Count - 1);
        }

        public void Clear()
        {
            int lastIdx = _Members.Count - 1;
            _Members.Clear();
            if (_Value != null)
                _Value.Clear();
            SignalReindex(0, lastIdx);
        }

        public bool Contains(IEvaluateable item) => IndexOf(item) >= 0;

        void ICollection<IEvaluateable>.CopyTo(IEvaluateable[] array, int arrayIndex)
        {
            int i = 0;
            while (arrayIndex < array.Length)
                array[arrayIndex++] = ((IndexWrapper)_Members[i++]).Evaluateable;
        }

        public bool Remove(IEvaluateable item)
        {
            int idx = IndexOf(item);
            if (idx < 0) return false;
            _Members.RemoveAt(idx);
            if (_Value != null)
                _Value.RemoveAt(idx);
            return true;
        }

        IEnumerator<IEvaluateable> IEnumerable<IEvaluateable>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        private IEnumerator<IEvaluateable> GetEnumerator()
        {
            foreach (IEvaluateable item in _Members)
            {
                if (item is IndexWrapper wrapper) yield return wrapper.Evaluateable;
                else yield return item;
            }
        }

        

        #endregion
    }
}
