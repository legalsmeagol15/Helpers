using Parsing.Functions;
using Parsing.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using DataStructures;

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

        public IContext Context { get; internal set; }




        public Variable(IContext context, string name) { Name = name; Context = context; }




        #region Variable contents members

        private IEvaluateable _Contents;
        public string Contents
        {
            get { lock (ModifyLock) lock (UpdateLock) return _Contents.ToString(); }
            set => SetContents(Expression.FromString(value, Context));
        }

        /// <summary>Does circularity checking.</summary>        
        public IEvaluateable SetContents(Expression exp)
        {
            if (exp.ChangeLock == null && exp.Context != null)
                throw new InvalidOperationException("Cannot set contents to a cancelled expression.");

            if (GetTermsOf(exp.Contents).Any(term => term.ListensTo(this)))
            {
                exp.Cancel();
                throw new CircularDependencyException(exp.Contents, this, null);
            }
            
            // ModifyLock is already locked by the Expression.
            lock (UpdateLock)
            {                
                // Find out which sources are no longer listened to by this variable, and which sources are now listened to.
                IEnumerable<Variable> oldSources = GetTermsOf(_Contents);
                IEnumerable<Variable> newSources = GetTermsOf(exp.Contents);
                IEnumerable<Variable> abandonedSources = oldSources.Except(newSources);
                IEnumerable<Variable> unawareSources = newSources.Except(oldSources);
                
                // Change the contents.
                _Contents = exp.Contents;

                // Delete abandoned variables.
                abandonedSources.Select(src => src.InternalListeners.Remove(this));
                Queue<IContext> orphanContexts = new Queue<IContext>();
                Queue<Variable> orphanVariables = new Queue<Variable>(abandonedSources.Where(src => src.InternalListeners.Count == 0));
                while (orphanVariables.Count > 0)
                {
                    Variable orphan = orphanVariables.Dequeue();                    
                    foreach (Variable src in GetTermsOf(orphan._Contents))
                    {
                        lock (src)
                        {
                            src.InternalListeners.Remove(orphan);
                            orphan.DeletionStatus = Expression.DeletionStatus.DELETED;
                            if (src.InternalListeners.Count > 0 || src.DeletionStatus != Expression.DeletionStatus.ALLOW_DELETION) continue;
                        }                        
                        orphanVariables.Enqueue(src);
                        orphanContexts.Enqueue(src.Context);
                    }                    
                }

                // Delete abandoned contexts
                while (orphanContexts.Count > 0)
                {
                    IContext ctxt = orphanContexts.Dequeue();
                    if (ctxt.Parent != null && ctxt.Parent.TryDelete(ctxt))
                    {                        
                        ctxt.DeletionStatus = Expression.DeletionStatus.DELETED;
                        orphanContexts.Enqueue(ctxt.Parent);
                    }
                }

                // Enroll this variable as a listener of the new sources.                
                foreach (Variable newTerm in newSources)
                    lock (newTerm.UpdateLock)
                        newTerm.InternalListeners.Add(this);                
            }

            exp.Commit();

            Update(out IDictionary<Variable, ChangedEventArgs<Variable, IEvaluateable>> changed);
            foreach (ChangedEventArgs<Variable, IEvaluateable> vArgs in changed.Values)
                vArgs.Object.OnValueChanged(vArgs);

            return Value;
        }
        #endregion



        #region Variable dependency members
        


        internal ISet<Variable> InternalListeners = new HashSet<Variable>();

        [System.Diagnostics.DebuggerStepThrough()]
        public static IEnumerable<Variable> GetTermsOf(IEvaluateable evaluateable)
        {
            switch (evaluateable)
            {
                case Reference r:
                    if (r.Head is Variable vHead) yield return vHead;
                    yield break;
                case Clause c:
                    if (c.Terms == null) yield break;
                    foreach (Variable term in c.Terms) yield return term;
                    yield break;
                case Variable v:
                    throw new ArgumentException("The terms of a variable would be a variable.");
            }
        }
        

        public bool ListensTo(Variable source)
        {
            lock (ModifyLock)
            {
                if (this.Equals(source)) return true;
                return source.InternalListeners.Any(listener => listener.ListensTo(this));
            }
        }



        #endregion



        #region Variable name members

        private string _Name;
        public string Name { get => _Name; private set { string oldName = _Name; _Name = value; OnNameChanged(oldName, _Name); } }
        private void OnNameChanged(string oldName, string newName)
        {
            if (oldName != newName)
                NameChanged?.Invoke(this, new ChangedEventArgs<Variable, string>(this, oldName, newName));
        }
        public delegate void NameChangedHandler(object sender, ChangedEventArgs<Variable, string> e);
        public event NameChangedHandler NameChanged;

        #endregion




        #region Variable deletion status members

        private Expression.DeletionStatus _DeletionStatus = Expression.DeletionStatus.ALLOW_DELETION;
        public Expression.DeletionStatus DeletionStatus
        {
            get => _DeletionStatus;
            internal set
            {
                Expression.DeletionStatus oldStatus = _DeletionStatus;
                _DeletionStatus = value;
                OnDeletionStatusChanged(oldStatus, _DeletionStatus);
            }
        }

        private void OnDeletionStatusChanged(Expression.DeletionStatus oldStatus, Expression.DeletionStatus newStatus)
        {
            if (oldStatus != newStatus)
                DeletionStatusChanged?.Invoke(this, new ChangedEventArgs<Variable, Expression.DeletionStatus>(this, oldStatus, newStatus));
        }
        public delegate void DeletionStatusChangedHandler(object sender, ChangedEventArgs<Variable, Expression.DeletionStatus> e);
        public event DeletionStatusChangedHandler DeletionStatusChanged;

        #endregion

        


        #region Variable value members

        [field: NonSerialized]
        internal readonly object UpdateLock = new object();

        public IEvaluateable Evaluate() => Value;
        public IEvaluateable Value = Null;

        private void OnValueChanged(ChangedEventArgs<Variable, IEvaluateable> e) => ValueChanged?.Invoke(this, e);
        public event ValueChangedHandler ValueChanged;
        public delegate void ValueChangedHandler(object sender, ChangedEventArgs<Variable, IEvaluateable> e);        
        
        public IEvaluateable Update(out IDictionary<Variable, ChangedEventArgs<Variable,  IEvaluateable>> changed)
        {
            Dictionary<Variable, ChangedEventArgs<Variable, IEvaluateable>> changedVars 
                = new Dictionary<Variable, ChangedEventArgs<Variable, IEvaluateable>>();
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
                    foreach (Variable listener in v.InternalListeners)
                        lock (listener.UpdateLock)
                            listener.UnresolvedInbound++;

                    // Update this var, but only if the new value would change.
                    IEvaluateable oldValue = v.Value, newValue = v._Contents.Evaluate();
                    if ((oldValue == null) ? (newValue != null) : !oldValue.Equals(newValue))
                    {
                        v.Value = newValue;
                        lock (changedVars)
                        {
                            if (changedVars.TryGetValue(v, out ChangedEventArgs < Variable, IEvaluateable > arg))
                                changedVars[v] = new ChangedEventArgs<Variable, IEvaluateable>(v, arg.Before, newValue);
                            else
                                changedVars[v] = new ChangedEventArgs<Variable, IEvaluateable>(v, oldValue, newValue);
                        }
                    }

                    // Signal to each listener that this var no longer prevents it from updating.
                    foreach (Variable listener in v.InternalListeners)
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


        public static Variable Declare(IContext ctxt, string name, string contents = "")
        {
            Expression exp = Expression.FromString(name, ctxt);
            Reference r = exp.Contents as Reference;
            if (r == null)
            {
                exp.Cancel();
                throw new InvalidOperationException("Only path contexts and variables may be declared.");
            }
            if (r.Variable == null)
            {
                exp.Cancel();
                throw new InvalidOperationException("A variable name was not declared.");
            }
            if (!exp.AddedVariables.Contains(r.Variable))
            {
                exp.Cancel();
                throw new DuplicateVariableException(r.Variable.Name, ctxt.Name);
            }
            exp.Commit();
            if (contents != "")
                r.Variable.Contents = contents;
            r.Variable.DeletionStatus = Expression.DeletionStatus.ALLOW_DELETION;            
            return r.Variable;
        }

    }

}
