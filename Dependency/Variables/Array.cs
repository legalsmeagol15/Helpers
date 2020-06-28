using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dependency.Variables
{
    public class Array : Variable, IIndexable
    {
        private readonly Variable[] _Items;
        public IEvaluateable this[int idx] => _Items[idx];

        public Array(int size) {
            this._Items = new Variable[size];
            for (int i = 0; i < size; i++)
                this._Items[i] = new Variable(Null.Instance) { Parent = this };        
        }
        public Array(IEnumerable<IEvaluateable> items) {
            this._Items = items.Select(item => new Variable(item) { Parent = this }).ToArray();
        }

        internal override bool CommitContents(IEvaluateable newContents)
        {
            // You can only replace the contents wholesale if the new contents is a vector 
            // containing items without parents.  Otherwise, you might re-assign the items' 
            // parents from the vector to this array, and that might not be the user's intent.
            if (!(newContents is Vector vec))
                throw new ArgumentException("New contents must be a vector.");
            if (vec.Size != _Items.Length)
                throw new ArgumentException("Invalid vector size (" + vec.Size + ").  Must be " + _Items.Length);

            IEvaluateable hasParent = vec.Inputs.FirstOrDefault(i => i.Value is ISyncUpdater isu && isu.Parent != null);
            if (hasParent != default)
                throw new ArgumentException("New content members may be of type " + nameof(ISyncUpdater) + "(parent cannot be displaced).");

            bool changed = false;
            for (int i = 0; i < _Items.Length; i++)
            {
                // The variable must update, but shouldn't cause redundant updates in this array.
                Variable v = _Items[i];
                IEvaluateable input = vec.Inputs[i];
                if (v.Contents.Equals(input)) continue;

                changed = true;
                v.Parent = null;
                v.Contents = input;
                v.Parent = this;
            }
            if (!changed)
                return false;
            
            return base.CommitContents(newContents);
        }

        bool IIndexable.TryIndex(IEvaluateable ordinal, out IEvaluateable item)
        {
            if (!(ordinal is Number n) || !n.IsInteger) { item = default; return false; }
            item = this[n];
            return !(item is Error);
        }

        internal override IEvaluateable Evaluate()
            => new Vector(_Items.Select(i => i.Value));

    }

    
}
