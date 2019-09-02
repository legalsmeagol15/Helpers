using DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependency.Variables
{
    public class CollectionVariable<T> :  IContext, IWeakVariable<T>, IList<T>
    {
        private const int DEFAULT_CAPACITY = 16;
        private List<Node> _List;
        private readonly Func<T, IEvaluateable> _ToEval;
        private readonly Func<IEvaluateable, T> _ToClr;

        public CollectionVariable(Func<T, IEvaluateable> toEval, Func<IEvaluateable, T> toClr, int capacity = DEFAULT_CAPACITY)
        {
            this._ToEval = toEval;
            this._ToClr = toClr;
            this._List = new List<Node>(capacity);
        }

        public int Add(T item)
        {

        }

        public bool Remove(T item)
        {

        }

        private class Node
        {
            public WeakReference<Variable> WeakRef;
            public Variable HardRef;
            public T Item;
            public IEvaluateable Contents;
            
        }
    }
}
