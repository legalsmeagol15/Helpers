using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing
{
    public abstract partial class Context
    { 
        public class CircularDependencyException : Exception
        {
            public readonly Variable V0, V1;
            IEvaluateable Contents;
            public CircularDependencyException(IEvaluateable contents, Variable v0, Variable v1) : base("A circular dependency exists.") { this.Contents = contents; this.V0 = v0; this.V1 = v1; }
        }


        public class Variable : IEvaluateable
        {


            public readonly string Name;
            public readonly Context Context;

            private IEvaluateable _Contents = null;
            internal IEvaluateable _CachedValue;
            internal ISet<Variable> _Sources;
            internal readonly ISet<Variable> _Listeners;
            
            public IEvaluateable Contents
            {
                get => _Contents;
                set
                {
                    // Lock because dependency state of this context will be changed.
                    lock (Context)
                    {
                        // Step # 0 - Before changing any state, check for circularity:  if a term of the new contents directly or 
                        // indirectly listens to this Variable.
                        if (value is Clause c)
                        {
                            foreach (Variable term in c.Terms) if (term.DependsOnUnsafe(this)) throw new CircularDependencyException(value, this, term);
                        } else if (value is Variable v)
                        {
                            if (v.DependsOnUnsafe(this)) throw new CircularDependencyException(value, v, this);
                        }

                        // Step #1 - for each source, remove this listener as a source.
                        foreach (Variable source in _Sources) source._Listeners.Remove(this);

                        // Step #2 - Set the contents to the new contents.
                        _Contents = value;
                        _Sources.Clear();

                        // Step #3 - if the new contents is a clause, add sources/listeners.
                        if (_Contents is Clause newClause)
                        {
                            foreach (Variable term in newClause.Terms)
                            {
                                term._Listeners.Add(this);
                                this._Sources.Add(term);
                            }
                        }
                        else if (_Contents is Variable newVariable)
                        {
                            newVariable._Listeners.Add(this);
                            this._Sources.Add(newVariable);
                        }

                        // Step #4 - clear the cached values of all listeners to this Variable.
                        Uncache(this);
                        void Uncache(Variable listener)
                        {
                            listener._CachedValue = null;
                            foreach (Variable l in listener._Listeners) Uncache(l);
                        }
                    }

                    // If the variable is nulled and has no listeners, it can be deleted.
                    Context.TryDelete(this);
                }
            }
            
            
            internal Variable(Context context, string name, IEvaluateable contents = null)
            {
                this.Name = name;
                this.Context = context;
                
                this._Sources = new HashSet<Variable>();
                this._Listeners = new HashSet<Variable>();

                this.Contents = contents;
            }

            /// <summary>Returns whether this Variable depends directly on the given source.</summary>
            /// <para/>Note that this method locks on the Context.
            public bool DependsOn(Variable source)
            {
                lock (Context)
                {
                    return DependsOnUnsafe(source);
                }
            }

            /// <summary>This method is "unsafe" because it uses no locking.</summary>
            private bool DependsOnUnsafe(Variable source)
            {
                if (source.Equals(this)) return true;
                if (_Sources.Contains(source)) return true;
                foreach (Variable sub_var in _Sources) if (sub_var.DependsOnUnsafe(source)) return true;
                return false;
            }
            /// <summary>Returns whether this Variable is a source for the given listener.</summary>
            /// <para/>Note that this method locks on the Context.
            public bool DependedOnBy(Variable listener) => listener.DependsOn(this);

            /// <summary>Returns the evaluation of this Variable.  The evaluation will be cached for fast reference next time.</summary>
            public virtual IEvaluateable Evaluate()
            {
                // Locking should not be necessary here.  Even though dependency is being relied upon to evaluate non-cached Variables, 
                // the explicit dependency structure is neither modified nor read.  The dependencies will only exist in the forms of 
                // direct pointers from a function to a variable, and from a variable to an immutable function.
                if (_CachedValue != null) return _CachedValue;
                return _CachedValue = (Contents == null ? Number.Zero : Contents.Evaluate());
            }

            /// <summary>A Variable's hash code is its name's hash code.</summary>
            public override sealed int GetHashCode() => Name.GetHashCode();

            /// <summary>Two Variables are equal only upon reference equality.</summary>
            public override sealed bool Equals(object obj) => ReferenceEquals(this, obj);

            /// <summary>Returns the Variable's name.</summary>
            public override string ToString() => Name;


        }
        
    }
}
