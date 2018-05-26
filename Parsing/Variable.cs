using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing
{
    public partial class DataContext
    {
        #region DataContext Variable dependency management

        private readonly Dictionary<Variable, HashSet<Variable>> Listeners = new Dictionary<Variable, HashSet<Variable>>();
        private readonly Dictionary<Variable, HashSet<Variable>> Sources = new Dictionary<Variable, HashSet<Variable>>();

        private static HashSet<Variable> DetectSources(Variable v)
        {
            HashSet<Variable> result = new HashSet<Variable>();
            DetectSourcesRecursive(v.Contents);
            return result;

            void DetectSourcesRecursive(IEvaluatable exp)
            {
                switch (exp)
                {
                    case Variable var: result.Add(var); break;
                    case Clause c: foreach (IEvaluatable input in c.Inputs) DetectSourcesRecursive(input); break;
                }
            }
        }

        /// <summary>Recursively notifies all a variable's listeners of a change to the contents.</summary>
        internal void NotifyChanged(Variable v)
        {
            v._CachedValue = null;
            lock (this)
            {
                foreach (Variable source in Sources[v]) Listeners[source].Remove(v);
                Sources[v] = DetectSources(v);
                foreach (Variable listener in Listeners[v]) NotifyChanged(listener);
            }
        }

        #endregion



        public sealed class Variable : IEvaluatable
        {
            public readonly string Name;
            public readonly DataContext Context;

            internal IEvaluatable _Contents = null;
            public IEvaluatable Contents
            {
                get => _Contents;
                set
                {
                    _Contents = value;

                    // TODO:  Context is null in testing?
                    Context.NotifyChanged(this);
                    Context.TryDelete(this);
                }
            }
            public HashSet<Variable> Dependents = new HashSet<Variable>();
            internal IEvaluatable _CachedValue;

            //internal ISet<Variable> Listeners { get; private set; } = new HashSet<Variable>();

            internal Variable(DataContext context, string name, IEvaluatable contents = null)
            {
                this.Name = name;
                this.Context = context;
                this._Contents = contents;
            }

            /// <summary>Returns whether this Variable depends directly on the given source.</summary>
            public bool DependsOn(Variable source) => Context.DependencyExists(source, this);
            /// <summary>Returns whether this Variable is a source for the given listener.</summary>
            public bool DependedOnBy(Variable listener) => Context.DependencyExists(listener, this);


            public IEvaluatable Evaluate()
            {
                if (_CachedValue != null) return _CachedValue;
                return _CachedValue = (Contents == null ? Contents : Contents.Evaluate());
            }

            public override int GetHashCode() => Name.GetHashCode();

            public override bool Equals(object obj) => ReferenceEquals(this, obj);

            public override string ToString() => Name;


        }
    }
}
