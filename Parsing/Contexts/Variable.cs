using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing
{



    /// <summary>
    /// Variables have a name and cache the values of their contents.  They participate a dependency system associated with their Context.
    /// <para/>
    /// A <see cref="Variable"/> is a <see cref="Context"/> that has an associated <see cref="Value"/> and updates <see cref="Listeners"/>.
    /// </summary>
    [Serializable]
    public class Variable : IEvaluateable
    {
        internal static readonly int SINGLE_THREAD_THRESHOLD = 5;
        /// <summary>
        /// Locks must always be ordered from source to listener.
        /// </summary>
        [field: NonSerialized]
        internal static readonly object LockObject = new object();

        internal ISet<Variable> Listeners = new HashSet<Variable>();

        public IEvaluateable Evaluate() => Value;

        public IEvaluateable Value;

        public IEvaluateable Contents;

        public Context Context;


        public string Name;


        public Variable(Context context, string name) { Name = name; Context = context; }

        
        #region Variable value members

        public class ValueChangedEventArgs
        {
            public readonly Variable Variable;
            public readonly object Before;
            public readonly object After;
            public ValueChangedEventArgs(Variable variable, object before, object after) { this.Variable = variable; this.Before = before; this.After = after; }
        }

        public IEvaluateable Update(out IList<IList<Variable>> changes)
        {

            object processLock = new object();

            // Variables must be updated in topological order, and the topo levels will function as groups of tasks which can be executed 
            // safely in parallel.
            lock (LockObject)
            {

                Dictionary<Variable, Graphs.TopologicalSort<Variable>.Node> nodes
                    = Graphs.TopologicalSort<Variable>.GetTopologicalGraph(this, v => v.Listeners);

                HashSet<Graphs.TopologicalSort<Variable>.Node> queue
                    = new HashSet<Graphs.TopologicalSort<Variable>.Node>();

                changes = new List<IList<Variable>>();


                queue.Add(nodes[this]);

                while (true)
                {
                    List<Variable> changedList = new List<Variable>();
                    HashSet<Graphs.TopologicalSort<Variable>.Node> nextQueue
                     = new HashSet<Graphs.TopologicalSort<Variable>.Node>();

                    if (queue.Count > SINGLE_THREAD_THRESHOLD)
                    {
                        List<Task> tasks = new List<Task>();
                        foreach (var focusNode in queue)
                        {
                            Task t = Task.Factory.StartNew(() => UpdateNodeVariable(focusNode, changedList, nextQueue));
                            tasks.Add(t);
                        }
                        Task.WaitAll(tasks.ToArray());
                    }
                    else
                        foreach (var focusNode in queue)
                            UpdateNodeVariable(focusNode, changedList, nextQueue);

                    if (changedList.Count > 0)
                        changes.Add(changedList);
                    if (nextQueue.Count == 0)
                        break;
                    queue = nextQueue;
                }

                return Value;

                void UpdateNodeVariable(Graphs.TopologicalSort<Variable>.Node node, List<Variable> list, HashSet<Graphs.TopologicalSort<Variable>.Node> nextQueue)
                {
                    IEvaluateable oldValue = node.Item.Value, newValue = node.Item.Contents.Evaluate();
                    bool changed = !newValue.Equals(oldValue);
                    if (changed)
                    {
                        node.Item.Value = newValue;
                        lock (processLock)
                        {
                            list.Add(node.Item);
                            foreach (var childNode in node.Children)
                            {
                                nextQueue.Add(childNode);
                                --childNode.Inbound;
                            }
                        }

                    }
                    else
                        foreach (var childNode in node.Children)
                            lock (processLock)
                                --childNode.Inbound;
                }
            }


        }



        #endregion

    }

}
