using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing.Contexts
{
    public abstract class SoftContext : IContext
    {
        private readonly Dictionary<string, WeakReference<Variable>> _WeakVariables 
            = new Dictionary<string, WeakReference<Variable>>();
        private readonly Dictionary<string, WeakReference<IContext>> _WeakContexts 
            = new Dictionary<string, WeakReference<IContext>>();

        /// <summary>
        /// A Variable's deletion status can be changed (an IContext's cannot).
        /// </summary>
        private readonly HashSet<Variable> _HardVariables = new HashSet<Variable>();

        private readonly HashSet<IContext> _HardContexts = new HashSet<IContext>();
        

        Variable IContext.this[string name] => throw new NotImplementedException();

        public string Name { get; private set; }

        public IContext Parent { get; private set; }

        Expression.DeletionStatus IContext.DeletionStatus { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        IEnumerable<Variable> IContext.GetVariables
        {
            get
            {
                foreach (WeakReference<Variable> wr in _WeakVariables.Values)
                    if (wr.TryGetTarget(out Variable v) && v.DeletionStatus != Expression.DeletionStatus.DELETED)
                        yield return v;
            }
        }

        IEnumerable<IContext> IContext.GetContexts
        {
            get
            {
                foreach (WeakReference<IContext> wr in _WeakContexts.Values)
                    if (wr.TryGetTarget(out IContext c) && c.DeletionStatus != Expression.DeletionStatus.DELETED)
                        yield return c;
            }
        }

        bool IContext.TryAdd(string name, out Variable variable)
        {
            if (_WeakVariables.TryGetValue(name, out WeakReference<Variable> wr) && wr.TryGetTarget(out variable) && variable.DeletionStatus != Expression.DeletionStatus.DELETED) return false;
            if (!TryCreateVariable(name, out variable)) return false;
            if (variable.DeletionStatus == Expression.DeletionStatus.DELETED) return false;
            wr = new WeakReference<Variable>(variable);
            _WeakVariables[name] = wr;
            if (variable.DeletionStatus == Expression.DeletionStatus.NO_DELETION)
                _HardVariables.Add(variable);
            variable.DeletionStatusChanged += OnDeletionStatusChanged;
            return true;
        }
        

        protected abstract bool TryCreateVariable(string name, out Variable v);

        bool IContext.TryAdd(string name, out IContext context)
        {
            if (_WeakContexts.TryGetValue(name, out WeakReference<IContext> wr)
                && wr.TryGetTarget(out context) 
                && context.DeletionStatus != Expression.DeletionStatus.DELETED) return false;
            if (!TryCreateSubcontext(name, out context)) return false;
            if (context.DeletionStatus == Expression.DeletionStatus.DELETED) return false;
            wr = new WeakReference<IContext>(context);            
            _WeakContexts[name] = wr;
            if (context.DeletionStatus == Expression.DeletionStatus.NO_DELETION)
                _HardContexts.Add(context);
            return true;
        }
        protected abstract bool TryCreateSubcontext(string name, out IContext subcontext);

        bool IContext.TryCreateFunction(string token, out Function f) { f = null; return false; }

        public bool TryDelete(Variable variable)
        {
            // The given variable might not be the variable linked to the name according to this context.
            string name = variable.Name;
            if (!TryGet(name, out Variable existingVar)) return false;
            if (!ReferenceEquals(variable, existingVar)) return false;
            if (variable.DeletionStatus != Expression.DeletionStatus.ALLOW_DELETION) return false;
            _HardVariables.Remove(variable);
            variable.DeletionStatus = Expression.DeletionStatus.DELETED;
            return _WeakVariables.Remove(name);
        }
        bool IContext.TryDelete(Variable variable) => TryDelete(variable);

        private bool TryDelete(IContext context)
        {
            // The given context might not be the context linked to the name according to this context.
            string name = context.Name;
            if (!TryGet(name, out IContext existingContext)) return false;
            if (!ReferenceEquals(context, existingContext)) return false;
            if (existingContext.DeletionStatus != Expression.DeletionStatus.ALLOW_DELETION) return false;
            _HardContexts.Remove(context);
            context.DeletionStatus = Expression.DeletionStatus.DELETED;
            return _WeakContexts.Remove(name);
        }
        bool IContext.TryDelete(IContext context) => TryDelete(context);

        private bool TryGet(string name, out IContext ctxt)
        {
            if (_WeakContexts.TryGetValue(name, out WeakReference<IContext> wr)
                && wr.TryGetTarget(out ctxt) 
                && ctxt.DeletionStatus != Expression.DeletionStatus.DELETED)
                return true;
            if (Parent != null)
                return Parent.TryGet(name, out ctxt);
            ctxt = null;
            return false;
        }
        bool IContext.TryGet(string name, out IContext ctxt) => TryGet(name, out ctxt);

        private bool TryGet(string name, out Variable v)
        {
            if (_WeakVariables.TryGetValue(name, out WeakReference<Variable> wr)
                && wr.TryGetTarget(out v) 
                && v.DeletionStatus != Expression.DeletionStatus.DELETED)
                return true;
            if (Parent != null)
                return Parent.TryGet(name, out v);
            v = null;
            return false;
        }
        bool IContext.TryGet(string name, out Variable v) => TryGet(name, out v);
        

        /// <summary>
        /// A variables deletion status can change (an IContext's cannot, at least not meaningfully).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDeletionStatusChanged(object sender, DataStructures.ChangedEventArgs<Variable, Expression.DeletionStatus> e)
        {
            switch (e.After)
            {
                case Expression.DeletionStatus.ALLOW_DELETION:
                    _HardVariables.Remove(e.Object);
                    break;
                case Expression.DeletionStatus.DELETED:
                    TryDelete(e.Object);
                    break;
                case Expression.DeletionStatus.NO_DELETION:
                    _HardVariables.Add(e.Object);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
