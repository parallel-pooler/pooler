using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

namespace Parallel {
    /// <summary>
    /// Task item for tasks store.
    /// </summary>
    public struct Task {
        /// <summary>
        /// Task job to execute.
        /// </summary>
        public object Job;
        /// <summary>
        /// Task background thread priority t run with.
        /// </summary>
        public ThreadPriority Priority;
        /// <summary>
        /// If true, task is asynchronous, false if task is synchronous.
        /// </summary>
        public bool Async;
    }
    /// <summary>
    /// Event arguments object with Exceptions field as List&lt;Exception&gt; of synchronously executed threads catched exceptions.
    /// </summary>
    public class PoolerAllDoneEventArgs: EventArgs {
        public List<Exception> Exceptions = new List<Exception>();
        public int PeakThreadsCount = 0;
        public int ExecutedTasksCount = 0;
        public int NotExecutedTasksCount = 0;
    }
    /// <summary>
    /// Event arguments object with possible task result and currently and simultaneously running threads count in threads pool.
    /// </summary>
    public class PoolerTaskDoneEventArgs: EventArgs {
        public object TaskResult = null;
        public int RunningThreadsCount = 0;
    }
    /// <summary>
    /// Event arguments object with Exception field as Exception of synchronously executed thread catched exception.
    /// </summary>
    public class PoolerExceptionEventArgs: EventArgs {
        public Exception Exception = null;
    }
    /// <summary>
    /// Threads pool to run background tasks in parallel mode.
    /// There is possible to add taks and run then after all or instantly after adding, 
    /// also there is possible to change count of running tasks any time you want and to 
    /// run events when there is any exception in synchronously executed task or run
    /// event after all tasks are done with synchronously executed tasks and their catched exceptions.
    /// </summary>
    public class Pooler {
        /// <summary>
        /// Default maximum for parallely executed tasks in threads pool.
        /// </summary>
        const int RUNNING_TASKS_MAX_DEFAULT = 10;
        /// <summary>
        /// Default miliseconds for pooler.Pause(); call inside any task to slow down CPU or any other computer resources for each running thread in threads pool.
        /// </summary>
        const int PAUSE_MILESECONDS_DEFAULT = 0;
        /// <summary>
        /// Event called after all tasks in store are done. First param is threads pool instance, second param is event arguments with possible synchronous tasks exceptions.
        /// </summary>
        /// <param name="pool">Threads instance pool instance.</param>
        /// <param name="poolAllDoneEventArgs">Event arguments object with Exceptions field as List&lt;Exception&gt; of synchronously executed threads catched exceptions.</param>
        public delegate void AllDoneHandler (Pooler pool, PoolerAllDoneEventArgs poolAllDoneEventArgs);
        /// <summary>
        /// Event called after each tasks in store is done. First param is threads pool instance, second param is event arguments with possible task result and currently and simultaneously running tasks count.
        /// </summary>
        /// <param name="pool">Threads instance pool instance.</param>
        /// <param name="poolAllDoneEventArgs">Event arguments object with possible task result and currently and simultaneously running tasks count.</param>
        public delegate void TaskDoneHandler (Pooler pool, PoolerTaskDoneEventArgs poolTaskDoneEventArgs);
        /// <summary>
        /// Event called from background thread when there is catched any exception in synchronously executed task.
        /// </summary>
        /// <param name="pool">Threads instance pool instance.</param>
        /// <param name="poolThreadExceptionEventArgs">Event arguments object with Exception field as Exception of synchronously executed thread catched exception.</param>
        public delegate void ThreadExceptionHandler (Pooler pool, PoolerExceptionEventArgs poolThreadExceptionEventArgs);
        /// <summary>
        /// Event called after all tasks in store are done. First param is threads pool instance, second param is event arguments with possible synchronous tasks exceptions.
        /// </summary>
        public event AllDoneHandler AllDone = null;
        /// <summary>
        /// Event called after each task in store is done. First param is threads pool instance, second param is event arguments with possible task result and currently and simultaneously running tasks count.
        /// </summary>
        public event TaskDoneHandler TaskDone = null;
        /// <summary>
        /// Event called from background thread when there is catched any exception in synchronously executed task.
        /// </summary>
        public event ThreadExceptionHandler ThreadException = null;
        /// <summary>
        /// Any delegate added internaly into tasks store in Add() method with no params section or with single param accepting Pooler type.
        /// </summary>
        /// <param name="pool">threads instance pool instance.</param>
        public delegate void TaskDelegate (Pooler pool);
        /// <summary>
        /// Static instance created only once.
        /// </summary>
        private static Pooler _instance = null;
        private static readonly object _instanceLock = new object { };
        /// <summary>
        /// Tasks store to run in background threads.
        /// </summary>
        private List<Task> _store = new List<Task>();
        private readonly object _storeLock = new object { };
        /// <summary>
        /// Tasks background executing threads store to abort them immediately if necessary.
        /// </summary>
        private List<Thread> _threads = new List<Thread>();
        /// <summary>
        /// Not executed tasks count.
        /// </summary>
        private int _notExecutedTasksCount = 0;
        /// <summary>
        /// Currently running tasks count.
        /// </summary>
        private int _runningTasksCount = 0;
        /// <summary>
        /// Successfully executed tasks count.
        /// </summary>
        private int _executedTasksCount = 0;
        /// <summary>
        /// Currently running tasks maximum.
        /// </summary>
        private int _runningTasksMax = Pooler.RUNNING_TASKS_MAX_DEFAULT;
        /// <summary>
        /// Maximum peak of running threads in one moment in one executing process.
        /// </summary>
        private int _runningTasksCountMax = 0;
        private readonly object _runningTasksLock = new object { };
        /// <summary>
        /// Miliseconds for pooler.Pause(); call inside any task to slow down CPU or any other computer resources for each running thread in threads pool.
        /// </summary>
        private int _pauseMiliseconds = Pooler.PAUSE_MILESECONDS_DEFAULT;
        private readonly object _pauseMilisecondsLock = new object { };
        /// <summary>
        /// Exceptions store for synchronously running task fails, returned in AllDone handler.
        /// </summary>
        private List<Exception> _exceptions = new List<Exception>();
        private readonly object _exceptionsLock = new object { };

