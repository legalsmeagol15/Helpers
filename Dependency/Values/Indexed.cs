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
    /// <typeparam name="ByT"></typeparam>
    internal sealed class Indexed<ByT> : ISyncUpdater where ByT : IEvaluateable
    {
        public ByT Index { get; set; }
        public IIndexable Parent { get; set; }
        ISyncUpdater ISyncUpdater.Parent { get => this.Parent; set { this.Parent = (IIndexable)value; } }

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
            Debug.Assert(indexedDomain.IsUniversal, "Misuse of " + nameof(Indexed<ByT>) + " - this class should exist only as a client of an " + nameof(IIndexable) + " and should only receive universal indices.");
            Debug.Assert(Value == Contents.Value);
            return indexedDomain;
        }
    }
}
