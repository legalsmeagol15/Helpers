using DataStructures;
using Dependency.Functions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using Mathematics;

namespace Dependency.Variables
{
    public sealed class Update
    {
        public static bool ActiveListeners = true;
        public static readonly ITrueSet<IEvaluateable> UniversalSet = DataStructures.Sets.TrueSet<IEvaluateable>.CreateUniversal();

        // Like a SQL transaction
        internal static readonly ReaderWriterLockSlim StructureLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        //  private readonly ConcurrentQueue<ISyncUpdater> _Items = new ConcurrentQueue<ISyncUpdater>();
        private static readonly ConcurrentQueue<Task> _Tasks = new ConcurrentQueue<Task>();
        internal readonly IVariable Starter;
        public readonly IEvaluateable NewContents;
        private readonly IEnumerable<IEvaluateable> Indices = null;

        private static long _Updating = 0;
        public static long Updating => Interlocked.Read(ref _Updating);

        public static event EventHandler Settled;
        internal static int MaxConcurrentExecutions = Environment.ProcessorCount * 2;
        internal static Semaphore SettledSignal = new Semaphore(MaxConcurrentExecutions, MaxConcurrentExecutions);

        
        private Update(IVariable var, IEvaluateable newContents, IEnumerable<IEvaluateable> indices)
        {
            this.Starter = var ?? throw new ArgumentNullException(nameof(var));
            if (newContents == null) newContents = Dependency.Null.Instance;
            else if (newContents is Expression exp) newContents = exp.Contents;
            this.NewContents = newContents;
            this.Indices = indices;
        }

        private readonly static object _PauseObject = new object();
        
        public static bool IsPaused { get; private set; } = false;

        /// <summary>
        /// Blocks until all updates are settled, then returns.
        /// </summary>
        /// <param name="maxDelay">Optional.  The maximum amount of time to wait for updates to 
        /// settle, in milliseconds.  If omitted, wait will be indefinite.</param>
        /// <param name="pause">Optional.  Whether to leave the updates paused after exiting this 
        /// method.  While paused, calls to <seealso cref="Execute"/> will block.  Default is 
        /// false.</param>
        /// <returns>Returns whether the pause was successful.  A pause may fail if another thread 
        /// has caused updates to pause, or if forcing the pause causes too long a delay.</returns>
        public static bool Settle(int maxDelay = -1, bool pause = false)
        {
            
            if (!Monitor.TryEnter(_PauseObject)) return false;
            if (IsPaused) return false;

            TimeSpan remainingDelay = (maxDelay < 0) ? TimeSpan.MaxValue : new TimeSpan(0, 0, 0, 0, maxDelay);

            int held = 0;
            try
            {
                TimeSpan loopDelay = new TimeSpan(0, 0, 0, 0, (int)Math.Min(100, remainingDelay.TotalMilliseconds / 100));

                DateTime start = DateTime.Now;
                DateTime giveupTime = DateTime.Now + remainingDelay;

                // Must hold all the semaphore accesses.
                DateTime now;
                while (held < MaxConcurrentExecutions && (now = DateTime.Now) < giveupTime)
                {
                    TimeSpan thisLoop = (now + loopDelay > giveupTime) ? (now - giveupTime) : loopDelay;
                    if (!SettledSignal.WaitOne(thisLoop))
                        return false;
                    held++;
                }
                if (pause) IsPaused = true;

                // Finish up any updates currenly being processed.  (TODO:  the time delay could expire while this is running.  Must take care of that).
                if (!Finish((int)(giveupTime - DateTime.Now).TotalMilliseconds))
                    return false;
                return true;
            }
            finally
            {   
                if (!pause && !IsPaused)
                    SettledSignal.Release(held);
                Monitor.Exit(_PauseObject);
            }
        }
        /// <summary>
        /// Resumes update operations after having been paused by the <seealso cref="Settle(int, bool)"/> 
        /// method.  Threads which are blocked on the <seealso cref="Execute"/> method will resume 
        /// automatically.
        /// </summary>
        public static void Unpause()
        {
            lock (_PauseObject)
            {
                if (!IsPaused) return;
                SettledSignal.Release(MaxConcurrentExecutions);
            }
        }

