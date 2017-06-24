using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Pooler {
	/// <summary>
	/// Pooler base abstract class for Parallel and Repeater
	/// with constants, events, fields and functions for both classes.
	/// </summary>
	public abstract class Base {

		/// <summary>
		/// Default maximum for parallely executed tasks in threads pool.
		/// </summary>
		public const int RUNNING_TASKS_MAX_DEFAULT = 10;
		/// <summary>
		/// Default miliseconds for pooler.Pause(); call inside any task to slow down CPU or any other computer resources for each running thread in threads pool.
		/// </summary>
		public const int PAUSE_MILESECONDS_DEFAULT = 0;

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
		public event TaskExceptionHandler TaskException = null;

		/// <summary>
		/// Tasks background executing threads store to abort them immediately if necessary.
		/// </summary>
		protected List<Thread> threads = new List<Thread>();
		/// <summary>
		/// Currently running tasks count.
		/// </summary>
		protected int runningTasksCount = 0;
		/// <summary>
		/// Currently running tasks maximum.
		/// </summary>
		protected int runningTasksMax = Base.RUNNING_TASKS_MAX_DEFAULT;
		/// <summary>
		/// Successfully executed tasks count.
		/// </summary>
		protected int executedTasksCount = 0;
		/// <summary>
		/// Maximum peak of running threads in one moment in one executing process.
		/// </summary>
		protected int runningTasksCountMax = 0;
		/// <summary>
		/// Lock for reading/writing from/into this.runningTasksCountMax;
		/// </summary>
		protected readonly object runningTasksLock = new object { };

		/// <summary>
		/// Miliseconds for pooler.Pause(); call inside any task to slow down CPU or any other computer resources for each running thread in threads pool.
		/// </summary>
		protected int pauseMiliseconds = Base.PAUSE_MILESECONDS_DEFAULT;
		/// <summary>
		/// Lock for reading/writing from/into this.pauseMiliseconds;
		/// </summary>
		protected readonly object pauseMilisecondsLock = new object { };

		/// <summary>
		/// Exceptions store for synchronously running task fails, returned in AllDone handler.
		/// </summary>
		protected List<Exception> exceptions = new List<Exception>();
		/// <summary>
		/// Lock for reading/writing from/into this.exceptions;
		/// </summary>
		protected readonly object exceptionsLock = new object { };

		/// <summary>
		/// Create and return new threads pool instance, nowhere regstered, just created.
		/// </summary>
		/// <param name="maxRunningTasks">Max threads running in parallel to execute given tasks.</param>
		/// <param name="pauseMiliseconds">Miliseconds for pooler.Pause(); call inside any task to slow down CPU or any other computer resources for each running thread in threads pool.</param>
		public Base (int maxRunningTasks = Base.RUNNING_TASKS_MAX_DEFAULT, int pauseMiliseconds = Base.PAUSE_MILESECONDS_DEFAULT) {
			this.runningTasksMax = maxRunningTasks;
			this.pauseMiliseconds = pauseMiliseconds;
		}

		/// <summary>
		/// Get running tasks maximum anytime you want.
		/// </summary>
		/// <returns>Maximum peak of running threads in one moment in one executing process.</returns>
		public virtual int GetMaxRunningTasks () {
			lock (this.runningTasksLock) {
				return this.runningTasksMax;
			}
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
		public virtual Base SetMaxRunningTasks (int maxRunningTasks = Base.RUNNING_TASKS_MAX_DEFAULT, bool increaseHeapRun = true) {
			int threadsCountToStart = 0;
			lock (this.runningTasksLock) {
				this.runningTasksMax = maxRunningTasks;
				if (increaseHeapRun) {
					if (this.runningTasksCount < maxRunningTasks) {
						threadsCountToStart = maxRunningTasks - this.runningTasksCount;
						if (threadsCountToStart > 0) {
							for (int i = 0; i < threadsCountToStart; i++) {
								this.runExecutingTaskInNewThread();
							}
						}
					}
				}
			}
			return this;
		}

		/// <summary>
		/// Currently configured pause miliseconds value any time you want.
		/// </summary>
		/// <returns>Currently configured pause miliseconds value.</returns>
		public virtual int GetPauseMiliseconds () {
			lock (this.pauseMilisecondsLock) {
				return this.pauseMiliseconds;
			}
		}
		/// <summary>
		/// Configure pause miliseconds value any time you want.
		/// </summary>
		/// <param name="pauseMiliseconds">Pause miliseconds to pause background execution thread by manual call pool.Pause() inside your job.</param>
		public virtual void SetPauseMiliseconds (int pauseMiliseconds = 0) {
			lock (this.pauseMilisecondsLock) {
				this.pauseMiliseconds = pauseMiliseconds;
			}
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
		public virtual Base StartProcessing (bool heapRun = true) {
			if (heapRun) {
				lock (this.runningTasksLock) {
					this.executedTasksCount = 0;
					int threadsCountToStart = this.runningTasksMax - this.runningTasksCount;
					if (threadsCountToStart > 0) {
						for (int i = 0; i < threadsCountToStart; i++) {
							this.runExecutingTaskInNewThread();
						}
					}
				}
			} else {
				lock (this.runningTasksLock) {
					this.executedTasksCount = 0;
					if (this.runningTasksCount < this.runningTasksMax) {
						this.runExecutingTaskInNewThread();
					}
				}
			}
			return this;
		}

		/// <summary>
		/// Pause your running task by this call to slow down CPU or to release more any other computer resources by internal Thread.Sleep(); call with globaly configured miliseconds value, 0 by default.
		/// </summary>
		public virtual void Pause () {
			int pauseMiliseconds = 0;
			lock (this.pauseMilisecondsLock) {
				pauseMiliseconds = this.pauseMiliseconds;
			}
			if (pauseMiliseconds > 0) Thread.Sleep(pauseMiliseconds);
		}

		/// <summary>
		/// Call this method in your task after all asynch code in your task is done
		/// to continue in next task in your threads pool.
		/// </summary>
		/// <param name="taskResult">If task was a function, put the result of the task into this place for TaskDone event.</param>
		public virtual void AsyncTaskDone (object taskResult = null) {
			this.done(taskResult);
		}

		/// <summary>
		/// Only execute given task in currently executed thread, 
		/// so there is no threading responsibility in this function.
		/// If task throw any exception, store the exception in exceptions 
		/// store and run ThreadException event imediatelly.
		/// </summary>
		/// <param name="task">Threads pool delegate or function from tasks store to execute.</param>
		protected virtual void executeTask (Task task) {
			Thread.CurrentThread.Priority = task.Priority;
			object taskResult = null;
			object taskJob = task.Job;
			try {
				if (taskJob is TaskDelegate) {
					(taskJob as TaskDelegate).Invoke(this);
				} else if (taskJob is Func<Base, object>) {
					taskResult = (taskJob as Func<Base, object>).Invoke(this);
				} else {
					throw new Exception("Pooler task has to be type 'delegate', 'Pooler.TaskDelegate' (void accepting first param to be 'Pooler.Base') or 'Func<Pooler.Base, object>'.");
				}
			} catch (Exception e) {
				lock (this.exceptionsLock) {
					this.exceptions.Add(e);
				}
				if (this.TaskException != null) this.TaskException.Invoke(this, new ExceptionEventArgs {
					Exception = e
				});
			} finally {
				if (!task.Async) this.done(taskResult);
			}
		}

		/// <summary>
		/// This method is necessary to call internaly only in this.runningTasksLock lock object!
		/// Remove one from this.runningTasksCount, unregister current thread from this.threads;
		/// Execute AllDone event only if running tasks count are done.
		/// </summary>
		protected virtual void executingThreadEnd () {
			// this function is always called inside: lock (this.runningTasksLock) {...
			this.runningTasksCount--;
			this.threads.Remove(Thread.CurrentThread);
			if (this.runningTasksCount == 0) {
				if (this.AllDone != null) this.AllDone.Invoke(this, new AllDoneEventArgs {
					Exceptions = new List<Exception>(this.exceptions),
					PeakThreadsCount = this.runningTasksCountMax,
					ExecutedTasksCount = this.executedTasksCount,
				});
				this.executedTasksCount = 0;
				if (this.runningTasksCountMax < this.runningTasksCount) {
					this.runningTasksCountMax = 0;
				}
				this.exceptions.Clear();
			}
		}

		/// <summary>
		/// Return true if there are any handlers attached on TaskDone event.
		/// </summary>
		/// <returns>True if any handler attached.</returns>
		protected virtual bool taskDoneHasHandlers () {
			return this.TaskDone != null;
		}
		
		/// <summary>
		/// Invoke all handlers on event TaskDone without checking if there are any handlers appended.
		/// Use the this.taskDoneHasHandlers() function to check it.
		/// </summary>
		/// <param name="e">Event arguments instance for invoking  handlers.</param>
		protected virtual void taskDoneInvoke (TaskDoneEventArgs e) {
			this.TaskDone.Invoke(this, e);
		}

		/// <summary>
		/// Stop processing background threads immediately by thread.Abort() 
		/// or naturaly to set something to stop next tasks runs in this.done(); function.
		/// </summary>
		/// <param name="abortAllThreadsImmediately">Abord all threads by thread.Abort(); to stop background executing threads immediately.</param>
		public abstract void StopProcessing (bool abortAllThreadsImmediately = true);

		/// <summary>
		/// After synchronous task is done, this function is called internaly.
		/// After any asynchronous taks is done, there is necessary to call pool.AsyncTaskDone(); method manualy from task job function.
		/// This method has to run next task if there is need to run anything more.
		/// If there is no need to run anything more, the thread has to stops itself.
		/// If there is higher running threads count than maximum and it still needs to run some tasks,
		/// it has to create new thread to run another next task by: this.runExecutingTaskInNewThread();
		/// If there is lower running threads count than maximum, it has to stop itself.
		/// </summary>
		/// <param name="taskResult">If task was a function, put the result of the task into this place for TaskDone event.</param>
		protected abstract void done (object taskResult = null);

		/// <summary>
		/// This function has to be called only inside this.runningTasksLock;
		/// It has to add one more into this.runningTasksCount, has to manage the this.runningTasksCountMax,
		/// it has to get task to process somewhere and it has to run the task in new registered background thread.
		/// </summary>
		protected abstract void runExecutingTaskInNewThread ();
	}
}
