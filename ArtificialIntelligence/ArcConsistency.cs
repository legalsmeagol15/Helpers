using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtificialIntelligence.ArcConsistency
{
    public class ConstraintResolver<TVariable, TDomain> : ICloneable
    {
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

        public ConstraintResolver<TVariable, TDomain> Clone()
        {
            
        }
        object ICloneable.Clone()
        {
            return Clone();
        }


        #region Variable and Relation content members

        
        public bool Add(TVariable labelA, TVariable labelB, Func<TDomain, TDomain, bool> rule)
        {
            
        }

        internal bool Add(IList<TVariable> labels, Func<IEnumerable<TDomain>, bool> rule)
        {

        }
        
        
        

       

        public enum Resolution
        {
            UNSOLVED = 0,
            INVALID = -1,
            SOLVED = 1
        }
        
        

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


            /// <summary>
            /// Adds the given domain value to this variable's domain and hypothesis sets.
            /// </summary>
            /// <returns>Returns this variable.</returns>
            public static Variable operator +(Variable var, TDomain domain)
            {
                var.Domain.Add(domain);
                return var;
            }
            /// <summary>
            /// Removes the given domain value from this variable's domain and hypothesis sets.  If the given domain value does not 
            /// exist in the variable's domain set, throws an exception.
            /// </summary>
            /// <returns>Returns this variable.</returns>            
            public static Variable operator -(Variable var, TDomain domain)
            {
                var.Domain.Remove(domain);                    
                return var;
            }
        }

        #endregion



        #region Relations

        /// <summary>
        /// An abstract class embodying the essence of a relation object:  that it relates one or more variables; that relations 
        /// dealing with the same variables in the same order will be equal (if the relations are of the same type); and that 
        /// relations dealing with the same variables hash identically.  
        /// <para/>Any relation sub-class must implement the Enforce() 
        /// method, which returns true or false based on whether enforcing the relation modifies the domain of one or more 
        /// variables.
        /// <para/>Additionally, any relation sub-class must implement the Copy() method, which uses the input dictionary 
        /// object to find the reference for the different variable labels.  This will allow the user of an arc consistency method 
        /// to copy the variables before the method makes any changes to the variable domains.
        /// <para/>Note for inheriting classes:  hashing for relations is defined by the variables that are managed by the relation 
        /// object.  The GetHashCode() method is defined and sealed in this class.
        /// </summary>
        public abstract class Relation
        {
            private int _HashCode = 0;

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

            /// <summary>
            /// Enforces the relation by culling hypothetical domain labels from the variables' domains.  If changes to a domain 
            /// are made, this method must return true.  Otherwise, it must return false.
            /// </summary>
            /// <returns></returns>
            public abstract Variable[] Enforce();


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

            public override Variable[] Enforce()
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

            public override Variable[] Enforce()
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

            public override Variable[] Enforce()
            {

                ////Start off to see if anything has changed.
                //int removedA = VariableA.Domain.RemoveWhere((domA) => !VariableB.Domain.Any((domB) => Rule(domA, domB)));
                //int removedB = VariableB.Domain.RemoveWhere((domB) => !VariableA.Domain.Any((domA) => Rule(domA, domB)));
                //bool changedA = false, changedB = false;

                //while (removedA > 0 || removedB > 0)
                //{
                //    //Update the status of what has changed.
                //    changedA |= (removedA > 0);
                //    changedB |= (removedB > 0);

                //    //Keep removing until there's nothing to remove.
                //    removedA = VariableA.Domain.RemoveWhere((domA) => !VariableB.Domain.Any((domB) => Rule(domA, domB)));
                //    removedB = VariableB.Domain.RemoveWhere((domB) => !VariableA.Domain.Any((domA) => Rule(domA, domB)));
                //}

                ////Return a matrix containing the changed variables.
                //if (changedA && changedB) return new Variable[] { VariableA, VariableB };
                //if (changedA) return new Variable[] { VariableA };
                //if (changedB) return new Variable[] { VariableB };
                //return null;
                throw new NotImplementedException();
            }

            public override bool Equals(object obj)
            {
                throw new NotImplementedException();
            }
        }

#pragma warning restore CS0659

        #endregion



    }

}
