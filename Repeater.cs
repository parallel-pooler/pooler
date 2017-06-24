using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Pooler {
	/// <summary>
	/// Threads pool to execute any type of one given task in background threads,
	/// driven by background executing threads variable count, possibly configured 
	/// in run and also driven by task pausing to save CPU or other resources from .NET environment,
	/// also possible to configure at run.
	/// Repeater thread pool gives you three events to get any exception in your executed task,
	/// event when each task is done and event when all tasks are done. In event, when each
	/// task is done you can get currently running executing threads count in background.
	/// In the same way much more in other events.
	/// </summary>
	public class Repeater: Base {

		/// <summary>
		/// Task to run in background threads.
		/// </summary>
		private Task? _task = null;
		/// <summary>
		/// Tasks count to process repeatedly, null means infinite.
		/// </summary>
		private int? _tasksCount = null;

		/// <summary>
		/// Static instance created only once.
		/// </summary>
		private static Repeater _instance = null;
		/// <summary>
		/// Lock for reading/writing from/into this._instance;
		/// </summary>
		private static readonly object _instanceLock = new object { };

		/// <summary>
		/// Create and return new threads pool instance, nowhere regstered, just created.
		/// </summary>
		/// <param name="maxRunningTasks">Max threads running in parallel to execute given tasks.</param>
		/// <param name="tasksCount">Tasks count to process repeatedly, zero means infinite.</param>
		/// <param name="pauseMiliseconds">Miliseconds for pooler.Pause(); call inside any task to slow down CPU or any other computer resources for each running thread in threads pool.</param>
		public Repeater (int maxRunningTasks = 10, int? tasksCount = null, int pauseMiliseconds = 0) : base(maxRunningTasks, pauseMiliseconds) {
			this._tasksCount = tasksCount;
		}
		/// <summary>
		/// Get single instance from Parallel.instance place created only once.
		/// </summary>
		/// <param name="maxRunningTasks">Max threads running in parallel to execute given tasks.</param>
		/// <param name="tasksCount">Tasks count to process repeatedly, zero means infinite.</param>
		/// <param name="pauseMiliseconds">Miliseconds for pooler.Pause(); call inside any task to slow down CPU or any other computer resources for each running thread in threads pool.</param>
		/// <returns>static instance created only once.</returns>
		public static Repeater GetStaticInstance (int maxRunningTasks = Base.RUNNING_TASKS_MAX_DEFAULT, int? tasksCount = null, int pauseMiliseconds = Base.PAUSE_MILESECONDS_DEFAULT) {
			lock (Repeater._instanceLock) {
				if (Repeater._instance == null) Repeater._instance = new Repeater(maxRunningTasks, tasksCount, pauseMiliseconds);
			}
			return Repeater._instance;
		}
		/// <summary>
		/// Create and return new threads pool instance, nowhere regstered, just created.
		/// </summary>
		/// <param name="maxRunningTasks">Max threads running in parallel to execute given tasks.</param>
		/// <param name="tasksCount">Tasks count to process repeatedly, zero means infinite.</param>
		/// <param name="pauseMiliseconds">Miliseconds for pooler.Pause(); call inside any task to slow down CPU or any other computer resources for each running thread in threads pool.</param>
		/// <returns>New threads pool instance to use.</returns>
		public static Repeater CreateNew (int maxRunningTasks = Base.RUNNING_TASKS_MAX_DEFAULT, int? tasksCount = null, int pauseMiliseconds = Base.PAUSE_MILESECONDS_DEFAULT) {
			return new Repeater(maxRunningTasks, tasksCount, pauseMiliseconds);
		}

		/// <summary>
		/// Set task into threads pool to run.
		/// </summary>
		/// <param name="task">Any function added internaly into tasks store with with single param accepting Parallel type and returning any object as result.</param>
		/// <param name="runInstantly">If true by default, run added task instantly after adding in it's own thread in background. If false, call after all Add() method calls the method StartProcessing() to start pool processing.</param>
		/// <param name="priority">Background thread priority for task executing.</param>
		/// <param name="async">If task is using any other threads to work or async code, set this to true and call pool.AsyncTaskDone() call after your task is done manualy.</param>
		/// <returns>Current threads pool instance.</returns>
		public Repeater Set (Func<Base, object> task, bool runInstantly = true, ThreadPriority priority = ThreadPriority.Normal, bool async = false) {
			lock (this.runningTasksLock) {
				this._task = new Task {
					Job = task,
					Priority = priority,
					Async = async
				};
			}
			if (runInstantly) {
				lock (this.runningTasksLock) {
					if (this.runningTasksCount < this.runningTasksMax) {
						this.runExecutingTaskInNewThread();
					}
				}
			}
			return this;
		}

		/// <summary>
		/// Set task into threads pool to run.
		/// </summary>
		/// <param name="task">Any delegate added internaly into tasks store with no params section or with single param accepting Parallel type.</param>
		/// <param name="runInstantly">If true by default, run added task instantly after adding in it's own thread in background. If false, call after all Add() method calls the method StartProcessing() to start pool processing.</param>
		/// <param name="priority">Background thread priority for task executing.</param>
		/// <param name="async">If task is using any other threads to work or async code, set this to true and call pool.AsyncTaskDone() call after your task is done manualy.</param>
		/// <returns>Current threads pool instance.</returns>
		public Repeater Set (TaskDelegate task, bool runInstantly = true, ThreadPriority priority = ThreadPriority.Normal, bool async = false) {
			lock (this.runningTasksLock) {
				this._task = new Task {
					Job = task,
					Priority = priority,
					Async = async
				};
			}
			if (runInstantly) {
				lock (this.runningTasksLock) {
					if (this.runningTasksCount < this.runningTasksMax) {
						this.runExecutingTaskInNewThread();
					}
				}
			}
			return this;
		}

		/// <summary>
		/// Stop processing background threads immediately by thread.Abort() or naturaly to empty
		/// the tasks store and run all runnung background threads into their natural end.
		/// </summary>
		/// <param name="abortAllThreadsImmediately">Abord all threads by thread.Abort(); to stop background executing threads immediately.</param>
		public override void StopProcessing (bool abortAllThreadsImmediately = true) {
			int threadsCount = 0;
			lock (this.runningTasksLock) {
				if (this._tasksCount.HasValue) {
					this.executedTasksCount = this._tasksCount.Value;
				} else {
					this._tasksCount = this.executedTasksCount;
				}
				threadsCount = this.threads.Count;
			}
			if (abortAllThreadsImmediately) {
				// yeah, I know, lines bellow are realy creazy, 
				// but it works much better than whole foreach cycle 
				// on this.threads inside the lock this.runningTasksLock, 
				// because that construction causes death locks at the end, 
				// realy don't know why:-(
				for (int i = 0; i < threadsCount; i++) {
					try {
						this.threads[i].Abort();
					} catch { }
				}
			}
		}

		/// <summary>
		/// After synchronous task is done, this function is called internaly.
		/// After any asynchronous taks is done, there is necessary to call pool.AsyncTaskDone(); method manualy from task job function.
		/// This method normaly run next taks, first from internal tasks store.
		/// If there is no task in the store, the thread stops itself.
		/// If there is higher running threads count than maximum and still enough tasks in the store,
		/// it creates new thread to run those tasks by: this.runExecutingTaskInNewThread();
		/// If there is lower running threads count than maximum, it stop itself.
		/// </summary>
		/// <param name="taskResult">If task was a function, put the result of the task into this place for TaskDone event.</param>
		protected override void done (object taskResult = null) {
			Task? task = null;
			int runningTasksCount = 0;
			int executedTasksCount = 0;
			lock (this.runningTasksLock) {
				runningTasksCount = this.runningTasksCount;
				executedTasksCount = this.executedTasksCount;
				if (this.runningTasksCount <= this.runningTasksMax) {
					if (this.runningTasksCount < this.runningTasksMax) {
						if ((!this._tasksCount.HasValue) || (this._tasksCount.HasValue && this.executedTasksCount + 1 < this._tasksCount.Value)) {
							this.runExecutingTaskInNewThread();
						}
					}
					if ((!this._tasksCount.HasValue) || (this._tasksCount.HasValue && this.executedTasksCount < this._tasksCount.Value)) {
						this.executedTasksCount++;
						task = this._task;
					}
				}
			}
			if (this.taskDoneHasHandlers()) {
				this.taskDoneInvoke(new TaskDoneEventArgs {
					RunningTasksCount = runningTasksCount,
					ExecutedTasksCount = executedTasksCount,
					TaskResult = taskResult
				});
			}
			if (task.HasValue) {
				this.executeTask(task.Value);
			} else {
				lock (this.runningTasksLock) {
					this.executingThreadEnd();
				}
			}
		}

		/// <summary>
		/// This function has to be called only inside this.runningTasksLock;
		/// It ads one more into this.runningTasksCount, manage the this.runningTasksCountMax,
		/// gets first task from tasks store and it runs the task in new registered background thread.
		/// </summary>
		protected override void runExecutingTaskInNewThread () {
			// this function is always called inside: lock (this.runningTasksLock) {
			this.executedTasksCount++;
			this.runningTasksCount++;
			if (this.runningTasksCountMax < this.runningTasksCount) {
				this.runningTasksCountMax = this.runningTasksCount;
			}
			Task task = this._task.Value;
			Thread t = new Thread(new ThreadStart(delegate {
				this.executeTask(task);
			}));
			t.IsBackground = true;
			t.Priority = task.Priority;
			this.threads.Add(t);
			t.Start();
		}
	}
}