        /// <summary>
        /// Create and return new threads pool instance, nowhere regstered, just created.
        /// </summary>
        /// <param name="maxRunningTasks">Max threads running in parallel to execute given tasks.</param>
        /// <param name="pauseMiliseconds">Miliseconds for pooler.Pause(); call inside any task to slow down CPU or any other computer resources for each running thread in threads pool.</param>
        public Pooler (int maxRunningTasks = Pooler.RUNNING_TASKS_MAX_DEFAULT, int pauseMiliseconds = Pooler.PAUSE_MILESECONDS_DEFAULT) {
            this._runningTasksMax = maxRunningTasks;
            this._pauseMiliseconds = pauseMiliseconds;
        }
        /// <summary>
        /// Get single instance from Pooler._instance place created only once.
        /// </summary>
        /// <param name="maxRunningTasks">Max threads running in parallel to execute given tasks.</param>
        /// <param name="pauseMiliseconds">Miliseconds for pooler.Pause(); call inside any task to slow down CPU or any other computer resources for each running thread in threads pool.</param>
        /// <returns>static instance created only once.</returns>
        public static Pooler GetStaticInstance (int maxRunningTasks = Pooler.RUNNING_TASKS_MAX_DEFAULT, int pauseMiliseconds = Pooler.PAUSE_MILESECONDS_DEFAULT) {
            lock (Pooler._instanceLock) {
                if (Pooler._instance == null) Pooler._instance = new Pooler(maxRunningTasks, pauseMiliseconds);
            }
            return Pooler._instance;
        }
        /// <summary>
        /// Create and return new threads pool instance, nowhere regstered, just created.
        /// </summary>
        /// <param name="maxRunningTasks">Max threads running in parallel to execute given tasks.</param>
        /// <param name="pauseMiliseconds">Miliseconds for pooler.Pause(); call inside any task to slow down CPU or any other computer resources for each running thread in threads pool.</param>
        /// <returns>New threads pool instance to use.</returns>
        public static Pooler CreateNew (int maxRunningTasks = Pooler.RUNNING_TASKS_MAX_DEFAULT, int pauseMiliseconds = Pooler.PAUSE_MILESECONDS_DEFAULT) {
            return new Pooler(maxRunningTasks, pauseMiliseconds);
        }
        /// <summary>
        /// Change running tasks maximum anytime you want. 
        /// If you count down this value, all threads will go to their normal end.
        /// If you increase this value, there will be started new threads to execute next tasks in tasks store.
        /// </summary>
        /// <param name="maxRunningTasks">Max parallely running tasks in background to execute all given tasks.</param>
        /// <param name="increaseHeapRun">Start executing all threads necessary to start in increasing
        /// in one moment by default or start executing store tasks by growing threads increasing, one threads after it's done 
        /// triggers new thread and than there are running 2 threads, two threads after they are done 
        /// triggers new 2 threads and than there are running 4 threads and so on to maximum.
        /// </param>
        /// <returns></returns>
        public Pooler SetMaxRunningTasks (int maxRunningTasks = Pooler.RUNNING_TASKS_MAX_DEFAULT, bool increaseHeapRun = true) {
            int threadsCountToStart = 0;
			lock (this._runningTasksLock) {
				this._runningTasksMax = maxRunningTasks;
				if (increaseHeapRun) {
					if (this._runningTasksCount < maxRunningTasks) {
                        threadsCountToStart = maxRunningTasks - this._runningTasksCount;
                        if (threadsCountToStart > 0) {
                            for (int i = 0; i < threadsCountToStart; i++) {
                                this._runningTasksCount++;
                                if (this._runningTasksCountMax < this._runningTasksCount) {
                                    this._runningTasksCountMax = this._runningTasksCount;
                                }
                                this._executeInNewThreadFirstStoreTaskIfAnyOrDie();
                            }
                        }
                    }
                }
            }
            return this;
        }
        /// <summary>
        /// Get running tasks maximum anytime you want.
        /// </summary>
        /// <returns>Maximum peak of running threads in one moment in one executing process.</returns>
        public int GetMaxRunningTasks () {
            lock (this._runningTasksLock) {
                return this._runningTasksMax;
            }
		}
		/// <summary>
		/// Add task into threads pool to run.
		/// </summary>
		/// <param name="task">Any function added internaly into tasks store with with single param accepting Pooler type and returning any object as result.</param>
		/// <param name="runInstantly">If true by default, run added task instantly after adding in it's own thread in background. If false, call after all Add() method calls the method StartProcessing() to start pool processing.</param>
		/// <param name="priority">Background thread priority for task executing.</param>
		/// <param name="async">If task is using any other threads to work or async code, set this to true and call pool.AsyncTaskDone() call after your task is done manualy.</param>
		/// <returns>Current threads pool instance.</returns>
		public Pooler Add (Func<Pooler, object> task, bool runInstantly = true, ThreadPriority priority = ThreadPriority.Normal, bool async = false) {
			lock (this._storeLock) {
				this._store.Add(new Task {
					Job = task,
					Priority = priority,
					Async = async
				});
			}
			if (runInstantly) {
				lock (this._runningTasksLock) {
					if (this._runningTasksCount < this._runningTasksMax) {
						this._runningTasksCount++;
						if (this._runningTasksCountMax < this._runningTasksCount) {
							this._runningTasksCountMax = this._runningTasksCount;
						}
						this._executeInNewThreadFirstStoreTaskIfAnyOrDie();
					}
				}
			}
			return this;
		}
		/// <summary>
		/// Add task into threads pool to run.
		/// </summary>
		/// <param name="task">Any delegate added internaly into tasks store with no params section or with single param accepting Pooler type.</param>
		/// <param name="runInstantly">If true by default, run added task instantly after adding in it's own thread in background. If false, call after all Add() method calls the method StartProcessing() to start pool processing.</param>
		/// <param name="priority">Background thread priority for task executing.</param>
		/// <param name="async">If task is using any other threads to work or async code, set this to true and call pool.AsyncTaskDone() call after your task is done manualy.</param>
		/// <returns>Current threads pool instance.</returns>
		public Pooler Add (TaskDelegate task, bool runInstantly = true, ThreadPriority priority = ThreadPriority.Normal, bool async = false) {
            lock (this._storeLock) {
                this._store.Add(new Task {
                    Job = task,
                    Priority = priority,
                    Async = async
                });
            }
            if (runInstantly) {
                lock (this._runningTasksLock) {
                    if (this._runningTasksCount < this._runningTasksMax) {
                        this._runningTasksCount++;
                        if (this._runningTasksCountMax < this._runningTasksCount) {
                            this._runningTasksCountMax = this._runningTasksCount;
                        }
                        this._executeInNewThreadFirstStoreTaskIfAnyOrDie();
                    }
                }
            }
            return this;
        }
        /// <summary>
        /// Start tasks store processing in it's threads.
        /// Run this method any time you want, normaly when you 
        /// add tasks into pool not run instantly.
        /// </summary>
        /// <param name="heapRun">Try to start executing all threads in one moment by default 
        /// or start executing store tasks by growing threads increasing, one threads after it's done 
        /// triggers new thread and than there are running 2 threads, two threads after they are done 
        /// triggers new 2 threads and than there are running 4 threads and so on to maximum.
        /// </param>
        /// <returns>Current threads pool instance.</returns>
        public Pooler StartProcessing (bool heapRun = true) {
            if (heapRun) {
                lock (this._runningTasksLock) {
                    this._executedTasksCount = 0;
                    int threadsCountToStart = this._runningTasksMax - this._runningTasksCount;
                    if (threadsCountToStart > 0) {
                        for (int i = 0; i < threadsCountToStart; i++) {
                            this._runningTasksCount++;
                            if (this._runningTasksCountMax < this._runningTasksCount) {
                                this._runningTasksCountMax = this._runningTasksCount;
                            }
                            this._executeInNewThreadFirstStoreTaskIfAnyOrDie();
                        }
                    }
                }
            } else {
                lock (this._runningTasksLock) {
                    this._executedTasksCount = 0;
                    if (this._runningTasksCount < this._runningTasksMax) {
                        this._runningTasksCount++;
                        if (this._runningTasksCountMax < this._runningTasksCount) {
                            this._runningTasksCountMax = this._runningTasksCount;
                        }
                        this._executeInNewThreadFirstStoreTaskIfAnyOrDie();
                    }
                }
            }
            return this;
        }
        /// <summary>
        /// Stop processing background threads immediately by thread.Abort() or naturaly to empty
        /// the tasks store and run all runnung background threads into their natural end.
        /// </summary>
        /// <param name="abortAllThreadsImmediately"></param>
        public void StopProcessing (bool abortAllThreadsImmediately = true) {
            lock (this._storeLock) {
                this._notExecutedTasksCount = this._store.Count;
                this._store = new List<Task>();
            }
            if (abortAllThreadsImmediately) {
				// yeah, I know, lines bellow are realy creazy, 
				// but it works much better than whole foreach cycle 
				// on this._threads inside the lock this._runningTasksLock, 
				// because that construction causes death locks at the end, 
				// realy don't know why:-(
				int threadsCount = 0;
				lock (this._runningTasksLock) {
					threadsCount = this._threads.Count;
				}
				for (int i = 0; i < threadsCount; i++) {
					try {
						this._threads[i].Abort();
					} catch { }
				}
			}
		}
		/// <summary>
		/// Currently configured pause miliseconds value any time you want.
		/// </summary>
		/// <returns>Currently configured pause miliseconds value.</returns>
		public int GetPauseMiliseconds () {
			lock (this._pauseMilisecondsLock) {
				return this._pauseMiliseconds;
			}
		}
		/// <summary>
		/// Configure pause miliseconds value any time you want.
		/// </summary>
		/// <param name="pauseMiliseconds">Pause miliseconds to pause background execution thread by manual call pool.Pause() inside your job.</param>
		public void SetPauseMiliseconds (int pauseMiliseconds = 0) {
			lock (this._pauseMilisecondsLock) {
				this._pauseMiliseconds = pauseMiliseconds;
			}
		}

