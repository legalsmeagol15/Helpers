using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Graphs;

namespace AI.Satisfiability
{
    public static class Constraints
    {
        
       
        /// <summary>
        /// Implements ArcConsistency1....???
        /// </summary>
        /// <typeparam name="TDomain"></typeparam>
        /// <param name="arcs"></param>
        /// <param name="hypothesis"></param>
        /// <returns>Returns true if the given hypothesis is consistent with the given constraints; otherwise, returns false.</returns>
        public static bool ArcConsistency1<TDomain>(IEnumerable<Constraint<TDomain>> arcs, Hypothesis<TDomain> hypothesis)
        {
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (Constraint<TDomain> arc in arcs)
                {
                    //Find out if any variables' domains were updated.
                    IEnumerable<Variable<TDomain>> updated = arc.Update(hypothesis);
                    if (updated == null || updated.Count() == 0) continue;

                    //Signal that a change was made.
                    changed = true;

                    //Was this hypothesis invalidated?  If so, return null.                    
                    foreach (Variable<TDomain> updatedVar in updated)
                    {
                        if (updatedVar.Domains.Count == 0) return false;                    
                    }                    
                }
            }

            return true;        
        }

        public static bool ArcConsistency3<TDomain>(IEnumerable<Constraint<TDomain>> arcs, Hypothesis<TDomain> hypothesis)
        {
            //Figure out which arcs may affect which tags.
            Queue<Constraint<TDomain>> workQueue = new Queue<Constraint<TDomain>>();
            HashSet<Constraint<TDomain>> inQueue = new HashSet<Constraint<TDomain>>();
            Dictionary<object, List<Constraint<TDomain>>> dependencies = new Dictionary<object, List<Constraint<TDomain>>>();
            foreach (Constraint<TDomain> arc in arcs)
            {
                foreach (object tag in arc.Tags)
                {
                    List<Constraint<TDomain>> list;
                    if (!dependencies.TryGetValue(tag, out list))
                    {
                        list = new List<Constraint<TDomain>>();
                        dependencies.Add(tag, list);
                    }
                    list.Add(arc);
                }
                workQueue.Enqueue(arc);
                inQueue.Add(arc);  
            }

            //Now, the domain resolution step.            
            while (workQueue.Count > 0)
            {
                Constraint<TDomain> arc = workQueue.Dequeue();
                inQueue.Remove(arc);
                IEnumerable<Variable<TDomain>> updated = arc.Update(hypothesis);
                if (updated == null || updated.Count() == 0) continue;

                foreach (Variable<TDomain> v in updated)
                {
                    if (v.Domains.Count == 0) return false;
                    List<Constraint<TDomain>> deps = dependencies[v.Tag];                 
                    foreach (Constraint<TDomain> dep in deps)
                    {
                        if (inQueue.Add(dep)) workQueue.Enqueue(dep);
                    }
                }
            }

            //Everything that can be constrainted has been constrained.
            return true;
        }


      
        /// <summary>
        /// Returns the solutions that are consistent with the given constraints, based on the original hypothesis.
        /// </summary>        
        /// <param name="arcs">The set of constraints that will determine whether an hypothesis is valid or not.</param>
        /// <param name="hypothesis">The set of possible domain values for a set of variables.</param>
        /// <param name="solutionCount">Optional.  The maximum number of solutions to return.  If only one solution is needed, setting this 
        /// value to 1 will return the first solution discovered.</param>
        /// <returns>Returns a collection of hypotheses, all of which will represent a complete solution of the given constraint problem.</returns>
        public static IEnumerable<Hypothesis<TDomain>> Solve<TDomain>(IEnumerable<Constraint<TDomain>> arcs, Hypothesis<TDomain> hypothesis, int solutionCount = int.MaxValue)
        {
            //Get an indexed list of variables.
            Variable<TDomain>[] variables = hypothesis.Variables.ToArray();
            if (variables.Length == 0) throw new ArgumentException("No variables in the given hypothesis.");

            //Set up and prime the work stack.
            Stack<SolveNode<TDomain>> workStack = new Stack<SolveNode<TDomain>>();
            Variable<TDomain> v = variables[0];
            foreach (TDomain domain in v.Domains)
            {
                Hypothesis<TDomain> hypo = hypothesis.Copy();
                IList<TDomain> possibles = hypo[v];
                possibles.Clear();
                possibles.Add(domain);
                SolveNode<TDomain> node = new SolveNode<TDomain>(hypo, 1);
                workStack.Push(node);
            }

            //Declare a result variable.
            List<Hypothesis<TDomain>> result = new List<Hypothesis<TDomain>>();

            //Do the work.
            while (workStack.Count > 0)
            {
                SolveNode<TDomain> node = workStack.Pop();

                //Base case #1 - are we looking at a solution?
                if (node.Depth == variables.Length)
                {
                    result.Add(node.Hypothesis);
                    if (result.Count == solutionCount) return result;
                    continue;
                }

                //Base case #2 - apply arc consistency, check if it's a dead end.
                if (ArcConsistency1(arcs, node.Hypothesis)== false)
                {
                    continue;
                }

                //Recursive case - add the possible values for the next variable.
                v = variables[node.Depth];
                foreach (TDomain domain in v.Domains)
                {
                    Hypothesis<TDomain> hypo = hypothesis.Copy();
                    IList<TDomain> possibles = hypo[v];
                    possibles.Clear();
                    possibles.Add(domain);
                    SolveNode<TDomain> newNode = new SolveNode<TDomain>(hypo, node.Depth + 1);
                    workStack.Push(node);
                }
            }

            //Finally, return the result.
            return result;

        }

        

        private class SolveNode<TDomain>
        {
            public readonly Hypothesis<TDomain> Hypothesis;
            public readonly int Depth;
            public SolveNode(Hypothesis<TDomain> hypothesis, int depth)
            {
                Hypothesis = hypothesis;
                Depth = depth;
            }
        }

    }
    

   
}
