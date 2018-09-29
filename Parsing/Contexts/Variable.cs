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

        [field: NonSerialized]
        internal static readonly object ModifyLock = new object();

        [field: NonSerialized]
        internal readonly object UpdateLock = new object();

        internal ISet<Variable> Listeners = new HashSet<Variable>();

        public IEvaluateable Evaluate() => Value;

        public IEvaluateable Value = Null;

        private IEvaluateable _Contents;
        public string Contents
        {
            get => GetContents.ToString();
            set => SetContents(Expression.FromString(value, Context, Context.Functions));
        }
        public IEvaluateable GetContents { get { lock (UpdateLock) return _Contents; } }
        /// <summary>
        /// Does circularity checking.
        /// </summary>
        /// <param name="exp"></param>
        public void SetContents(Expression exp)
        {
            foreach (Variable term in GetTermsOf(exp.Contents))
            {
                if (term.ListensTo(this))
                {
                    exp.Cancel();
                    throw new CircularDependencyException(exp.Contents, this, null);
                }
            }
        
            lock (UpdateLock)
            {
                foreach (Variable oldTerm in GetTermsOf(_Contents)) lock(oldTerm) oldTerm.Listeners.Remove(this);                
                _Contents = exp.Contents;
                foreach (Variable newTerm in GetTermsOf(_Contents)) lock (newTerm) newTerm.Listeners.Add(this);
            }
            exp.Commit();

            Update(out _);
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
                    if ((oldValue == null) ? (newValue != null) : !oldValue.Equals(newValue)){
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
