using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtificialIntelligence.ArcConsistency
{

    /// <summary>
    /// An object that tracks the possible values of various variables and attempts to resolve their actual values by applying different constraints.  
    /// <para/>The classic use of a Constraint Resolver is the N-queens problem, which posits a chessboard of size N x N, requires a single queen on each column and on each row, but 
    /// allows now queens to sit diagonal to each other (i.e., no queen would be in jeopardy of capture by any other queen).  In this case, each row is a separate variable with a 
    /// possible value of the queen sitting in each column.  Given one or more queens whose position are already known, the resolver will attempt to determine one of several 
    /// possibilities:  1) the resolver identifies the positions of the queens of all the remaining rows; 2) the resolver applies the constraints until the constraints themselves 
    /// cannot help deduce the positions of the queens with first-order logic; or 3) the resolver finds that the given arrangement of queens will not allow a valid solution to the 
    /// board.
    /// <para/>The N-queens problem will be implemented by adding... etc.  TODO:  show an example of ConstraintResolver in use.    
    /// </summary>
    /// <typeparam name="TVariable"></typeparam>
    /// <typeparam name="TDomain"></typeparam>
    public class ConstraintResolver<TVariable, TDomain> : ICloneable
    {
        //TODO:  validate ConstraintResolver.
        private Dictionary<TVariable, Variable> _Variables = new Dictionary<TVariable, Variable>();
        private HashSet<Relation> _Relations = new HashSet<Relation>();

        private IEnumerable<TDomain> _StandardDomain;

        public bool IsValid { get; private set; }
        public bool IsSolved { get; private set; }
        private bool _NeedsResolve = true;

        public ConstraintResolver(IEnumerable<TDomain> standardDomain)
        {
            _StandardDomain = standardDomain;
        }

        public ConstraintResolver<TVariable, TDomain> Copy()
        {
            ConstraintResolver<TVariable, TDomain> copy = new ConstraintResolver<TVariable, TDomain>(_StandardDomain);
            foreach (Relation rOrig in _Relations)
            {
                foreach (Variable varOrig in rOrig.GetAllVariables())
                {
                    if (!copy._Variables.ContainsKey(varOrig.Tag))
                    {
                        Variable varCopy = new Variable(varOrig.Tag, varOrig.Domain);
                        copy._Variables.Add(varOrig.Tag, varCopy);
                    }
                }                
                copy._Relations.Add(rOrig.Copy(copy._Variables));
            }
            return copy;
        }
        object ICloneable.Clone()
        {
            return Copy();
        }

        /// <summary>
        /// Resolves the constraint resolution problem by applying the constraints to cull the possible domains of the Variables.
        /// </summary>
        /// <returns>If any variables were changed, the returned Variable array will contain references to those variables.  If no progress towards solution could be made, 
        /// the returned array will be empty.  If the constraint resolution problem is insoluable, a null reference is returns.</returns>
        public Variable[] Resolve()
        {
            if (_Relations.Count == 0) return new Variable[0];

            //HashSet<Relation> changed = new HashSet<Relation>();
            HashSet<Variable> result = new HashSet<Variable>();
            Queue<Relation> unresolved = new Queue<Relation>(_Relations);
            HashSet<Relation> onQueue = new HashSet<Relation>(_Relations);
            Relation lastChanged = unresolved.Last(); //The end-of-change marker.
            
            while (unresolved.Count > 0)
            {
                //Pull the next constraint from the queue, and run its enforcement mechanism.
                Relation r = unresolved.Dequeue();
                onQueue.Remove(r);
                IEnumerable<Variable> updated = r.EnforceRelation();

                //QUESTION #1:  What, if anything, should be added to the queue as a result of the enforcement?
                //If any variable has been invalidated, the problem cannot be solved.
                if (updated.Count() > 0)
                {                    

                    //If the variable is now invalid, return null.
                    if (updated.Any((v) => v.Domain.Count == 0)) return null;

                    ///Any relation involving a changed variable should be added to the queue.
                    foreach (Variable changedVar in updated)
                    {
                        result.Add(changedVar);

                        foreach (Relation affectedRel in changedVar.Relations)
                            if (!ReferenceEquals(affectedRel, r) && affectedRel.IsActive && onQueue.Add(affectedRel))
                                unresolved.Enqueue(affectedRel);
                    }
                }
                

                //Finally, if this relation is still active, throw it on the queue last again.
                if (r.IsActive && onQueue.Add(r)) unresolved.Enqueue(r);

                //QUESTION #2:  Have we gone thru a full set of constraints without any changes?  If so, time to quit.
                ///If there was a change, set the end-of-change marker to the current last.  Otherwise, if we're at the end-of-change 
                ///marker, then it's time to quit.
                if (updated.Count() > 0) lastChanged = unresolved.Last();
                else if (ReferenceEquals(r, lastChanged)) break;
            }

            return result.ToArray();
        }


        #region Variable and Relation content members

        /// <summary>
        /// 
        /// </summary>
        /// <param name="label"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        public bool Add(TVariable label, Func<TDomain, bool> rule)
        {
            Variable v;
            if (!_Variables.TryGetValue(label, out v))
            {
                v = new Variable(label, _StandardDomain);
                _Variables.Add(label, v);
            }
            UnaryRelation ur = new UnaryRelation(v, rule);
            return _Relations.Add(ur);
        }

        public bool Add(TVariable labelA, TVariable labelB, Func<TDomain, TDomain, bool> rule)
        {            
            Variable varA, varB;
            if (!_Variables.TryGetValue(labelA, out varA))
            {
                varA = new Variable(labelA, _StandardDomain);
                _Variables.Add(labelA, varA);
            }
            if (!_Variables.TryGetValue(labelB, out varB))
            {
                varB = new Variable(labelB, _StandardDomain);
                _Variables.Add(labelB, varB);
            }
            BinaryRelation br = new BinaryRelation(varA, varB, rule);
            return _Relations.Add(br);
        }
        
        public bool Add(TVariable labelA, TVariable labelB, TVariable labelC, Func<TDomain, TDomain, TDomain, bool> rule)
        {
            Variable varA, varB, varC;
            if (!_Variables.TryGetValue(labelA, out varA))
            {
                varA = new Variable(labelA, _StandardDomain);
                _Variables.Add(labelA, varA);
            }
            if (!_Variables.TryGetValue(labelB, out varB))
            {
                varB = new Variable(labelB, _StandardDomain);
                _Variables.Add(labelB, varB);
            }
            if (!_Variables.TryGetValue(labelC, out varC))
            {
                varC = new Variable(labelC, _StandardDomain);
                _Variables.Add(labelC, varC);
            }
            TernaryRelation tr = new TernaryRelation(varA, varB, varC, rule);
            return _Relations.Add(tr);
        }
        
        

        //public enum Resolution
        //{
        //    INVALID = -1,
        //    UNSOLVED = 0,
        //    PARTIALLY_SOLVED = 1,
        //    SOLVED = 2
        //}
        
        
        /// <summary>
        /// Represents a variable tagged by a unique identifier, whose actual value may be any value within the variable's domain.
        /// </summary>
        public class Variable 
        {
            /// <summary>
            /// The unique identifier associated with this variable.
            /// </summary>
            public TVariable Tag { get; }

            /// <summary>
            /// The set of labels which represent the known possible domain labels of this variable.
            /// </summary>
            public HashSet<TDomain> Domain { get; private set; }


            internal HashSet<Relation> Relations { get; private set; }

            /// <summary>
            /// Returns whether the variable still has a valid domain.  The variable has an invalid domain if it has no more 
            /// hypothetical values it could assume.
            /// </summary>
            public bool IsValid { get { return Domain.Count > 0; } }           

            /// <summary>
            /// Creates a new variable with the given label, and sets up the initial domain and hypothesis sets from the given domain 
            /// IEnumerable.
            /// </summary>
            public Variable(TVariable label, IEnumerable<TDomain> domain)
            {
                Tag = label;
                Domain = new HashSet<TDomain>(domain);
                Relations = new HashSet<Relation>();            
            }

            
            /// <summary>
            /// Returns true if the variables' tags are equal.  Does not check domains.
            /// </summary>
            public override bool Equals(object obj)
            {
                return obj is Variable && ((Variable)obj).Tag.Equals(Tag);
            }
            /// <summary>
            /// Returns the hash code of the tag.
            /// </summary>            
            public override int GetHashCode()
            {
                return Tag.GetHashCode();
            }

            

            ///// <summary>
            ///// Adds the given domain value to this variable's domain and hypothesis sets.
            ///// </summary>
            ///// <returns>Returns this variable.</returns>
            //public static Variable operator +(Variable var, TDomain domain)
            //{
            //    var.Domain.Add(domain);
            //    return var;
            //}
            ///// <summary>
            ///// Removes the given domain value from this variable's domain and hypothesis sets.  If the given domain value does not 
            ///// exist in the variable's domain set, throws an exception.
            ///// </summary>
            ///// <returns>Returns this variable.</returns>            
            //public static Variable operator -(Variable var, TDomain domain)
            //{
            //    var.Domain.Remove(domain);                    
            //    return var;
            //}
        }

        #endregion



        #region Relations

        /// <summary>
        /// An abstract class embodying the essence of a relation object:  that it relates one or more variables; that relations 
        /// dealing with the same variables in the same order will be equal (if the relations are of the same type); and that 
        /// relations dealing with the same variables hash identically.  Classes which inherit from Relation should be immutable 
        /// once they are created.
        /// <para/>Any relation sub-class must implement the Enforce() method, which returns true or false based on whether 
        /// enforcing the relation modifies the domain of one or more variables.
        /// <para/>Additionally, any relation sub-class must implement the Copy() method, which uses the input dictionary 
        /// object to find the reference for the different variable labels.  This will allow the user of an arc consistency method 
        /// to copy the variables before the method makes any changes to the variable domains.
        /// <para/>Note for inheriting classes:  hashing for relations is defined by the variables that are managed by the relation 
        /// object.  The GetHashCode() method is defined and sealed in this class.
        /// </summary>
        public abstract class Relation
        {
            /// <summary>
            /// A cached value storing the hash code.
            /// </summary>
            private int _HashCode = 0;

            /// <summary>
            /// A cached value indicating whether this relation is still active.
            /// </summary>
            internal bool IsActive = true;

            /// <summary>
            /// Creates a new relation object referencing the given variables.  The object's hash code will be set by summing the 
            /// hash codes of the tags of the given variables.
            /// </summary>
            /// <param name="variables"></param>
            protected Relation(IEnumerable<Variable> variables)
            {
                foreach (Variable var in variables)
                {
                    unchecked
                    {
                        _HashCode += var.Tag.GetHashCode();
                    }
                }
                _HashCode = Math.Abs(_HashCode);
            }

            protected virtual bool IsUnresolved()
            {
                return GetAllVariables().All((v) => v.Domain.Count > 1);
            }
            

            internal IEnumerable<Variable> EnforceRelation()
            {
                IEnumerable<Variable> result = Enforce();
                IsActive = IsUnresolved();
                return result;
            }

            /// <summary>
            /// Enforces the relation by culling hypothetical domain labels from the variables' domains.  If changes to a domain 
            /// are made, this method must return true.  Otherwise, it must return false.
            /// </summary>
            /// <returns></returns>
            public abstract IEnumerable<Variable> Enforce();

            protected internal abstract IEnumerable<Variable> GetAllVariables();


            /// <summary>
            /// The hashcode returned is the sum of the hash codes of the tags of the variables related to this relation object.  
            /// This means that all relations dealing with the same variables will hash identically, even if Equals() is not true 
            /// because the variables are in different orders.
            /// </summary>            
            public override sealed int GetHashCode()
            {
                return _HashCode;
            }

            /// <summary>
            /// Returns whether two relation objects are identical (i.e., they reference the same variables and enforce the same 
            /// rule, not simply reference-equal).  
            /// </summary>
            public override abstract bool Equals(object obj);


            public abstract Relation Copy(IDictionary<TVariable, Variable> dictionary);

        }

#pragma warning disable CS0659



        internal sealed class UnaryRelation : Relation
        {
            public readonly Variable Variable;
            public readonly Func<TDomain, bool> Rule;

            public UnaryRelation(Variable variable, Func<TDomain, bool> rule)
                : base(new Variable[] { variable })
            {
                Variable = variable;
                Rule = rule;
            }

            protected override bool IsUnresolved()
            {
                return Variable.Domain.Count > 1;
            }
            protected internal override IEnumerable<Variable> GetAllVariables()
            {
                return new Variable[] { Variable };
            }

            public override IEnumerable<Variable> Enforce()
            {
                bool changed = false;
                List<TDomain> toRemove = new List<TDomain>();
                while (true)
                {
                    foreach (TDomain d in Variable.Domain) if (!Rule(d)) toRemove.Add(d);
                    if (toRemove.Count == 0) break;
                    foreach (TDomain d in toRemove) Variable.Domain.Remove(d);
                    changed = true;
                    toRemove.Clear();
                }
                return changed ? new Variable[] { Variable } : null;
            }

            public override bool Equals(object obj)
            {
                return obj is UnaryRelation && ((UnaryRelation)obj).Variable.Equals(Variable);
            }


            public override Relation Copy(IDictionary<TVariable, Variable> dictionary)
            {
                return new UnaryRelation(dictionary[Variable.Tag], Rule);                
            }

        }

        internal sealed class BinaryRelation : Relation
        {
            public readonly Variable VariableA, VariableB;
            public readonly Func<TDomain, TDomain, bool> Rule;

            public BinaryRelation(Variable variableA, Variable variableB, Func<TDomain, TDomain, bool> rule)
                : base(new Variable[] { variableA, variableB })
            {
                VariableA = variableA;
                VariableB = variableB;
                Rule = rule;
            }

            protected override bool IsUnresolved()
            {
                return VariableA.Domain.Count > 1 && VariableB.Domain.Count > 1;
            }
            protected internal override IEnumerable<Variable> GetAllVariables()
            {
                return new Variable[] { VariableA, VariableB };
            }

            public override IEnumerable<Variable> Enforce()
            {
                //Start off to see if anything has changed.
                int removedA = VariableA.Domain.RemoveWhere((domA) => !VariableB.Domain.Any((domB) => Rule(domA, domB)));
                int removedB = VariableB.Domain.RemoveWhere((domB) => !VariableA.Domain.Any((domA) => Rule(domA, domB)));
                bool changedA = false, changedB = false;

                while (removedA > 0 || removedB > 0)
                {
                    //Update the status of what has changed.
                    changedA |= (removedA > 0);
                    changedB |= (removedB > 0);

                    //Keep removing until there's nothing to remove.
                    removedA = VariableA.Domain.RemoveWhere((domA) => !VariableB.Domain.Any((domB) => Rule(domA, domB)));
                    removedB = VariableB.Domain.RemoveWhere((domB) => !VariableA.Domain.Any((domA) => Rule(domA, domB)));
                }

                //Return a matrix containing the changed variables.
                if (changedA && changedB) return new Variable[] { VariableA, VariableB };
                if (changedA) return new Variable[] { VariableA };
                if (changedB) return new Variable[] { VariableB };
                return null;
            }

            public override bool Equals(object obj)
            {
                if (obj is BinaryRelation)
                {
                    BinaryRelation br = (BinaryRelation)obj;
                    return VariableA.Equals(br.VariableA) && VariableB.Equals(br.VariableB);
                }
                return false;
            }

            public override Relation Copy(IDictionary<TVariable, Variable> dictionary)
            {
                return new BinaryRelation(dictionary[VariableA.Tag], dictionary[VariableB.Tag], Rule);
            }

        }

        internal sealed class TernaryRelation : Relation
        {
            public readonly Variable VariableA, VariableB, VariableC;
            public readonly Func<TDomain, TDomain, TDomain, bool> Rule;

            public TernaryRelation(Variable variableA,  Variable variableB, Variable variableC, Func<TDomain,  TDomain, TDomain, bool> rule) 
                : base(new Variable[] { variableA, variableB, variableC })
            {
                VariableA = variableA;
                VariableB = variableB;
                VariableC = variableC;
                Rule = rule;
            }
            protected override bool IsUnresolved()
            {
                return VariableA.Domain.Count > 1 && VariableB.Domain.Count > 1 && VariableC.Domain.Count > 1;
            }
            protected internal override IEnumerable<Variable> GetAllVariables()
            {
                return new Variable[] { VariableA, VariableB, VariableC };
            }

            public override IEnumerable<Variable> Enforce()
            {
                throw new NotImplementedException();
            }

            public override bool Equals(object obj)
            {
                if (obj is TernaryRelation)
                {
                    TernaryRelation tr = (TernaryRelation)obj;
                    return VariableA.Equals(tr.VariableA) && VariableB.Equals(tr.VariableB) && VariableC.Equals(tr.VariableC);
                }
                return false;
            }

            public override Relation Copy(IDictionary<TVariable, Variable> dictionary)
            {
                return new TernaryRelation(dictionary[VariableA.Tag], dictionary[VariableB.Tag], dictionary[VariableC.Tag], Rule);
            }
        }

        internal sealed class GlobalRelation : Relation
        {
            public readonly Variable[] Variables;
            public readonly Func<IList<TDomain>, bool> Rule;

            public GlobalRelation(IList<Variable> variables, Func<IList<TDomain>, bool> rule)
                : base(variables)
            {
                Variables = variables.ToArray();
                Rule = rule;
            }

            protected internal override IEnumerable<Variable> GetAllVariables()
            {
                return Variables;
            }

            public override IEnumerable<Variable> Enforce()
            {
                //Get all the domains into lists that can be indexed.
                List<TDomain>[] domainLists = new List<TDomain>[Variables.Length];
                int varLength = Variables.Length;
                for (int i = 0; i < varLength; i++)                
                    domainLists[i] = new List<TDomain>(Variables[i].Domain);                

                
                
                throw new NotImplementedException();
            }

            public override bool Equals(object obj)
            {
                throw new NotImplementedException();
            }

            public override Relation Copy(IDictionary<TVariable, Variable> dictionary)
            {
                throw new NotImplementedException();
            }
        }

#pragma warning restore CS0659

        #endregion



    }

}