        /// <summary>Creates an updating transaction for the given <seealso cref="Variable"/>.</summary>
        /// <param name="var">The <seealso cref="Variable"/> whose value and listeners will need 
        /// to be updated.</param>
        /// <param name="newContents">The new contents for the <seealso cref="Variable"/>.  If 
        /// provided null, the <seealso cref="Variable"/> will not have its 
        /// <seealso cref="Variable.Contents"/> changed, but its <seealso cref="Variable.Value"/> 
        /// and all listeners will be updated.
        /// </param>
        public static Update ForVariable(IVariable var, IEvaluateable newContents = null) => new Update(var, newContents, null);

        public static Update ForCollection(IVariable var, params IEvaluateable[] indices) => new Update(var, null, indices);

        /// <summary>
        /// Updates this object's <seealso cref="Update.Starter"/> with the given 
        /// <seealso cref="Update.NewContents"/>, and then ensures it reaches conclusion.
        /// </summary>
        public void Execute()
        {
            Start(true);
            Finish();
        }
        /// <summary>
        /// Starts the update for this object's <seealso cref="Update.Starter"/> with the given 
        /// <seealso cref="Update.NewContents"/>.
        /// </summary>
        /// <returns>Returns whether any change is made to the value of the <seealso cref="Update.Starter"/>.</returns>
        public bool Start(bool checkCircularity = true)
        {
            if (!SettledSignal.WaitOne(1000000))
                // TODO:  this indicates some kind of deadlock in the dependency update process.
                throw new InvalidOperationException("Waited too long to process update.");

            try
            {
                StructureLock.EnterUpgradeableReadLock();

                if (Starter is IUpdatedVariable iuv)
                {
                    // If the new contents equal the old contents, it can't possibly matter.
                    if (iuv.Contents.Equals(NewContents)) return false;

                    // Evaluate the new contents.  This will potentially establish a reference between NewContents and 
                    // existing variables.
                    IEvaluateable newValue = Helpers.Recalculate(NewContents);

                    // Update the iuv's new contents.
                    StructureLock.EnterWriteLock();
                    try
                    {
                        // Update the content's parent pointer.
                        if (iuv.Contents is ISyncUpdater idi_before && ReferenceEquals(idi_before.Parent, iuv))
                            idi_before.Parent = null;
                        if (NewContents is ISyncUpdater idi_after)
                            idi_after.Parent = (ISyncUpdater)iuv;

                        // Commit the new contents
                        if (!iuv.CommitContents(NewContents))
                            throw new InvalidOperationException("Invalid contents: " + NewContents.ToString());
                    }
                    finally { StructureLock.ExitWriteLock(); }

                    // If the iuv is now part of a circularity, the new value will be a CircularityError
                    if (checkCircularity && Helpers.TryFindDependency(NewContents, Starter, out var path))
                        newValue = new CircularityError(iuv, path);

                    // If the new value won't change the old value, no need to update listeners.
                    if (!iuv.CommitValue(newValue))
                        return false;
                }

                // The value must have changed.  If Starter updates synchronously,  get the synchronous update 
                // started.
                if (Starter is ISyncUpdater isu)
                {
                    Interlocked.Increment(ref _Updating);
                    _Execute(isu, isu.Parent, UniversalSet);
                }
                    

                // Finally, if Starter updates asynchronously, kick off the asynchronous update.
                if (Starter is IAsyncUpdater iau)
                {
                    foreach (var listener in iau.GetListeners())
                        Enqueue(iau, listener, UniversalSet);
                }
            }
            finally { StructureLock.ExitUpgradeableReadLock(); }



            // Done.  The user may separately call Finish()
            return true;
        }
        /// <summary>
        /// Force all updates on the queue to finish.
        /// </summary>
        public static bool Finish(int maxTime = -1)
        {
            // Force all the tasks to finish.  StructureLock should not be held.
            DateTime giveupTime = maxTime < 0 ? DateTime.MaxValue : DateTime.Now + new TimeSpan(0, 0, 0, 0, maxTime);
            while (_Tasks.TryDequeue(out Task t))
            {
                t.Wait();
                if (t.Exception != null)
                    throw new Exception("TODO:  a meaningful  message", t.Exception);
                if (DateTime.Now > giveupTime) return false;
            }
            return true;
        }

