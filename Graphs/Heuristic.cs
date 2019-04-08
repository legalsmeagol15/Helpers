using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphs
{
    /// <summary>
    /// 
    /// </summary>
    public static class Heuristic
    {

        /// <summary>
        /// Returns the edge-by-edge solution set that searches from the given initial state to the given goal state.  This search method 
        /// is otherwise known as A* search.
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="initialState">The starting state from which to search for the goal state.</param>
        /// <param name="goalState">The goal state being sought in this search.</param>
        /// <param name="edgeGetter">The function that returns the non-redundant frontier states for the given state. </param>
        /// <param name="heuristic">The method that estimates the cost between the given state and the goal state.</param>
        /// <param name="decisionRoot">The output root of the search tree produced.</param>
        /// <returns>An IList specifying the edge-by-edge solution set.  If the result is null, no solution could be found.</returns>
        public static IEnumerable<IEdge<TState>> HeuristicSearch<TState>(TState initialState, TState goalState, 
                                                                        Func<TState, HashSet<TState>, IEnumerable<IEdge<TState>>> edgeGetter,
                                                                        Func<TState, double> heuristic,
                                                                        out IVertex<TState> decisionRoot)
        {
            //TODO:  Validate Graphs.Heuristic.HeuristicSearch

            Node<TState> root = new Node<TState>(initialState, null, 0.0d, heuristic(initialState));
            HashSet<TState> added = new HashSet<TState>();
            added.Add(initialState);                  
            decisionRoot = root;
            DataStructures.SkipList<Node<TState>> edges = new DataStructures.SkipList<Node<TState>>(new HeuristicEdgeComparer<TState>());
            edges.Add(root);
            while (edges.Count > 0)
            {
                Node<TState> focus = edges.RemoveMin();

                //Is the focus state the goal state?
                if (focus.State.Equals(goalState))
                {                    
                    LinkedList<IEdge<TState>> result = new LinkedList<IEdge<TState>>();                    
                    while (focus.Parent != null)
                    {
                        result.AddFirst(focus.Parent.Children[focus.State]);
                        focus = focus.Parent;
                    }
                    return result;
                }

                // Find the new, original children.
                IEnumerable<IEdge<TState>> childEdges = edgeGetter(focus.State, added);                
                foreach (IEdge<TState> e in childEdges)
                {
                    if (!added.Add(e.Destination.Data)) continue;   //Skip non-original children.

                    Node<TState> childNode = new Node<TState>(e.Destination.Data, focus, focus.AccumulatedCost + e.Cost, 
                                                              heuristic(e.Destination.Data));

                    edges.Add(childNode);
                }
            }

            //Ran out of edges to try.  No solution, though there will be a search three.
            decisionRoot = root;
            return null;
        }

        private class HeuristicEdgeComparer<TState> : IComparer<Node<TState>>
        {
            public int Compare(Node<TState> x, Node<TState> y)
            {
                return ((x.AccumulatedCost + x.EstimatedCost).CompareTo(y.AccumulatedCost + y.EstimatedCost));
            }
        }


        private class Node<TState> : IVertex<TState>
        {
            public readonly TState State;
            public readonly double AccumulatedCost;
            public readonly double EstimatedCost;
            public readonly Dictionary<TState, IEdge<TState>> Children = new Dictionary<TState, IEdge<TState>>();
            public readonly Node<TState> Parent;

            TState IVertex<TState>.Data { get { return State; } }
                        
            IEnumerable<IEdge<TState>> IVertex<TState>.Edges { get { return Children.Values; } }

            public Node (TState state, Node<TState> parent, double accumulatedCost, double estimatedCost)
            {
                State = state;
                Parent = parent;                
                AccumulatedCost = accumulatedCost;
                EstimatedCost = estimatedCost;
            }
        }
    }
}
