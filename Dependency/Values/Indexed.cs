using Dependency.Variables;
using Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency.Values
{
    /// <summary>
    /// This class essentially just wraps another <seealso cref="IEvaluateable"/> object, and 
    /// associates a <see cref="Index"/> to it.  Its job is to constrain the updated domain when 
    /// the <see cref="Contents"/> is updated.
    /// </summary>
    /// <typeparam name="ByT">The type of value to index by.  For example, in a number-based 
    /// list, the <typeparamref name="ByT"/> would be <seealso cref="Number"/>.</typeparam>
    internal sealed class Indexed<ByT> : ISyncUpdater where ByT : IEvaluateable
    {
        public ByT Index { get; internal set; }
        public IIndexable Parent { get; internal set; }
        ISyncUpdater ISyncUpdater.Parent { get => (ISyncUpdater)this.Parent; set { this.Parent = (IIndexable)value; } }

        public IEvaluateable Value => Contents.Value;

        private IEvaluateable _Contents;
        public IEvaluateable Contents
        {
            get => _Contents;
            set
            {
                if (_Contents is ISyncUpdater isu_old) 
                    isu_old.Parent = null;
                
                if ((_Contents = value) is ISyncUpdater isu_new)
                {
                    Debug.Assert(isu_new.Parent == null);
                    isu_new.Parent = this;
                }
                if (this.Parent != null)
                    this.Parent.IndexedContentsChanged(Index, value);
            }
        }

        public Indexed(IIndexable parent, IEvaluateable contents, ByT index)
        {
            this.Parent = parent;
            this.Contents = contents;
            if ((this.Contents = contents) is ISyncUpdater isu) isu.Parent = this;
            this.Index = index;
        }

        ITrueSet<IEvaluateable> ISyncUpdater.Update(Update caller, ISyncUpdater updatedChild, ITrueSet<IEvaluateable> indexedDomain)
        {
            Debug.Assert(updatedChild == Contents);
            if (!indexedDomain.Contains(Index)) return null;
            return indexedDomain;
        }

        internal bool SetError()
        {
            if (_Contents is IVariable v && !(v.Contents is IndexingError))
            {
                Update.ForVariable(v, new IndexingError(null, "Invalid index: " + this.Index.ToString())).Execute();
                return true;
            }
            return false;
                
        }

        public override string ToString() => Index.ToString() + ":" + Value.ToString();
    }
}
