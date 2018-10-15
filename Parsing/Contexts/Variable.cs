using Parsing.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Parsing
{
    /// <summary>
    /// Variables have a name and cache the values of their contents.  They participate a dependency system associated with their Context.    
    /// </summary>
    [Serializable]
    public class Variable : IEvaluateable
    {        
        public static readonly Number Null = new Number(0m);

        [field: NonSerialized]
        internal static readonly object ModifyLock = new object();

        public Context Context { get; internal set; }

        public string Name { get; private set; }

        public Expression.DeletionStatus DeletionStatus { get; internal set; } = Expression.DeletionStatus.NO_DELETION;



        public Variable(Context context, string name) { Name = name; Context = context; }




        #region Variable contents members

        private IEvaluateable _Contents;
        public string Contents
        {
            get { lock (ModifyLock) lock (UpdateLock) return _Contents.ToString(); }
            set => SetContents(Expression.FromString(value, Context));
        }

        /// <summary>Does circularity checking.</summary>        
        public string SetContents(Expression exp)
        {
            if (exp.ChangeLock == null && exp.Context != null)
                throw new InvalidOperationException("Cannot set contents to a cancelled expression.");

            if (GetTermsOf(exp.Contents).Any(term => term.ListensTo(this)))
            {
                exp.Cancel();
                throw new CircularDependencyException(exp.Contents, this, null);
            }

            string asString;
            // ModifyLock is already locked by the Expression.
            lock (UpdateLock)
            {
                // Delete this variable as a listener of the old sources
                List<Variable> oldSources = GetTermsOf(_Contents).ToList();
                foreach (Variable oldSource in oldSources)
                    oldSource.Listeners.Remove(this);

                // Change the contents.
                _Contents = exp.Contents;
                asString = _Contents.ToString();
                DeletionStatus = (_Contents == null || _Contents is Null) ? Expression.DeletionStatus.ALLOW_DELETION : Expression.DeletionStatus.NO_DELETION;

                // Enroll this variable as a listener of the new sources.
                HashSet<Variable> newSources = new HashSet<Variable>(GetTermsOf(_Contents));
                foreach (Variable newTerm in newSources)
                    lock (newTerm.UpdateLock)
                        newTerm.Listeners.Add(this);

                // Find out if something is left orphan (nobody listening to it), and try to delete it.  If it can be deleted, try to 
                // delete containing contexts.
                foreach (Variable oldSource in oldSources.Except(newSources))
                {
                    List<Variable> orphanSources = new List<Variable>();
                    if (!IdentifyOrphans(oldSource, orphanSources, new HashSet<Variable>()))
                        continue;
                    foreach (Variable orphan in orphanSources.Where(o => o.Context != null))
                        orphan.Context.TryDelete(orphan);
                }
            }

            exp.Commit();

            Update(out IDictionary<Variable, VariableChangedEventArgs> changed);
            foreach (VariableChangedEventArgs vArgs in changed.Values)
                vArgs.Variable.OnChanged(vArgs);

            return asString;
        }
        #endregion



        #region Variable dependency members

        // Identifies a component of orphans which will allow themselves to be deleted.  This occurs in the situation of a variable 
        // like a line's length, which listens to x0,y0 and x1,y1.  If some external variable A listens to length, and some variable 
        // B listens to variable x0, then removing B should not clear out anything; removing A should clear out length, y0, x1, and 
        // y1; and removing both A and B should clear out all the variables of the line (length, x0, y0, x1, y1).  A variable that can 
        // be cleared out is called an "orphan".
        internal static bool IdentifyOrphans(Variable focus, List<Variable> orphans, HashSet<Variable> visited)
        {
            // Finding a listener which will NOT allow delete means that anything that variable listeners to cannot be deleted
            // either.
            if (focus.DeletionStatus == Expression.DeletionStatus.NO_DELETION)
                return false;
            orphans.Add(focus);
            foreach (Variable listener in focus.Listeners)
                if (visited.Add(listener))
                    if (!IdentifyOrphans(listener, orphans, visited))
                        return false;
            return true;
        }


        internal ISet<Variable> Listeners = new HashSet<Variable>();

        [System.Diagnostics.DebuggerStepThrough()]
        public static IEnumerable<Variable> GetTermsOf(IEvaluateable evaluateable)
        {
            switch (evaluateable)
            {
                case Reference r:
                    if (r.Head is Variable v) yield return v;
                    yield break;
                case Clause c:
                    if (c.Terms == null) yield break;
                    foreach (Variable term in c.Terms) yield return term;
                    yield break;
            }
        }

        public bool IsContext
        {
            get
            {
                lock (ModifyLock)
                    return Context != null && Name.Equals(Context.Name)
                        && Context.TryGet(Name, out Variable v) && ReferenceEquals(this, v)
                        && Context.TryGet(Name, out Context sub_ctxt) && ReferenceEquals(this.Context, sub_ctxt);
            }
        }



        public bool ListensTo(Variable source)
        {
            lock (ModifyLock)
            {
                if (this.Equals(source)) return true;
                return source.Listeners.Any(listener => listener.ListensTo(this));
            }
        }



        #endregion



        #region Variable value members

        [field: NonSerialized]
        internal readonly object UpdateLock = new object();

        public IEvaluateable Evaluate() => Value;

        public IEvaluateable Value = Null;

        private void OnChanged(VariableChangedEventArgs e) => Changed?.Invoke(this, e);
        public event VariableChangedHandler Changed;
        public delegate void VariableChangedHandler(object sender, VariableChangedEventArgs e);
        public class VariableChangedEventArgs : EventArgs
        {
            public readonly Variable Variable;
            public readonly IEvaluateable Before, After;
            public VariableChangedEventArgs(Variable v, IEvaluateable before, IEvaluateable after) { this.Variable = v; this.Before = before; this.After = after; }
        }

        public IEvaluateable Update(out IDictionary<Variable, VariableChangedEventArgs> changed)
        {
            Dictionary<Variable, VariableChangedEventArgs> changedVars = new Dictionary<Variable, VariableChangedEventArgs>();
            HashSet<Variable> ready = new HashSet<Variable>() { this };

            while (ready.Count > 0)
            {
                HashSet<Variable> next = new HashSet<Variable>();
                List<Task> tasks = new List<Task>();
                foreach (Variable v in ready)
                {
                    Task t = Task.Factory.StartNew(() => UpdateConcurrent(v, next));
                    tasks.Add(t);
                }

                Task.WaitAll(tasks.ToArray());
                ready = next;
            }

            changed = changedVars;
            lock (UpdateLock)
                return Value;

            void UpdateConcurrent(Variable v, ISet<Variable> next)
            {
                lock (v.UpdateLock)
                {
                    // If another var has made this var unready to update, skip it.
                    if (v.UnresolvedInbound != 0) return;

                    // Signal every listener that it's not ready for update yet.
                    foreach (Variable listener in v.Listeners)
                        lock (listener.UpdateLock)
                            listener.UnresolvedInbound++;

                    // Update this var, but only if the new value would change.
                    IEvaluateable oldValue = v.Value, newValue = v._Contents.Evaluate();
                    if ((oldValue == null) ? (newValue != null) : !oldValue.Equals(newValue))
                    {
                        v.Value = newValue;
                        lock (changedVars)
                        {
                            if (changedVars.TryGetValue(v, out VariableChangedEventArgs arg))
                                changedVars[v] = new VariableChangedEventArgs(v, arg.Before, newValue);
                            else
                                changedVars[v] = new VariableChangedEventArgs(v, oldValue, newValue);
                        }
                    }

                    // Signal to each listener that this var no longer prevents it from updating.
                    foreach (Variable listener in v.Listeners)
                        lock (listener.UpdateLock)
                            if (--listener.UnresolvedInbound == 0)
                                lock (next)
                                    next.Add(listener);
                }
            }
        }

        private int UnresolvedInbound = 0;






        #endregion


        public override string ToString() => Name;


    }

}
