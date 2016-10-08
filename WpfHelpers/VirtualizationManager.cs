using DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Collections;

namespace WpfHelpers
{

    /// <summary>
    /// Manages virtualization by determining if content items should be visible based on the given Viewport.  Virtualization is possible only 
    /// for items that implement the IVIrtualizeable interface; however, any Visual object can be maintained here.
    /// </summary>
    /// <remarks> Virtualization works by using a HashList to track which strokes should be "Visible", and comparing it against all items that 
    /// are stored on a quadtree that performantly tracks which objects would be snagged by the current Viewport.
    ///<para/>The VirtualizationManager can have multiple instances of the same IVirtualizeable object, but only a single instance of 
    /// any other type of item.
    /// <para/>The VirtualizationManager is not thread-safe for reads and writes of its contents, so threading should be planned 
    /// accordingly.  Further, the Visible property is updated only after an asynchronous call.  While it is only accessed in the GUI 
    /// thread, its state is not guaranteed to be synchronized with the contents of the VirtualizationManager for any given Viewport.
    /// <para/>TODO:  Someday, implement virtualization  of ANY visual (through VIsualTreeHelper boundary checking).
    /// </remarks>
    
    public class VirtualizationManager : ICollection<Visual>
    {
        /// Invariant rules:
        /// 1) Main thread modifying _QuadTree must lock _QuadTree.
        /// 2) Background thread that reads _QuadTree must lock _QuadTree.
        /// 3) Main thread reading _QuadTree must NOT lock _QuadTree.


        /// <summary>
        /// Yeah the Viewport backer.
        /// </summary>
        private Rect _Viewport;
        /// <summary>
        /// The current viewable area.
        /// </summary>
        public Rect Viewport
        {
            get
            {
                return _Viewport;
            }
            set
            {
                _Viewport = value;
                UpdateVirtualization();
            }
        }

        /// <summary>
        /// The set of all IVirtualizeable items to be held in this manager.
        /// </summary>
        private QuadTree<IVirtualizeable> _QuadTree = new QuadTree<IVirtualizeable>((ib) => ib.Boundary);
        /// <summary>
        /// The set of items currently visible.
        /// </summary>
        private HashList<Visual> _Visible = new HashList<Visual>();
        /// <summary>
        /// Returns all the items currently visible as a read-only list.
        /// </summary>
        public IReadOnlyList<Visual> Visible { get { return _Visible.AsReadOnly(); } }
        /// <summary>
        /// The set of non-IVirtualizeable objects to be held in this manager.
        /// </summary>
        private HashSet<Visual> _NonVirtualizeable = new HashSet<Visual>();

        private bool _IsVisible = true;
        /// <summary>
        /// Whether the items contained on this manager are visible.  Changing this value will cause an update to fire (assuming there are 
        /// items contained in this manager that would be visible in the current Viewport).
        /// </summary>
        public bool IsVisible
        {
            get
            {
                return _IsVisible;
            }
            set
            {
                _IsVisible = value;
                UpdateVirtualization();
            }
        }

        public Rect Extent { get; private set; }




        /// <summary>
        /// Creates a new VirtualizationManager.
        /// </summary>
        public VirtualizationManager()
        {
            //Set up the VirtualizationWorker.
            _VirtualizationWorker = new DataStructures.Threading.PipelineWorker<object>(false, false, 1);
            _VirtualizationWorker.OverflowStrategy = DataStructures.Threading.PipelineWorker<object>.OverflowStrategies.DrainPipe;
            _VirtualizationWorker.DoWork += VirtualizationWorker_DoWork;
            _VirtualizationWorker.RunWorkerCompleted += VirtualizationWorker_RunWorkerCompleted;            
        }



        #region VirtualizationManager process members

        /// <summary>
        /// Begins the virtualization update process.  If any item is added or removed, an event will fire.
        /// </summary>
        public void UpdateVirtualization()
        {
            _VirtualizationWorker.Enqueue(DateTime.Now);    //The datetime is unimportant, it just distinguishes from other items in pipe.  
        }


