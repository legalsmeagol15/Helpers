using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsing
{
    
    public  partial class Context
    { 
        public class CircularDependencyException : Exception
        {
            public readonly Variable V0, V1;
            IEvaluateable Contents;
            public CircularDependencyException(IEvaluateable contents, Variable v0, Variable v1) : base("A circular dependency exists.")
            {
                this.Contents = contents; this.V0 = v0; this.V1 = v1;
            }
        }


      

        
        /// <summary>
        /// Variables have a name and cache the values of their contents.  They participate a dependency system associated with their Context.
        /// </summary>
        [Serializable]
        public class Variable : IEvaluateable
        {
            /// <summary>
            /// Write locking must be done ONLY in three conditions:  1) the contents of a Variable are changed; and 2) a new Variable is 
            /// added to the dependency structure; and 3) a Variable is removed out of the dependency structure.  It is possible that there 
            /// exists multiple non-contiguous dependency graphs.  However, these graph components need NOT be contiguous with the extent of 
            /// a Context (or a super-Context or a sub-Context).  Therefore, given how I expect Variables to be used, I have opted not to try 
            /// to track contiguous dependency graphs, but instead just presume that everyone locks the same lock for any change in the 
            /// dependency graph.
            /// </summary>
            private static object DependencyMutex = new object();


            /// <summary>
            /// Read locking will be done every time the contents of a Variable is read.
            /// </summary>
            //private object ContentMutex = new object();

            public readonly string Name;

            /// <summary>Where this variable lives.</summary>
            public readonly IContext Context;
            
            internal IEvaluateable _CachedValue;
            internal ISet<Variable> _Sources;
            internal readonly ISet<Variable> _Listeners;

            private IEvaluateable _Contents = null;
            /// <summary>
            /// The contents of the variable.  This is guaranteed never to be null.  If there are no meaningful contents, the Variable 
            /// will contain Number.Null (which evaluates to 0m) instead.
            /// </summary>
            public virtual IEvaluateable Contents { get => _Contents; set => SetContents(value); }

            /// <summary>
            /// Sets the Contents as indicated.
            /// </summary>            
            /// <exception cref="Context.CircularDependencyException">Thrown when a circular dependency would be introduced.  No state 
            /// will be changed if such an exception is thrown.</exception>
            /// <param name="context">The context wherein content variables will be interpreted.  If omitted, the context of this Variable 
            /// will be used.</param>
            /// <returns>Returns the IEvaluatable contents.</returns>
            public void SetContents(string str, IContext context = null)
            {
                lock (DependencyMutex)
                {                    
                    Contents = Expression.FromString(str, context ?? Context);
                    _CachedValue = (Contents == null) ? Number.Null : Contents.Evaluate();
                }
                
            }

            /// <summary>Sets the Contents as indicated. </summary>    
            /// <exception cref="Context.CircularDependencyException">Thrown when a circular dependency would be introduced.  No state 
            /// will be changed if such an exception is thrown.</exception>
            private void SetContents(IEvaluateable value)
            {
                // Lock because dependency state of this context will be changed.
                lock (DependencyMutex)
                {
                    // Step # 0 - Before changing any state, check for circularity:  if a term of the new contents directly or 
                    // indirectly listens to this Variable.
                    if (value is Clause c)
                    {
                        foreach (Variable term in c.Terms)
                            if (term.DependsOnUnsafe(this))
                                throw new CircularDependencyException(value, this, term);
                    }
                    else if (value is Variable v && v.DependsOnUnsafe(this))
                        throw new CircularDependencyException(value, v, this);

                    // Step #1 - for each source, remove this listener as a source.
                    foreach (Variable source in _Sources) source._Listeners.Remove(this);

                    // Step #2 - Set the contents to the new contents.
                    _Contents = value;

                    // Step #3 - remove all sources that this variable was listening to.
                    _Sources.Clear();

                    // Step #4 - if the new contents is a clause, add sources/listeners.
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

                    // Step #5 - update the cached value for this and all listening variables.
                    CacheUpdate(this);
                    
                }

                
                void CacheUpdate(Variable v) {                    
                    IEvaluateable newValue = (v.Contents == null) ? Number.Null : v.Contents.Evaluate();
                    if (v._CachedValue.Equals(newValue)) return;
                    v._CachedValue = newValue;
                    foreach (Variable listener in v._Listeners) CacheUpdate(listener);
                }
                
            }
            
           
            
            public Variable(IContext context, string name, IEvaluateable contents = null)
            {
                this.Name = name;
                this.Context = context;
                
                this._Sources = new HashSet<Variable>();
                this._Listeners = new HashSet<Variable>();

                this.Contents = contents;
            }

            /// <summary>Returns whether this Variable depends directly on the given source.</summary>
            /// <para/>Note that this method locks on the Context.
            public bool DependsOn(Variable source) { lock (DependencyMutex) { return DependsOnUnsafe(source); } }


            /// <summary>This method is "unsafe" because it uses no locking.</summary>
            private bool DependsOnUnsafe(Variable source)
            {
                if (source.Equals(this)) return true;
                if (_Sources.Contains(source)) return true;
                foreach (Variable sub_var in _Sources) if (sub_var.DependsOnUnsafe(source)) return true;
                return false;
            }


            /// <summary>Returns the evaluation of this Variable.  The evaluation will be cached for fast reference next time.</summary>
            //public virtual IEvaluateable Evaluate() => _CachedValue;
            public IEvaluateable Value { get => _CachedValue; }
            IEvaluateable IEvaluateable.Evaluate() => _CachedValue;

            /// <summary>A Variable's hash code is its name's hash code.</summary>
            public override sealed int GetHashCode() => Name.GetHashCode();

            /// <summary>Two Variables are equal only upon reference equality.</summary>
            public override sealed bool Equals(object obj) => ReferenceEquals(this, obj);

            /// <summary>Returns the Variable's name.</summary>
            public override string ToString() => Name;

            /// <summary>Guaranteed to never fail.  If the value cannot  be boiled down to a double, returns double.NaN.</summary>            
            public double ToDouble() => (Value is Number n) ? (double)n.Value : double.NaN;
            /// <summary>Guaranteed to never fail.  If the value cannot be boiled down to a byte, returns byte.MinValue (which is 0).</summary>            
            public byte ToByte() => (Value is Number n) ? (byte)n.Value : byte.MinValue;
            /// <summary>Guaranteed to never fail.  If the value cannot be boiled down to an int, returns int.MinValue (which is -2,147,483,648).</summary>            
            public int ToInt() => (Value is Number n) ? (int)n.Value : int.MinValue;
            
        }
        
        
    }
}
