using Parsing.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using DataStructures;

namespace Dependency
{
    public enum ErrorState
    {
        NONE = 0, SYNTAX_ERROR = 1, CIRCULAR_REFERENCE = 2, TYPE_ERROR = 4
    }

    public enum TypeGuarantee
    {
        NUMBER = 1,
        MATRIX = 2,
        STRING = 4,
        ANY = NUMBER | MATRIX | STRING
    }
    
    
    public sealed class Variable 
    {
        [NonSerialized]
        internal static readonly object DependencyLock = new object();
        [NonSerialized]
        internal readonly object UpdateLock = new object();

        public readonly IContext Parent;

        public ErrorState Error { get; internal set; } = ErrorState.NONE;

        public Variable(string contents, IContext parent = null)
        {
            this.Parent = parent;
            
        }




        #region "Variable contents members"

        private IEvaluateable _Contents = Null.Instance;
        public string Contents
        {
            get => _Contents.ToString();
            set => SetContents(value);
        }

        public IEvaluateable SetContents(string str)
        {
            try
            {
                IEvaluateable exp = Expression.FromStringInternal(str, this.Parent, Function.Factory.StandardFactory);
                return SetContents(exp);
            }
            catch (SyntaxException)
            {
                this.Error |= ErrorState.SYNTAX_ERROR;
            }
            return Value;
        }

        public IEvaluateable SetContents(IEvaluateable iev)
        {
            try
            {
                return SetContentsInternal(iev);
            }
            catch (CircularDependencyException)
            {
                this.Error |= ErrorState.CIRCULAR_REFERENCE;
            }
            return Value;
        }

        private IEvaluateable SetContentsInternal(IEvaluateable iev)
        {
            lock (DependencyLock)
            {
                IEnumerable<Variable> newSources = GetTermsOf(iev);
                foreach (Variable newSrc in newSources)
                    lock (newSrc)
                        if (newSrc.ListensTo(this) && !ReferenceEquals(newSrc, this))
                            throw new CircularDependencyException(iev, newSrc, this);

                IEnumerable<Variable> oldSources = GetTermsOf(_Contents).Except(newSources);
                foreach (Variable oldSrc in oldSources)
                    lock (oldSrc.UpdateLock)
                        if (!oldSrc.RemoveListener(this))
                            throw new Exception("Possible dependency corruption."); // TODO:  clean this when validated.

                foreach (Variable newSrc in newSources)
                    lock (newSrc.UpdateLock)
                        newSrc.AddListener(this);

                IEvaluateable oldContents, newContents;
                lock (UpdateLock)
                {
                    oldContents = this._Contents;
                    newContents = (this._Contents = iev ?? Null.Instance);
                }
                OnContentsChanged(oldContents, newContents);
            }
            IEvaluateable oldValue = this.Value;
            this.Value = this.UpdateValue(out IDictionary<Variable, ChangedEventArgs<Variable, IEvaluateable>> changes);
            OnValueChanged(changes);
            return iev;
        }

        public event ContentsChangedEventHandler ContentsChanged;
        public delegate void ContentsChangedEventHandler(Object sender, ChangedEventArgs<Variable, IEvaluateable> e);
        private void OnContentsChanged(IEvaluateable before, IEvaluateable after)
        {
            if (before.Equals(after)) return;
            ContentsChanged?.Invoke(this, new ChangedEventArgs<Variable, IEvaluateable>(this, before, after));
        }

        #endregion




        #region "Variable dependency members"


        /// <summary>
        /// This MUST be a set.  Otherwise, we'll end up with a variable appearing multiple times in the list.
        /// </summary>       
        private HashSet<Variable> _Listeners = new HashSet<Variable>();
        internal bool AddListener(Variable v) => _Listeners.Add(v);
        internal bool RemoveListener(Variable v)
        {
            if (_Listeners.Remove(v))
            {
                if (_Listeners.Count == 0) Release();
                return true;
            }
            return false;
        }

        [System.Diagnostics.DebuggerStepThrough()]
        internal static IEnumerable<Reference> GetReferencesOf(IEvaluateable evaluateable)
        {
            switch (evaluateable)
            {
                case Reference r:
                    yield return r;
                    break;
                case Clause c:
                    foreach (IEvaluateable iev in c.Inputs)
                        foreach (Reference r in GetReferencesOf(iev)) yield return r;
                    break;
            }
        }

        public static IEnumerable<Variable> GetTermsOf(IEvaluateable evaluateable)
        {
            switch (evaluateable)
            {
                case Reference r:
                    if (r.Variable != null) yield return r.Variable;
                    break;
                case Clause c:
                    foreach (IEvaluateable iev in c.Inputs)
                        foreach (Variable var in GetTermsOf(iev)) yield return var;
                    break;
            }
        }


        public bool ListensTo(Variable source)
        {
            lock (DependencyLock)
            {
                if (this.Equals(source)) return true;
                return source._Listeners.Any(listener => listener.ListensTo(this));
            }
        }
        
        #endregion




        #region "Variable value members"

        public IEvaluateable Value { get; private set; } = Null.Instance;


        public IEvaluateable UpdateValue(out IDictionary<Variable, ChangedEventArgs<Variable, IEvaluateable>> changed)
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
            lock (DependencyLock)
                return Value;

            void UpdateConcurrent(Variable v, ISet<Variable> next)
            {
                lock (v.UpdateLock)
                {
                    // If another var has made this var unready to update, skip it.
                    if (v.UnresolvedInbound != 0) return;

                    if (v.Error != ErrorState.NONE) return;

                    // Signal every listener that it's not ready for update yet.
                    foreach (Variable listener in v._Listeners)
                        lock (listener.UpdateLock)
                            listener.UnresolvedInbound++;

                    // Update this var, but only if the new value would change.
                    IEvaluateable oldValue = v.Value, newValue = v._Contents.Evaluate();
                    if ((oldValue == null) ? (newValue != null) : !oldValue.Equals(newValue))
                    {
                        v.Value = newValue;
                        lock (changedVars)
                        {
                            if (changedVars.TryGetValue(v, out ChangedEventArgs<Variable, IEvaluateable> arg))
                                changedVars[v] = new ChangedEventArgs<Variable, IEvaluateable>(v, arg.Before, newValue);
                            else
                                changedVars[v] = new ChangedEventArgs<Variable, IEvaluateable>(v, oldValue, newValue);
                        }
                    }

                    // Signal to each listener that this var no longer prevents it from updating.
                    foreach (Variable listener in v._Listeners)
                        lock (listener.UpdateLock)
                            if (--listener.UnresolvedInbound == 0)
                                lock (next)
                                    next.Add(listener);
                }
            }
        }

        private int UnresolvedInbound = 0;

        private void OnValueChanged(IDictionary<Variable, ChangedEventArgs<Variable, IEvaluateable>> changes)
        {
            foreach (ChangedEventArgs<Variable, IEvaluateable> change in changes.Values) OnValueChanged(change);
        }
        private void OnValueChanged(ChangedEventArgs<Variable, IEvaluateable> e) => ValueChanged?.Invoke(this, e);
        public event ValueChangedHandler ValueChanged;
        public delegate void ValueChangedHandler(object sender, ChangedEventArgs<Variable, IEvaluateable> e);

        #endregion



        public override string ToString() => Value.ToString();
    }
    
    

    

}
