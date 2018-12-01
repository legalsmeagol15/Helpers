using Parsing.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using DataStructures;

namespace Parsing.Dependency
{
    public enum ErrorState
    {
        NONE = 0
    }

    public enum TypeGuarantee
    {        
        NUMBER = 1,
        MATRIX = 2,
        STRING = 4,
        ANY = NUMBER | MATRIX | STRING
    }

    /// <summary>
    /// Variables have a name and cache the values of their contents.  They participate a dependency system associated with their Context.    
    /// </summary>
    [Serializable]
    public class Variable 
    {
        [NonSerialized]
        public static readonly IEvaluateable Null = new Number(0m);

        //[NonSerialized]
        //internal readonly object ChangeLock = new object();

        [NonSerialized]
        internal readonly object ValueLock = new object();

        public Context Parent { get; internal set; }

        /// <summary>The object that has a reference to this variable.</summary>
        internal readonly Context Context;

        internal Variable(Context parentCtxt) { this.Parent = parentCtxt; }

        public ErrorState Error { get; internal set; } = ErrorState.NONE;

        public TypeGuarantee Type { get; private set; } = TypeGuarantee.ANY;


        #region Variable contents members

        private IEvaluateable _Contents;
        public string Contents
        {
            get => _Contents.ToString();
            set => SetContents(value);
        }

        public void SetNull()
        {
            throw new NotImplementedException();
        }


        public IEvaluateable SetContents(string str)
        {
            try
            {
                IEvaluateable exp = Expression.FromStringInternal(str, this.Parent, Function.Factory.StandardFactory);
                return SetContents(exp);
            }
            catch (SyntaxException synEx)
            {
                throw new NotImplementedException("Must remove all new references.");
            }
        }



        internal IEvaluateable SetContents(IEvaluateable iev)
        {
            lock (this.ValueLock)
            {
                foreach (Variable v in GetTermsOf(iev))
                {
                    if (Monitor.IsEntered(v.ValueLock))
                        throw new InvalidOperationException("This thread cannot have any locks on the new evaluateable.");
                    if (v.ListensTo(this))
                        throw new CircularDependencyException(iev, v, this);
                }
                foreach (Variable v in GetTermsOf(iev))
                    Monitor.Enter(v.ValueLock);
                IEnumerable<Variable> newSources = GetTermsOf(iev);
                foreach (Variable oldSrc in this.InternalListeners.Except(newSources))
                {
                    lock (oldSrc.ValueLock)
                        if (!oldSrc.InternalListeners.Remove(this))
                            throw new Exception("Possible dependency corruption."); // TODO:  clean this when validated.
                }
                foreach (Variable v in GetTermsOf(iev))
                    lock (v.ValueLock)
                        v.InternalListeners.Add(this);
                IEvaluateable oldContents = this._Contents;
                this._Contents = iev;
                OnContentsChanged(oldContents, this._Contents);
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



        #region Variable dependency members


        /// <summary>
        /// This MUST be a set.  Otherwise, we'll end up with a variable appearing multiple times in the list.
        /// </summary>
        internal ISet<Variable> InternalListeners = new HashSet<Variable>();

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
            lock (source.ValueLock)
            {
                if (this.Equals(source)) return true;
                return source.InternalListeners.Any(listener => listener.ListensTo(this));
            }
        }
        
        #endregion


        
        
                


        #region Variable value members
        

        public virtual IEvaluateable Evaluate() => Value;
        public IEvaluateable Value { get; private set; } = Null;

        private void OnValueChanged(IDictionary<Variable, ChangedEventArgs<Variable, IEvaluateable>> changes)
        {
            foreach (ChangedEventArgs<Variable, IEvaluateable> change in changes.Values) OnValueChanged(change);
        }
        private void OnValueChanged(ChangedEventArgs<Variable, IEvaluateable> e) => ValueChanged?.Invoke(this, e);
        public event ValueChangedHandler ValueChanged;
        public delegate void ValueChangedHandler(object sender, ChangedEventArgs<Variable, IEvaluateable> e);        
        
        public IEvaluateable UpdateValue(out IDictionary<Variable, ChangedEventArgs<Variable,  IEvaluateable>> changed)
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
            lock (ValueLock)
                return Value;

            void UpdateConcurrent(Variable v, ISet<Variable> next)
            {
                lock (v.ValueLock)
                {
                    // If another var has made this var unready to update, skip it.
                    if (v.UnresolvedInbound != 0) return;

                    if (v.Error != ErrorState.NONE) return;

                    // Signal every listener that it's not ready for update yet.
                    foreach (Variable listener in v.InternalListeners)
                        lock (listener.ValueLock)
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
                        lock (listener.ValueLock)
                            if (--listener.UnresolvedInbound == 0)
                                lock (next)
                                    next.Add(listener);
                }
            }
        }

        private int UnresolvedInbound = 0;

        


        #endregion




        public bool Release()
        {
            if (InternalListeners.Any()) return false;
            if (Parent == null || Context == null) return true;
            throw new NotImplementedException();          
            
        }

        public override string ToString() => Value.ToString();


        

    }

}
