using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataStructures;
using DataStructures.Graphs;
using DataStructures.Sets;


namespace ArtificialIntelligence.Heuristics
{
    public static class Search
    {
        /// <summary>
        /// Conducts a search from the given start label start, or from every label reachable from the given start label state, to 
        /// the goal label state.  The search will be guided and prioritized according to the given heuristic.
        /// </summary>
        /// <typeparam name="TLabel">The type of the data label for the search.</typeparam>
        /// <param name="start">The starting label.  For example, if the search is across a 2-dimensional space, the label may be a 
        /// point (1,1).</param>
        /// <param name="goal">The goal label.  For example, if the search is across a 2-dimensional space, the label may be a 
        /// point (10,10).</param>
        /// <param name="childGetter">The function used to find the child labels of a given label.  For example, if the search is 
        /// across a 2-dimensional space, the children of the point (2,2) would be four points:  (3,2), (2,3), (1,2), and (2,1).  
        /// These four points would be returned in an array or List object.
        /// </param>
        /// <param name="heuristic">The heuristic priority function that elevates labels in order of their likelihood to 
        /// bring the search to the goal.  </param>
        /// <param name="vField">An IVertex object that contains a reference to the entire heuristically-guided search field, which 
        /// starts at this vertex.  The field can be traversed by referene to the vertex's edges.</param>
        /// <param name="costGetter">Optional.  The function used to weight the transition from one label to another.  If omitted, 
        /// the weight in all cases will be presumed to be 1.0.</param>
        /// <param name="terminateAtGoal">Optional.  Determines whether to terminate the search upon reaching the goal.  If the 
        /// search is allowed to continue, the vField output will return a comprehensive search of all label states reachable 
        /// from the given start.  If omitted, the method will presume to terminate upon reaching the goal (will be set to 'true').
        /// </param>
        /// <returns>Returns a linked list representing the route of the search from start to goal.</returns>
        public static IEnumerable<TLabel> AStarSearch<TLabel>(TLabel start, TLabel goal, Func<TLabel,
                                                              IEnumerable<TLabel>> childGetter, IComparer<TLabel> heuristic,
                                                              out IVertex<TLabel> vField,
                                                              Func<TLabel, TLabel, double> costGetter = null,
                                                              bool terminateAtGoal = true)
        {
            //TODO:  Validate AStarSearch

            //Step #0 - set up data to start the search.
            Heap<TLabel> toExplore = new Heap<TLabel>(heuristic);
            AStarNode<TLabel> vGoal = null, vStart = new AStarNode<TLabel>(start);
            vStart.Prior = null;
            vStart.AccumulatedCost = 0.0d;
            Dictionary<TLabel, AStarNode<TLabel>> prepared = new Dictionary<TLabel, AStarNode<TLabel>>();
            prepared.Add(start, vStart);
            toExplore.Enqueue(start);
            double maxCost = double.PositiveInfinity;

            //First, explore to find the route from start (or from all reachable labels) to goal.
            while (toExplore.Count > 0)
            {
                //Step #1 - figure out what the current node is.
                TLabel currentLabel = toExplore.Dequeue();
                AStarNode<TLabel> currentNode = prepared[currentLabel];

                //Step #2a - if the current node is equal to goal, make sure the method knows that and no further processing needed 
                //for this node (doesn't matter what children the goal node may have).
                if (currentLabel.Equals(goal))
                {
                    vGoal = currentNode;
                    if (vGoal.AccumulatedCost < maxCost) maxCost = vGoal.AccumulatedCost;
                    continue;
                }
                //Step #2b - if it's not the goal, but the accumulated cost is to great to find a cheaper path and we don't need to 
                //proceed past the goal, then there's no reason to examine this node's children anyway.
                if (terminateAtGoal && currentNode.AccumulatedCost >= maxCost)
                    continue;

                //Step #3 - add the children for the current node.
                IEnumerable<TLabel> currentChildren = childGetter(currentLabel);
                foreach (TLabel childLabel in currentChildren)
                {
                    //Find out what the accumulated cost would be to get to the child label through the current label.
                    double accCost = currentNode.AccumulatedCost;
                    accCost += (costGetter == null ? 1.0d : costGetter(currentLabel, childLabel));

                    //If the child doesn't already exists on the dict, then time to create the child node and have its priors point 
                    //to the current node.
                    AStarNode<TLabel> childNode;
                    if (!prepared.TryGetValue(childLabel, out childNode))
                    {
                        childNode = new AStarNode<TLabel>(childLabel);
                        childNode.Prior = currentNode;
                        childNode.AccumulatedCost = accCost;
                        prepared.Add(childLabel, childNode);
                        toExplore.Enqueue(childLabel);
                    }
                    //If the child DOES exist on the dict, check to see if we've found a cheaper route to that child.
                    else if (accCost < childNode.AccumulatedCost)
                    {
                        childNode.Prior = currentNode;
                        childNode.AccumulatedCost = accCost;
                    }

                    //Either way, add the edge from the current node to the child node.
                    currentNode.Edges.Add(new Edge<TLabel>(currentNode, childNode));
                }
            }

            //Now, get the route by traversing backwards.
            LinkedList<TLabel> route = new LinkedList<TLabel>();
            while (vGoal != null)
            {
                route.AddFirst(vGoal.Label);
                vGoal = vGoal.Prior;
            }

            //Done.  Store and return the results.
            vField = vStart;
            return route;
        }


