using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataStructures;

namespace AI
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
    /// <author>Wesley Oates</author>
    /// <date>Jan 12, 2017</date>
    public class ConstraintResolver<TVariable, TDomain> : ICloneable
    {
        //TODO:  validate ConstraintResolver.
        private Dictionary<TVariable, Variable> _Variables = new Dictionary<TVariable, Variable>();
        private HashSet<Relation> _Relations = new HashSet<Relation>();
        private IEnumerable<TDomain> _StandardDomain;
        
        

        public ConstraintResolver(IEnumerable<TDomain> standardDomain)
        {
            _StandardDomain = standardDomain;
        }

        /// <summary>
        /// Copies this constraint resolution problem.  Useful for backtrack searching.
        /// </summary>        
        public ConstraintResolver<TVariable, TDomain> Copy()
        {
            ConstraintResolver<TVariable, TDomain> copy = new ConstraintResolver<TVariable, TDomain>(_StandardDomain);
            foreach (Relation rOrig in _Relations)
                if (rOrig.IsActive) copy._Relations.Add(rOrig.GetCopy(copy));
            return copy;
        }
        object ICloneable.Clone()
        {
            return Copy();
        }

        /// <summary>
        /// Resolves a constraint resolution problem embodied by a set of Relation objects by applying Mackworth's AC-1 algorithm.  This 
        /// algorithm works by cycling through the entire set of relations, and culling each variable's domain of potential values not 
        /// supported by the relation with regard to other variables.  Once the algorithm passes through the entire set of Relation 
        /// objects and no change to any variable's domain is made, the algroithm terminates.
        /// </summary>
        /// <param name="relations">The set of relation objects which describe whether a given domain in variable A is supported by any 
        /// potential value in the domain  of variable B.</param>
        /// <returns>Returns the set of Variable objects which are modified by application of the algorithm.  If the problem is not 
        /// solveable, returns null.</returns>
        private static IEnumerable<Variable> ArcConsistency1(IEnumerable<Relation> relations)
        {
            bool changed = true;
            HashSet<Variable> result = new HashSet<Variable>();
            while (changed)
            {
                changed = false;
                foreach (Relation r in relations)
                {
                    if (!r.IsActive) continue;
                    IEnumerable<Variable> changedVars = r.EnforceRelation();
                    if (changedVars == null) continue;
                    if (changedVars.Count() > 0)
                    {
                        changed = true;
                        foreach (Variable v in changedVars) result.Add(v);
                    }
                }
            }
            return result;
        }
        /// <summary>
        /// Resolve a constraint resolution problem embodied by a set of Relation objects by applying Mackworth's AC-3 algorithm.  This 
        /// algorithm works by examining each relation at least once.  Any time enforcing the relation causes a variable's domain to be 
        /// constrained, that variables relations are enqueued for examination.  Once the queue runs out of relations to be examined, 
        /// the algorithm terminates.
        /// </summary>
        /// <param name="relations">The set of relation objects which describe whether a given domain in variable A is supported by any 
        /// potential value in the domain  of variable B.</param>
        /// <returns>Returns the set of Variable objects which are modified by application of the algorithm.  If the problem is not 
        /// solveable, returns null.</returns>
        private static IEnumerable<Variable> ArcConsistency3(IEnumerable<Relation> relations)
        {
            HashSet<Variable> result = new HashSet<Variable>();             //The result is the set of all changed variables.
            Queue<Relation> queue = new Queue<Relation>(relations);         //The work queue.
            HashSet<Relation> onQueue = new HashSet<Relation>(relations);   //To allow O(1) determination if something is on the queue already.    

            while (queue.Count > 0)
            {
                //Pull the next constraint from the queue, and run its enforcement mechanism.
                Relation r = queue.Dequeue();
                onQueue.Remove(r);
                IEnumerable<Variable> changedVars = r.EnforceRelation();

                //What, if anything, should be added to the queue as a result of the enforcement?                
                if (changedVars == null) continue;
                foreach (Variable changedVar in changedVars)
                {
                    //If any variable has been invalidated, the problem cannot be solved.
                    if (changedVar.Domain.Count == 0) return null;

                    //Ensure that the result includes this changed variable.
                    result.Add(changedVar);

                    //Enqueue the relations of every changed variable.
                    foreach (Relation affectedRel in changedVar.Relations)
                        if (affectedRel.IsActive && onQueue.Add(affectedRel))
                            queue.Enqueue(affectedRel);
                }
            }

            return result;

        }

        /// <summary>
        /// Resolves the constraint resolution problem by applying the constraints to cull the possible domains of the Variables.
        /// </summary>
        /// <returns>If any variables were changed, the returned Variable array will contain references to those variables.  If no progress towards solution could be made, 
        /// the returned array will be empty.  If the constraint resolution problem is insoluable, a null reference is returns.</returns>
        internal void Resolve()
        {
            //TODO:  ConstraintResolver:  is AC-1 any faster than AC-3?  There may be some threshold at which to switch from one to the other.
            ArcConsistency3(_Relations);
        }


        public IEnumerable<IDictionary<TVariable, TDomain>> Solve()
        {
            Stack<ConstraintResolver<TVariable, TDomain>> stack = new Stack<ConstraintResolver<TVariable, TDomain>>();
            HashSet<ConstraintResolver<TVariable, TDomain>> onStack = new HashSet<ConstraintResolver<TVariable, TDomain>>();
            stack.Push(this);
            List<IDictionary<TVariable, TDomain>> solutions = new List<IDictionary<TVariable, TDomain>>();

            while (stack.Count > 0)
            {
                ConstraintResolver<TVariable, TDomain> problem = stack.Pop();
                onStack.Remove(problem);

                //What is changed by resolution?
                problem.Resolve();
                
                int size = stack.Count; //Use to signal if we've found either an invalid constraint, or we're at a solution.
                foreach (Variable v in problem._Variables.Values)
                {
                    if (v.IsValid)
                    {
                        size = -1;
                        break;
                    }
                    if (v.IsSolved)
                        continue;
                    //Try each unresolved domain in each variable to see if they lead to dead ends.
                    foreach (TDomain tryDomain in v.Domain)
                    {
                        ConstraintResolver<TVariable, TDomain> child = this.Copy();
                        Variable childVar = child._Variables[v.Tag];
                        childVar.Domain.Clear();
                        childVar.Domain.Add(tryDomain);
                        if (onStack.Add(child)) stack.Push(child);
                    }
                }

                if (size < 0) continue; //Was invalid.
                if (size == stack.Count)
                {
                    //Nothing new was added to the stack, which means that all variables are solved.
                    Dictionary<TVariable, TDomain> newSolution = new Dictionary<TVariable, TDomain>();
                    foreach (Variable v in problem._Variables.Values)                    
                        newSolution.Add(v.Tag, v.Domain.First());
                    solutions.Add(newSolution);                                        
                }               
                
            }
            return solutions;
        }


        public override bool Equals(object obj)
        {
            ConstraintResolver<TVariable, TDomain> cr = obj as ConstraintResolver<TVariable, TDomain>;
            if (cr == null) return false;
            if (cr._Relations.Count != _Relations.Count) return false;

            foreach (Relation r in _Relations)
            {
                if (!cr._Relations.Contains(r)) return false;                
            }
            return true;
        }
        public override int GetHashCode()
        {
            return _Relations.Sum((r) => r.GetHashCode());
        }



        #region Problem structure manipulation members

        public bool Add(TVariable label, Predicate<TDomain> rule)
        {
            Variable v;
            if (!_Variables.TryGetValue(label, out v))
            {
                v = new Variable(label, _StandardDomain);
                _Variables.Add(label, v);
            }
            UnaryRelation ur = new UnaryRelation(v, (d)=>rule(d));
            return _Relations.Add(ur);
        }

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
        
        
        
        /// <summary>
        /// Represents a variable tagged by a unique identifier, whose actual value may be any value within the variable's domain.
        /// </summary>
        internal sealed class Variable 
        {
            /// <summary>
            /// The unique identifier associated with this variable.
            /// </summary>
            public TVariable Tag { get; }

            /// <summary>
            /// The set of labels which represent the known possible domain labels of this variable.
            /// </summary>
            public HashList<TDomain> Domain { get; private set; }


            internal HashSet<Relation> Relations { get; private set; }

            /// <summary>
            /// Returns whether the variable still has a valid domain.  The variable has an invalid domain if it has no more 
            /// hypothetical values it could assume.
            /// </summary>
            public bool IsValid { get { return Domain.Count > 0; } }

            /// <summary>
            /// Returns whether the variable has been solved.
            /// </summary>
            public bool IsSolved { get { return Domain.Count == 1; } }

            /// <summary>
            /// Creates a new variable with the given label, and sets up the initial domain and hypothesis sets from the given domain 
            /// IEnumerable.
            /// </summary>
            public Variable(TVariable tag, IEnumerable<TDomain> domain)
            {
                Tag = tag;
                Domain = new HashList<TDomain>(domain);
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
        internal abstract class Relation
        {
            /// <summary>
            /// A cached value storing the hash code.
            /// </summary>
            private int _HashCode = 0;

            /// <summary>
            /// A cached value indicating whether this relation is still active.  An inheriting class may set this to false once the 
            /// relation can do no further useful work (i.e., all variables are either invalid or solved).  Inactive relations are not 
            /// examined any further by some arc consistency algorithms 
            /// </summary>
            protected internal bool IsActive = true;

         
            
            /// <summary>
            /// Creates a new relation object referencing the given variables.  The object's hash code will be set by summing the 
            /// hash codes of the tags of the given variables.
            /// </summary>
            /// <param name="variables"></param>
            protected Relation(IEnumerable<Variable> variables)
            {
                foreach (Variable v in variables)
                {
                    unchecked
                    {
                        _HashCode += v.Tag.GetHashCode();
                    }
                    v.Relations.Add(this);
                }
                _HashCode = Math.Abs(_HashCode);                
            }

            /// <summary>
            /// Returns whether this relation has yet to be fully resolved.
            /// </summary>            
            internal abstract bool GetIsUnresolved();
            

            internal IEnumerable<Variable> EnforceRelation()
            {
                IEnumerable<Variable> result = Enforce();
                IsActive = GetIsUnresolved();
                return result;
            }

            //public abstract IEnumerable<Variable> GetAllVariables();

            /// <summary>
            /// Enforces the relation by culling hypothetical domain labels from the variables' domains.  This method returns an 
            /// IEnumerable containing all Variable objects whose domains were changed as a result of the enforcement.
            /// </summary>            
            public abstract IEnumerable<Variable> Enforce();            
            

            /// <summary>
            /// The hashcode returned is the sum of the hash codes of the tags of the variables related to this relation object.  
            /// This means that all relations dealing with the same variables will hash identically, even if Equals() is not true 
            /// because the variables are in different orders.
            /// <para/>Note that inheriting members cannot override the GetHashCode() method, even if they override the Equals() 
            /// method.  This is because the hash code is cached in a private member defined in the base class to improve hashing 
            /// efficiency, which is a common operation in the arc consistency algorithms.  Use "#pragma warning disable CS0659" and 
            /// "#pragma warning restore CS0659" to deal with the obnoxious warnings.
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

            /// <summary>
            /// Returns a deep copy of this Relation, which is used to copy a constraint resolution problem.  Variables must be copied 
            /// automatically, but if anything else needs to be copied (like a lambda expression) this can be done here.
            /// </summary>
            protected internal abstract Relation GetCopy(ConstraintResolver<TVariable, TDomain> problem);

        }

#pragma warning disable CS0659

        /// <summary>
        /// A relationship of a single variable.  The potential values in the variable's domain that do not comply with the given rule 
        /// will be culled.
        /// </summary>
        internal sealed class UnaryRelation : Relation
        {
            public readonly Variable Variable;
            public readonly Func<TDomain, bool> Rule;

            /// <summary>
            /// Creates an unary relation for a single variable.
            /// </summary>
            /// <param name="variable">The variable whose domain will be affected by the given rule.</param>
            /// <param name="rule">The rule which will be examined to determine if a given domain is consistent or not.</param>
            public UnaryRelation(Variable variable, Func<TDomain, bool> rule)
                : base(new Variable[] { variable })
            {
                Variable = variable;
                Rule = rule;
            }

            internal override bool GetIsUnresolved()
            {
                return Variable.Domain.Count > 1;
            }
                     
           

            public override IEnumerable<Variable> Enforce()
            {                
                if ( Variable.Domain.RemoveWhere((d) => !Rule(d)) > 0)
                {
                    if (Variable.Domain.Count <= 1) IsActive = false;
                    return new Variable[] { Variable };
                }
                return new Variable[0];
            }

            public override bool Equals(object obj)
            {
                return obj is UnaryRelation && ((UnaryRelation)obj).Variable.Equals(Variable);
            }

            protected internal override Relation GetCopy(ConstraintResolver<TVariable, TDomain> problem)
            {                
                Variable v;
                if (!problem._Variables.TryGetValue(Variable.Tag, out v))
                {
                    v = new Variable(Variable.Tag, Variable.Domain);
                    problem._Variables.Add(v.Tag, v);
                }
                return new UnaryRelation(v, Rule);
            }
            
            
        }

        /// <summary>
        /// A relationship of two variables.  At each iteration of the arc consistency algorithms, if none of Variable B's domain values 
        /// support the existence of Variable A's domain value, that value will be culled from A's domain.
        /// </summary>
        internal sealed class BinaryRelation : Relation
        {
            public readonly Variable VariableA, VariableB;
            public readonly Func<TDomain, TDomain, bool> Rule;

            /// <summary>
            /// Creates a binary relation for two variables.
            /// </summary>            
            /// <param name="rule">The rule which will be examined to determine if a given pair of domains is consistent or not.</param>
            public BinaryRelation(Variable variableA, Variable variableB, Func<TDomain, TDomain, bool> rule)
                : base(new Variable[] { variableA, variableB })
            {
                VariableA = variableA;
                VariableB = variableB;
                Rule = rule;
            }

            internal override bool GetIsUnresolved()
            {
                return VariableA.Domain.Count > 1 || VariableB.Domain.Count > 1;
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

            protected internal override Relation GetCopy(ConstraintResolver<TVariable, TDomain> problem)
            {
                Variable vA, vB;
                if (!problem._Variables.TryGetValue(VariableA.Tag, out vA))
                {
                    vA = new Variable(VariableA.Tag, VariableA.Domain);
                    problem._Variables.Add(vA.Tag, vA);
                }
                if (!problem._Variables.TryGetValue(VariableB.Tag, out vB))
                {
                    vB = new Variable(VariableB.Tag, VariableB.Domain);
                    problem._Variables.Add(vB.Tag, vB);
                }

                return new BinaryRelation(vA, vB, Rule);
            }
        }

        internal sealed class TernaryRelation : Relation
        {
            //TODO:  ConstraintResolver:  finish implementing TernaryRelation.
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
            internal override bool GetIsUnresolved()
            {
                return VariableA.Domain.Count > 1 && VariableB.Domain.Count > 1 && VariableC.Domain.Count > 1;
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
            protected internal override Relation GetCopy(ConstraintResolver<TVariable, TDomain> problem)
            {
                Variable vA, vB, vC;                
                if (!problem._Variables.TryGetValue(VariableA.Tag, out vA))
                {
                    vA = new Variable(VariableA.Tag, VariableA.Domain);
                    problem._Variables.Add(vA.Tag, vA);
                }
                if (!problem._Variables.TryGetValue(VariableB.Tag, out vB))
                {
                    vB = new Variable(VariableB.Tag, VariableB.Domain);
                    problem._Variables.Add(vB.Tag, vB);
                }
                if (!problem._Variables.TryGetValue(VariableC.Tag, out vC))
                {
                    vC = new Variable(VariableC.Tag, VariableC.Domain);
                    problem._Variables.Add(vC.Tag, vC);
                }

                return new TernaryRelation(vA, vB, vC, Rule);
            }
        }

        internal sealed class GlobalRelation : Relation
        {
            //TODO:  ConstraintResolver:  finish implementing GlobalRelation.          
            public readonly Func<IList<TDomain>, bool> Rule;

            public GlobalRelation(IList<Variable> variables, Func<IList<TDomain>, bool> rule)
                : base(variables)
            {               
                Rule = rule;
            }

          

            public override IEnumerable<Variable> Enforce()
            {
               
                
                throw new NotImplementedException();
            }

            public override bool Equals(object obj)
            {
                throw new NotImplementedException();
            }
            internal override bool GetIsUnresolved()
            {
                throw new NotImplementedException();
            }
            protected internal override Relation GetCopy(ConstraintResolver<TVariable, TDomain> problem)
            {
                throw new NotImplementedException();
            }
        }

#pragma warning restore CS0659

        #endregion


        
    }

}