		/// <summary>
		/// Pause your running task by this call to slow down CPU or to release more any other computer resources by internal Thread.Sleep(); call with globaly configured miliseconds value, 0 by default.
		/// </summary>
		public void Pause () {
            int pauseMiliseconds = 0;
            lock (this._pauseMilisecondsLock) {
                pauseMiliseconds = this._pauseMiliseconds;
            }
            if (pauseMiliseconds > 0) Thread.Sleep(pauseMiliseconds);
        }
        /// <summary>
        /// Call this method in your task after all asynch code in your task is done
        /// to continue in next task in your threads pool.
        /// </summary>
        /// <param name="taskResult">If task was a function, put the result of the task into this place for TaskDone event.</param>
        public void AsyncTaskDone (object taskResult = null) {
            this._done(taskResult);
        }
        /// <summary>
        /// After synchronous task is done, this function is called internaly.
        /// After any asynchronous taks is done, there is necessary to call AsyncTaskDone() method.
        /// This method normaly run next taks, first from tasks store.
        /// If there is no taks in store, it stop itself.
        /// If there is higher running threads count than maximum and still enough tasks in store,
        /// it creates new thread to run those tasks by: this._checkMaximumChangeToStartNewOrStopCurrent();
        /// If there is lower running threads count than maximum, it stop itself.
        /// </summary>
        /// <param name="taskResult">If task was a function, put the result of the task into this place for TaskDone event.</param>
        private void _done (object taskResult = null) {
            if (this.TaskDone != null) {
                int runningTasksCount = 0;
                lock (this._runningTasksLock) {
                    runningTasksCount = this._runningTasksCount;
                }
                this.TaskDone.Invoke(this, new PoolerTaskDoneEventArgs {
                    RunningThreadsCount = runningTasksCount,
                    TaskResult = taskResult
                });
            }
            Task? task = null;
            lock (this._runningTasksLock) {
                this._executedTasksCount++;
                if (!this._checkMaximumChangeToStartNewOrStopCurrent()) return; // die
                lock (this._storeLock) {
                    if (this._store.Count > 0) {
                        task = this._store.ElementAt(0);
                        this._store.RemoveAt(0);
                    }
                }
            }
            if (task.HasValue) {
                this._executeTask(task.Value);
            } else {
                lock (this._runningTasksLock) {
                    this._runningTasksCount--;
                    this._allDoneIfNecessary();
                    this._threads.Remove(Thread.CurrentThread);
                    return; // die
                }
            }
        }
        /// <summary>
        /// After any thread is done, check if running threads count is higher 
        /// than maximum and count it down and die if it is lower.
        /// As the same check if running threads count is lower and also
        /// if store count is still bigger than one (so two, three and more)
        /// and run new thread to execute those remaining tasks, because there
        /// was probably change in threads maximum.
        /// </summary>
        private bool _checkMaximumChangeToStartNewOrStopCurrent() {
			// this method is always called inside: lock (this._runningTasksLock) {...
			if (this._runningTasksCount > this._runningTasksMax) {
                this._runningTasksCount--;
                this._allDoneIfNecessary();
                this._threads.Remove(Thread.CurrentThread);
                return false; // die
            } else if (this._runningTasksCount < this._runningTasksMax) {
				bool runNew = false;
				lock (this._storeLock) {
					if (this._store.Count > 1) runNew = true;
				}
				if (runNew) {
					this._runningTasksCount++;
                    if (this._runningTasksCountMax < this._runningTasksCount) {
                        this._runningTasksCountMax = this._runningTasksCount;
                    }
                    this._executeInNewThreadFirstStoreTaskIfAnyOrDie();
                }
            }
            return true;
        }
        /// <summary>
        /// Execute AllDone event only if running tasks count are done.
        /// This method is necessary to call internaly, 
        /// only in this._runningTasksLock lock object!
        /// </summary>
        private void _allDoneIfNecessary () {
            // this function is always called inside: lock (this._runningTasksLock) {...
            if (this._runningTasksCount == 0) {
                if (this.AllDone != null) this.AllDone.Invoke(this, new PoolerAllDoneEventArgs() {
                    Exceptions = new List<Exception>(this._exceptions),
                    PeakThreadsCount = this._runningTasksCountMax,
                    ExecutedTasksCount = this._executedTasksCount,
                    NotExecutedTasksCount = this._notExecutedTasksCount
                });
                this._executedTasksCount = 0;
                this._notExecutedTasksCount = 0;
                if (this._runningTasksCountMax < this._runningTasksCount) {
                    this._runningTasksCountMax = 0;
                }
                this._exceptions.Clear();
            }
        }
        /// <summary>
        /// Try to get first task from tasks store and run it in new thread.
        /// If there is no task, die wit all necessary count downs.
        /// </summary>
        private void _executeInNewThreadFirstStoreTaskIfAnyOrDie () {
            // this function is always called inside: lock (this._runningTasksLock) {
            Task task;
            lock (this._storeLock) {
                if (this._store.Count > 0) {
                    task = this._store.ElementAt(0);
                    this._store.RemoveAt(0);
                } else {
                    this._runningTasksCount--;
                    this._allDoneIfNecessary();
                    this._threads.Remove(Thread.CurrentThread);
                    return; // die
                }
            }
            Thread t = new Thread(new ThreadStart(delegate {
                this._executeTask(task);
            }));
            t.IsBackground = true;
            t.Priority = ThreadPriority.Lowest;
            this._threads.Add(t);
            t.Start();
        }
        /// <summary>
        /// Only execute given task in currently executed thread, 
        /// so there is no threading responsibility in this function.
        /// If task throw any exception, store the exception in exceptions 
        /// store and run ThreadException event imediatelly.
        /// </summary>
        /// <param name="task">Threads pool delegate or function from tasks store to execute.</param>
        private void _executeTask (Task task) {
            Thread.CurrentThread.Priority = task.Priority;
            object taskResult = null;
            TaskDelegate taskDelegate;
            Func<Pooler, object> taskFunction;
            if (task.Job is TaskDelegate) {
                taskDelegate = task.Job as TaskDelegate;
                try {
                    taskDelegate.Invoke(this);
                } catch (Exception e) {
                    lock (this._exceptionsLock) {
                        this._exceptions.Add(e);
                    }
                    if (this.ThreadException != null) this.ThreadException.Invoke(this, new PoolerExceptionEventArgs {
                        Exception = e
                    });
                } finally {
                    if (!task.Async) this._done(taskResult);
                }
            } else {
                taskFunction = task.Job as Func<Pooler, object>;
                try {
                    taskResult = taskFunction.Invoke(this);
                } catch (Exception e) {
                    lock (this._exceptionsLock) {
                        this._exceptions.Add(e);
                    }
                    if (this.ThreadException != null) this.ThreadException.Invoke(this, new PoolerExceptionEventArgs {
                        Exception = e
                    });
                } finally {
                    if (!task.Async) this._done(taskResult);
                }
            }
        }
    }
}