        /// <summary>
        /// The worker that determines which objects should be virtualized on a separate thread.
        /// </summary>
        private DataStructures.Threading.PipelineWorker<object> _VirtualizationWorker;


        private void VirtualizationWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            //Step #0 - snag a copy of the current view.
            Rect view = _Viewport;
            
            //Step #1 - get the list of everything that should be visible, and sort it by ZIndex.
            List<IVirtualizeable> inView;
            lock (_QuadTree)
                inView = new List<IVirtualizeable>(_QuadTree.Intersect(view));

            //Step #1a - Put in correct ZIndex order.
            inView.Sort(_Comparer);
            //inView.Sort(delegate (IVirtualizeable a, IVirtualizeable b) { return a.CompareTo(b); });    
           
            //Step #2 - determine what needs to be added and what needs to be removed due to virtualization.
            HashList<Visual> newVisible;
            lock (_NonVirtualizeable)
                newVisible = new HashList<Visual>(_NonVirtualizeable);           
            List<Visual> added = new List<Visual>();
            List<Visual> removed = new List<Visual>();

            //Step #2a - What gets added?
            if (_IsVisible)
            {
                foreach (Visual newV in inView)     //There is an implicit type cast from IVirtualizeable to Visual here.
                {
                    newVisible.Add(newV);
                    if (!_Visible.Contains(newV)) added.Add(newV);
                }
            }            

            //Step #2b - What gets removed?     
            foreach (Visual oldV in _Visible)
                if (oldV is IVirtualizeable && !newVisible.Contains(oldV))
                    removed.Add(oldV);


            //Step #3 - set the result to be the newvisible
            e.Result = new VisibilityUpdatedEventArgs(view, newVisible, added, removed);
        }

        private static readonly ComparableComparer _Comparer = new ComparableComparer();
        private class ComparableComparer : IComparer<IVirtualizeable>
        {
            int IComparer<IVirtualizeable>.Compare(IVirtualizeable a, IVirtualizeable b) { return a.ZIndex.CompareTo(b.ZIndex); }
        }

        /// <summary>
        /// Signifies a change in the visibility status of the given objects.
        /// </summary>
        public class VisibilityUpdatedEventArgs : EventArgs
        {
            /// <summary>
            /// The viewport for the virtualization information contained here.
            /// </summary>
            public readonly Rect Viewport;

            /// <summary>
            /// The items that were added to the visible set.
            /// </summary>
            public readonly IEnumerable<Visual> Added;

            /// <summary>
            /// The items that were removed from the visible set.
            /// </summary>
            public readonly IEnumerable<Visual> Removed;

            /// <summary>
            /// The set of all visible items.
            /// </summary>
            internal readonly HashList<Visual> AllVisible;

            public VisibilityUpdatedEventArgs(Rect viewport, HashList<Visual> result, IEnumerable<Visual> added, IEnumerable<Visual> removed)
            {
                this.Viewport = viewport;
                this.Removed = removed;
                this.Added = added;
                this.AllVisible = result;
            }
        }
      


        private void VirtualizationWorker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
                Console.WriteLine("VirtualizationWorker error:\n" + e.Error.ToString());
            else if (e.Result == null) return;

            //Add/remove the BoundaryChanged event for each changed child.
            VisibilityUpdatedEventArgs result = (VisibilityUpdatedEventArgs)e.Result;
                  
            foreach (Visual removed in result.Removed)
            {
                if (removed is IVirtualizeable)
                    ((IVirtualizeable)removed).ShapeChanged -= Child_BoundaryChanged;
            }
            foreach (Visual added in result.Added)
            {
                if (added is IVirtualizeable)
                    ((IVirtualizeable)added).ShapeChanged += Child_BoundaryChanged;
            }

