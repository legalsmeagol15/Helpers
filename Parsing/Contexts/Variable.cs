using Parsing.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        public static readonly IEvaluateable N = new Clause("", "", new IEvaluateable[] { Number.Zero });
        public static readonly Number Null = new Number(0m);
        public bool AllowDeletion { get; set; } = true;

        [field: NonSerialized]
        internal static readonly object ModifyLock = new object();

        [field: NonSerialized]
        internal readonly object UpdateLock = new object();

        public Action UpdateCallback;

        internal ISet<Variable> Listeners = new HashSet<Variable>();

        public IEvaluateable Evaluate() => Value;

        public IEvaluateable Value = Null;

        private IEvaluateable _Contents;
        public string Contents
        {
            get => GetContents.ToString();
            set => SetContents(Expression.FromString(value, Context));
        }
        public IEvaluateable GetContents { get { lock (ModifyLock)lock (UpdateLock) return _Contents; } }

        /// <summary>Does circularity checking.</summary>        
        public void SetContents(Expression exp)
        {
            if (GetTermsOf(exp.Contents).Any(term => term.ListensTo(this)))
            {
                exp.Cancel();
                throw new CircularDependencyException(exp.Contents, this, null);
            }
            
            lock (ModifyLock)
            {
                lock (UpdateLock)
                {
                    // Delete this variable as a listener of the old sources
                    List<Variable> oldSources = GetTermsOf(_Contents).ToList();
                    foreach (Variable oldSource in oldSources)
                        oldSource.Listeners.Remove(this);

                    // Change the contents.
                    _Contents = exp.Contents;
                    AllowDeletion = (_Contents == null || _Contents is Null);

                    // Enroll this variable as a listener of the new sources.
                    HashSet<Variable> newSources = new HashSet<Variable>(GetTermsOf(_Contents));
                    foreach (Variable newTerm in newSources)
                        lock (newTerm)
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
            }
            exp.Commit();

            Update(out _);           
        }

        // Identifies a component of orphans which will allow themselves to be deleted.  This occurs in the situation of a variable 
        // like a line's length, which listens to x0,y0 and x1,y1.  If some external variable A listens to length, and some variable 
        // B listens to variable x0, then removing B should not clear out anything; removing A should clear out length, y0, x1, and 
        // y1; and removing both A and B should clear out all the variables of the line (length, x0, y0, x1, y1).  A variable that can 
        // be cleared out is called an "orphan".
        internal static bool IdentifyOrphans(Variable focus, List<Variable> orphans, HashSet<Variable> visited)
        {
            // Finding a listener which will NOT allow delete means that anything that variable listeners to cannot be deleted
            // either.
            if (!focus.AllowDeletion)
                return false;
            orphans.Add(focus);
            foreach (Variable listener in focus.Listeners)
                if (visited.Add(listener))
                    if (!IdentifyOrphans(listener, orphans, visited))
                        return false;
            return true;
        }


        public Context Context;


        public string Name;


        public Variable(Context context, string name) { Name = name; Context = context; }
        

        #region Variable dependency members

        [System.Diagnostics.DebuggerStepThrough()]
        public static IEnumerable<Variable> GetTermsOf(IEvaluateable evaluateable)
        {
            switch (evaluateable)
            {
                case Relation r: yield return r.Variable; yield break;
                case Clause c:
                    if (c.Terms == null) yield break;
                    foreach (Variable term in c.Terms) yield return term; yield break;
                case Variable v: yield return v; yield break;
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
                //if (source.Listeners.Contains(this)) return true;
                return source.Listeners.Any(listener => listener.ListensTo(this));
            }            
        }
        
        

        #endregion



        #region Variable value members


        public IEvaluateable Update(out ISet<Variable> changed)
        {
            HashSet<Variable> changedVars = new HashSet<Variable>();
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
            lock (this)
                return Value;

            void UpdateConcurrent(Variable v, ISet<Variable> next)
            {
                lock (v.UpdateLock)
                {
                    // If another var has made this var unready to update, skip it.
                    if (v.Inbound != 0) return;

                    // Signal every listener that it's not ready for update yet.
                    foreach (Variable listener in v.Listeners)
                        lock (listener.UpdateLock)
                            listener.Inbound++;

                    // Update this var, but only if the new value would change.
                    IEvaluateable oldValue = v.Value, newValue = v._Contents.Evaluate();
                    if ((oldValue == null) ? (newValue != null) : !oldValue.Equals(newValue))
                    {
                        v.Value = newValue;
                        lock (changedVars)
                            changedVars.Add(v);
                    }

                    // Signal to each listener that this var no longer prevents it from updating.
                    foreach (Variable listener in v.Listeners)
                        lock (listener.UpdateLock)
                            if (--listener.Inbound == 0)
                                lock (next)
                                    next.Add(listener);
                }
            }
        }

        private int Inbound = 0;

        public override string ToString() => Name;



        #endregion

        
    }

}