        internal void Enqueue(IAsyncUpdater source, ISyncUpdater listener, ITrueSet<IEvaluateable> indices) // TODO:  this should be done within the StructureLock read lock?
        {
            Interlocked.Increment(ref _Updating);
            _Tasks.Enqueue(Task.Run(() => _Execute(source, listener, indices)));
        }

        /// <summary>Executes this <see cref="Update"/> for the given 
        /// <seealso cref="ISyncUpdater"/> from the perspective of the given 
        /// <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The item called which executed the update for the 
        /// <paramref name="target"/>.  This  may be an <seealso cref="ISyncUpdater"/>, an 
        /// <seealso cref="IAsyncUpdater"/>, or an object that implements both.</param>
        /// <param name="target">The item which will be updated.  The <paramref name="target"/>'s 
        /// Parent will be the next item updated, and so on.</param>
        /// <param name="indexedDomain">The domain indexes that were updated below.</param>
        /// <returns>Returns true if any item's value was changed; otherwise, returns false.
        /// </returns>
        private bool _Execute(object source, ISyncUpdater target, ITrueSet<IEvaluateable> indexedDomain = null)
        {
            if (indexedDomain == null) indexedDomain = UniversalSet;
            ISyncUpdater start = target;
            ISyncUpdater updatedChild = source as ISyncUpdater;
            bool result = true;
            while (target != null)
            {
                // If nothing was updated, return false.
                indexedDomain = target.Update(this, updatedChild, indexedDomain);
                if (indexedDomain == null
                    || indexedDomain.IsEmpty) { result = !target.Equals(start); break; }

                // Since target was updated, enqueue its listeners and proceed (but only if Active is turned on).
                if (ActiveListeners)
                    if (target is IAsyncUpdater iv)
                        foreach (var listener in iv.GetListeners())
                            Enqueue(iv, listener, indexedDomain);

                // Set up for next level up.
                updatedChild = target;
                target = target.Parent;
                if (target is Reference)
                    throw new InvalidOperationException("A " + nameof(Reference) + " is not permitted to be a parent.");
            }

            if (Interlocked.Decrement(ref _Updating) == 0L)
            {
                SettledSignal.Release();
                Settled?.Invoke(this, new EventArgs());
            }
                
            return result;
        }

        /// <summary>Manages the thread access to a set of listeners.</summary>
        internal sealed class ListenerManager : IEnumerable<ISyncUpdater>
        {
            // This can NOT be the weak-weak pattern (a WeakReference<WeakReferenceSet<...>>).
            private readonly WeakReferenceSet<ISyncUpdater> _Listeners = new WeakReferenceSet<ISyncUpdater>();

            public int Count => _Listeners.Count;
            private WeakReferenceSet<ISyncUpdater> GetListeners() => _Listeners;
            public bool Add(ISyncUpdater listener)
            {
                StructureLock.EnterWriteLock();
                try { return _Listeners.Add(listener); }
                finally { StructureLock.ExitWriteLock(); }
            }
            public bool Remove(ISyncUpdater listener)
            {
                StructureLock.EnterWriteLock();
                try
                { return _Listeners.Remove(listener); }
                finally { StructureLock.ExitWriteLock(); }
            }

            IEnumerator<ISyncUpdater> IEnumerable<ISyncUpdater>.GetEnumerator() => _Listeners.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => _Listeners.GetEnumerator();
        }

    }
}