            //If a change was made, fire off the Updated event.
            if (result.Added.Count() > 0 || result.Removed.Count() > 0)
            {
                _Visible = result.AllVisible;
                EventHandler<VisibilityUpdatedEventArgs> handler = Updated;
                if (handler != null) handler(this, result);
            }

        }

        /// <summary>
        /// Raised when the virtualization status of some child of this manager is updated.
        /// </summary>
        public event EventHandler<VisibilityUpdatedEventArgs> Updated;

        #endregion




        #region VirtualizationManager contents modification and queries members

        private void Child_BoundaryChanged(object sender, EventArgs e)
        {
            //This method runs on the main thread.
            IVirtualizeable s = (IVirtualizeable)sender;
            lock (_QuadTree)
            {
                _QuadTree.Remove(s);
                _QuadTree.Add(s);
            }
            Extent = _QuadTree.Extent;
            UpdateVirtualization();
        }

        /// <summary>
        /// Ensures the given item exists on this manager.
        /// </summary>
        /// <param name="item"></param>
        public void Add(Visual item)
        {
            //This method runs on the main thread.
            if (item is IVirtualizeable)
            {
                lock (_QuadTree) _QuadTree.Add((IVirtualizeable)item);
                Extent = _QuadTree.Extent;
            }
                
            else
                lock (_NonVirtualizeable) _NonVirtualizeable.Add(item);
            
            UpdateVirtualization();            
        }

        /// <summary>
        /// Removes all items from this manager.
        /// </summary>
        public void Clear()
        {
            //This method runs on the main thread
            lock (_QuadTree) _QuadTree.Clear();
            Extent = _QuadTree.Extent;
            lock (_NonVirtualizeable) _NonVirtualizeable.Clear();            
            UpdateVirtualization();
        }

        /// <summary>
        /// Returns whether the given item is contained on this manager (whether virtualized or not).  This method is an O(1) operation.
        /// </summary>
        public bool Contains(Visual item)
        {
            if (item is IVirtualizeable) return _QuadTree.Contains((IVirtualizeable)item);
            return _NonVirtualizeable.Contains(item);
        }

        /// <summary>
        /// Returns a set (actually a List) of items contained in this manager which intersect the given Rect.
        /// </summary>
        public IEnumerable<IVirtualizeable> GetIntersection(Rect rect)
        {            
            return _QuadTree.Intersect(rect);
        }

      

        void ICollection<Visual>.CopyTo(Visual[] array, int arrayIndex)
        {
            foreach (IVirtualizeable iv in _QuadTree)
            {
                if (arrayIndex >= array.Length) return;
                array[arrayIndex++] = (Visual)iv;
            }
            foreach (Visual v in _NonVirtualizeable)
            {
                if (arrayIndex >= array.Length) return;
                array[arrayIndex++] = v;
            }
        }

        /// <summary>
        /// Returns the number of items currently contained, whether virtualized, not virtualized, or nonvirtualizeable.
        /// </summary>
        public int Count { get { return _QuadTree.Count + _NonVirtualizeable.Count; } }



        public IEnumerator<Visual> GetEnumerator()
        {            
            foreach (IVirtualizeable item in _QuadTree) yield return (Visual)item;
            foreach (Visual item in _NonVirtualizeable) yield return item;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

   
       

        bool ICollection<Visual>.IsReadOnly { get { return false; } }


        /// <summary>
        /// Removes the given item from this manager.
        /// </summary>
        public bool Remove(Visual item)
        {
            if (item is IVirtualizeable)
            {
                IVirtualizeable iv = (IVirtualizeable)item;
                int itemCount;
                lock (_QuadTree) 
                    itemCount = _QuadTree.Remove(iv);
                Extent = _QuadTree.Extent;
                if (itemCount < 0) return false;                
                else if (itemCount == 0) UpdateVirtualization();
                return true;
            }
            else
            {
                lock (_NonVirtualizeable)
                    if (!_NonVirtualizeable.Remove(item)) return false;
                UpdateVirtualization();
                return true;
            }
        }
        #endregion


    }
}