        /// <summary>
        /// Conducts a search from the given start label state, to the goal label state.  The search will be guided and prioritized 
        /// according to the given heuristic.
        /// </summary>
        /// <typeparam name="TLabel">The type of the data label for the search.</typeparam>
        /// <param name="start">The starting label.  For example, if the search is across a 2-dimensional space, the label may be a 
        /// point (1,1).</param>
        /// <param name="goal">The goal label.  For example, if the search is across a 2-dimensional space, the label may be a 
        /// point (10,10).</param>
        /// <param name="childGetter">The function used to find the child labels of a given label.  For example, if the search is 
        /// across a 2-dimensional space, the children of the point (2,2) would be four points:  (3,2), (2,3), (1,2), and (2,1).  
        /// These four points would be returned in an array or List object.
        /// </param>
        /// <param name="heuristic">The heuristic priority function that elevates labels in order of their likelihood to 
        /// bring the search to the goal.  </param>        
        /// <param name="costGetter">Optional.  The function used to weight the transition from one label to another.  If omitted, 
        /// the weight in all cases will be presumed to be 1.0.</param>
        /// <returns>Returns a linked list representing the route of the search from start to goal.</returns>
        public static IEnumerable<TLabel> AStarSearch<TLabel>(TLabel start, TLabel goal, Func<TLabel,
                                                              IEnumerable<TLabel>> childGetter, IComparer<TLabel> heuristic,
                                                              Func<TLabel, TLabel, double> costGetter = null)
        {
            IVertex<TLabel> dummyVertex;
            return AStarSearch(start, goal, childGetter, heuristic, out dummyVertex, costGetter, true);
        }





        /// <summary>
        /// A lightweight data structure used to represent a node in the A* search.
        /// </summary>    
        internal class AStarNode<TLabel> : IVertex<TLabel>
        {
            public AStarNode(TLabel label)
            {
                Label = label;
            }

            /// <summary>
            /// The edges leading from this node out.
            /// </summary>
            public List<IEdge<TLabel>> Edges = new List<IEdge<TLabel>>();
            IEnumerable<IEdge<TLabel>> IVertex<TLabel>.Edges { get { return Edges; } }

            /// <summary>
            /// The last node visited before reaching this node.
            /// </summary>
            public AStarNode<TLabel> Prior { get; set; }

            /// <summary>
            /// The data associated with this node.
            /// </summary>
            public TLabel Label { get; }

            /// <summary>
            /// The accumulated cost of reaching this node.
            /// </summary>
            public double AccumulatedCost { get; set; }
        }
    }





}
