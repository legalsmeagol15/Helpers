using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataStructures
{
    public class DynamicHeap<T> : IPriorityQueue<T> where T : IComparable<T>
    {
        // TODO:  validate it all

        private const int DEFAULT_CAPACITY = 16;
        private Node[] _Table;
        private readonly Dictionary<T, Node> _Nodes = new Dictionary<T, Node>();

        public T Dequeue()
        {
            if (Count == 0) throw new InvalidOperationException("This heap is empty.");
            T result = _Table[0].Item;
            _Nodes.Remove(result);
            _Table[0] = _Table[--Count];
            PercolateDown(0);            
            return result;
        }

        public void Enqueue(T item)
        {            
            if (Count >= _Table.Length)
            {
                Node[] newTable = new Node[_Table.Length * 2];
                for (int i = 0; i < Count; i++) newTable[i] = _Table[i];
                _Table = newTable;
            }
            _Nodes.Add(item, _Table[Count] = new Node(item, Count));
            PercolateUp(Count);
            Count++;
        }


        /// <summary>Updates the position in the heap of the given item.</summary>
        /// <returns>Returns true if the heap was changed as a result of this method; false if not.</returns>
        public bool Update(T item)
        {
            Node node = _Nodes[item];
            int oldIdx = node.Index;
            if (oldIdx == PercolateUp(node.Index) && oldIdx == PercolateDown(node.Index)) return false;
            return true;
        }

        #region DynamicHeap contents queries

        public int this[T item]{ get => IndexOf(item);}

        public bool Contains(T item) => _Nodes.ContainsKey(item);

        public int Count { get; private set; }

        public int IndexOf(T item) => _Nodes[item].Index;

        public T Peek()
        {
            if (Count == 0) throw new InvalidOperationException("This heap is empty.");
            return _Table[0].Item;
        }

        #endregion


        #region DynamicHeap percolation members

        private int PercolateUp(int index)
        {
            while (index > 0)
            {
                int parentIdx = (index - 1) / 2;
                if (_Table[parentIdx].Item.CompareTo(_Table[index].Item) < 0) break;
                Swap(parentIdx, index);
                index = parentIdx;
            }
            return index;
        }

        private int PercolateDown(int index)
        {
            int childIdx = (index * 2) + 1;
            while (childIdx < Count)
            {
                if (_Table[index].Item.CompareTo(_Table[childIdx].Item) >= 0
                    && _Table[index].Item.CompareTo(_Table[++childIdx].Item) >= 0) break;
                Swap(index, childIdx);
                index = childIdx;
            }
            return index;
        }

        private void Swap(int indexA, int indexB)
        {
            Node temp = _Table[indexA];
            _Table[indexA] = _Table[indexB];
            _Table[indexB] = temp;
            _Table[indexA].Index = indexB;
            _Table[indexB].Index = indexA;
        }


        private class Node
        {
            public readonly T Item;

            public int Index;
            public Node(T item, int index) { this.Item = item; this.Index = index; }
        }

        #endregion


    }
}
